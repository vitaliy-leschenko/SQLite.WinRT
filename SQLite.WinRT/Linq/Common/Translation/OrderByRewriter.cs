﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using SQLite.WinRT.Linq.Base;
using SQLite.WinRT.Linq.Common.Expressions;

namespace SQLite.WinRT.Linq.Common.Translation
{
    /// <summary>
    ///     Moves order-bys to the outermost select if possible
    /// </summary>
    public class OrderByRewriter : DbExpressionVisitor
    {
        private IList<OrderExpression> gatheredOrderings;

        private bool isOuterMostSelect;

        private bool suppressOrderby;

        private OrderByRewriter()
        {
            isOuterMostSelect = true;
        }

        public static Expression Rewrite(Expression expression)
        {
            return new OrderByRewriter().Visit(expression);
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            bool saveIsOuterMostSelect = isOuterMostSelect;
            try
            {
                isOuterMostSelect = false;

                bool saveSuppressOrderBy = suppressOrderby;
                suppressOrderby = false;

                IList<OrderExpression> saveGatheredOrderings = gatheredOrderings;
                if (saveSuppressOrderBy)
                {
                    gatheredOrderings = null;
                    isOuterMostSelect = true;
                }

                select = (SelectExpression) base.VisitSelect(select);

                suppressOrderby = saveSuppressOrderBy;
                if (saveSuppressOrderBy)
                {
                    gatheredOrderings = saveGatheredOrderings;
                }

                bool hasOrderBy = select.OrderBy != null && select.OrderBy.Count > 0;
                bool hasGroupBy = select.GroupBy != null && select.GroupBy.Count > 0;
                bool canHaveOrderBy = saveIsOuterMostSelect || select.Take != null || select.Skip != null;
                bool canReceiveOrderings = canHaveOrderBy && !hasGroupBy && !select.IsDistinct
                                           && !AggregateChecker.HasAggregates(select) && !suppressOrderby;

                if (hasOrderBy)
                {
                    PrependOrderings(select.OrderBy);
                }

                if (select.IsReverse)
                {
                    ReverseOrderings();
                }

                IEnumerable<OrderExpression> orderings = null;
                if (canReceiveOrderings)
                {
                    orderings = gatheredOrderings;
                }
                else if (canHaveOrderBy)
                {
                    orderings = select.OrderBy;
                }

                bool canPassOnOrderings = !saveIsOuterMostSelect && !hasGroupBy && !select.IsDistinct &&
                                          !suppressOrderby;
                ReadOnlyCollection<ColumnDeclaration> columns = select.Columns;
                if (gatheredOrderings != null)
                {
                    if (canPassOnOrderings)
                    {
                        HashSet<TableAlias> producedAliases = DeclaredAliasGatherer.Gather(select.From);
                        // reproject order expressions using this select's alias so the outer select will have properly formed expressions
                        BindResult project = RebindOrderings(gatheredOrderings, select.Alias, producedAliases,
                            select.Columns);
                        gatheredOrderings = null;
                        PrependOrderings(project.Orderings);
                        columns = project.Columns;
                    }
                    else
                    {
                        gatheredOrderings = null;
                    }
                }

                if (orderings != select.OrderBy || columns != select.Columns || select.IsReverse)
                {
                    select = new SelectExpression(
                        select.Alias,
                        columns,
                        select.From,
                        select.Where,
                        orderings,
                        select.GroupBy,
                        select.IsDistinct,
                        select.Skip,
                        select.Take,
                        false);
                }

                return select;
            }
            finally
            {
                isOuterMostSelect = saveIsOuterMostSelect;
            }
        }

        protected override Expression VisitSubquery(SubqueryExpression subquery)
        {
            bool saveSuppressOrderBy = suppressOrderby;
            suppressOrderby = true;
            Expression result = base.VisitSubquery(subquery);
            suppressOrderby = saveSuppressOrderBy;
            return result;
        }

        protected override Expression VisitJoin(JoinExpression join)
        {
            // make sure order by expressions lifted up from the left side are not lost
            // when visiting the right side
            Expression left = VisitSource(join.Left);
            IList<OrderExpression> leftOrders = gatheredOrderings;
            gatheredOrderings = null; // start on the right with a clean slate
            Expression right = VisitSource(join.Right);
            PrependOrderings(leftOrders);
            Expression condition = Visit(join.Condition);
            if (left != join.Left || right != join.Right || condition != join.Condition)
            {
                return new JoinExpression(join.Join, left, right, condition);
            }
            return join;
        }

        /// <summary>
        ///     Add a sequence of order expressions to an accumulated list, prepending so as
        ///     to give precedence to the new expressions over any previous expressions
        /// </summary>
        /// <param name="newOrderings"></param>
        protected void PrependOrderings(IList<OrderExpression> newOrderings)
        {
            if (newOrderings != null)
            {
                if (gatheredOrderings == null)
                {
                    gatheredOrderings = new List<OrderExpression>();
                }
                for (int i = newOrderings.Count - 1; i >= 0; i--)
                {
                    gatheredOrderings.Insert(0, newOrderings[i]);
                }
                // trim off obvious duplicates
                var unique = new HashSet<string>();
                for (int i = 0; i < gatheredOrderings.Count;)
                {
                    var column = gatheredOrderings[i].Expression as ColumnExpression;
                    if (column != null)
                    {
                        string hash = column.Alias + ":" + column.Name;
                        if (unique.Contains(hash))
                        {
                            gatheredOrderings.RemoveAt(i);
                            // don't increment 'i', just continue
                            continue;
                        }
                        unique.Add(hash);
                    }
                    i++;
                }
            }
        }

        protected void ReverseOrderings()
        {
            if (gatheredOrderings != null)
            {
                for (int i = 0, n = gatheredOrderings.Count; i < n; i++)
                {
                    OrderExpression ord = gatheredOrderings[i];
                    gatheredOrderings[i] =
                        new OrderExpression(
                            ord.OrderType == OrderType.Ascending ? OrderType.Descending : OrderType.Ascending,
                            ord.Expression);
                }
            }
        }

        /// <summary>
        ///     Rebind order expressions to reference a new alias and add to column declarations if necessary
        /// </summary>
        protected virtual BindResult RebindOrderings(
            IEnumerable<OrderExpression> orderings,
            TableAlias alias,
            HashSet<TableAlias> existingAliases,
            IEnumerable<ColumnDeclaration> existingColumns)
        {
            List<ColumnDeclaration> newColumns = null;
            var newOrderings = new List<OrderExpression>();
            foreach (OrderExpression ordering in orderings)
            {
                Expression expr = ordering.Expression;
                var column = expr as ColumnExpression;
                if (column == null || (existingAliases != null && existingAliases.Contains(column.Alias)))
                {
                    // check to see if a declared column already contains a similar expression
                    int iOrdinal = 0;
                    foreach (ColumnDeclaration decl in existingColumns)
                    {
                        var declColumn = decl.Expression as ColumnExpression;
                        if (decl.Expression == ordering.Expression
                            ||
                            (column != null && declColumn != null && column.Alias == declColumn.Alias &&
                             column.Name == declColumn.Name))
                        {
                            // found it, so make a reference to this column
                            expr = new ColumnExpression(column.Type, column.QueryType, alias, decl.Name);
                            break;
                        }
                        iOrdinal++;
                    }
                    // if not already projected, add a new column declaration for it
                    if (expr == ordering.Expression)
                    {
                        if (newColumns == null)
                        {
                            newColumns = new List<ColumnDeclaration>(existingColumns);
                            existingColumns = newColumns;
                        }
                        string colName = column != null ? column.Name : "c" + iOrdinal;
                        colName = newColumns.GetAvailableColumnName(colName);
                        DbQueryType colType = DbTypeSystem.GetColumnType(expr.Type);
                        newColumns.Add(new ColumnDeclaration(colName, ordering.Expression, colType));
                        expr = new ColumnExpression(expr.Type, colType, alias, colName);
                    }
                    newOrderings.Add(new OrderExpression(ordering.OrderType, expr));
                }
            }
            return new BindResult(existingColumns, newOrderings);
        }

        protected class BindResult
        {
            private readonly ReadOnlyCollection<ColumnDeclaration> columns;

            private readonly ReadOnlyCollection<OrderExpression> orderings;

            public BindResult(IEnumerable<ColumnDeclaration> columns, IEnumerable<OrderExpression> orderings)
            {
                this.columns = columns as ReadOnlyCollection<ColumnDeclaration>;
                if (this.columns == null)
                {
                    this.columns = new List<ColumnDeclaration>(columns).AsReadOnly();
                }
                this.orderings = orderings as ReadOnlyCollection<OrderExpression>;
                if (this.orderings == null)
                {
                    this.orderings = new List<OrderExpression>(orderings).AsReadOnly();
                }
            }

            public ReadOnlyCollection<ColumnDeclaration> Columns
            {
                get { return columns; }
            }

            public ReadOnlyCollection<OrderExpression> Orderings
            {
                get { return orderings; }
            }
        }
    }
}