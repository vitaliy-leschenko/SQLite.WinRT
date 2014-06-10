// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq.Expressions;
using SQLite.WinRT.Linq.Base;
using SQLite.WinRT.Linq.Common.Expressions;

namespace SQLite.WinRT.Linq.Common.Translation
{
    /// <summary>
    ///     Removes column declarations in SelectExpression's that are not referenced
    /// </summary>
    public class UnusedColumnRemover : DbExpressionVisitor
    {
        private readonly Dictionary<TableAlias, HashSet<string>> allColumnsUsed;

        private bool retainAllColumns;

        private UnusedColumnRemover()
        {
            allColumnsUsed = new Dictionary<TableAlias, HashSet<string>>();
        }

        public static Expression Remove(Expression expression)
        {
            return new UnusedColumnRemover().Visit(expression);
        }

        private void MarkColumnAsUsed(TableAlias alias, string name)
        {
            HashSet<string> columns;
            if (!allColumnsUsed.TryGetValue(alias, out columns))
            {
                columns = new HashSet<string>();
                allColumnsUsed.Add(alias, columns);
            }
            columns.Add(name);
        }

        private bool IsColumnUsed(TableAlias alias, string name)
        {
            HashSet<string> columnsUsed;
            if (allColumnsUsed.TryGetValue(alias, out columnsUsed))
            {
                if (columnsUsed != null)
                {
                    return columnsUsed.Contains(name);
                }
            }
            return false;
        }

        private void ClearColumnsUsed(TableAlias alias)
        {
            allColumnsUsed[alias] = new HashSet<string>();
        }

        protected override Expression VisitColumn(ColumnExpression column)
        {
            MarkColumnAsUsed(column.Alias, column.Name);
            return column;
        }

        protected override Expression VisitSubquery(SubqueryExpression subquery)
        {
            if ((subquery.NodeType == (ExpressionType) DbExpressionType.Scalar
                 || subquery.NodeType == (ExpressionType) DbExpressionType.In) && subquery.Select != null)
            {
                Debug.Assert(subquery.Select.Columns.Count == 1);
                MarkColumnAsUsed(subquery.Select.Alias, subquery.Select.Columns[0].Name);
            }
            return base.VisitSubquery(subquery);
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            // visit column projection first
            ReadOnlyCollection<ColumnDeclaration> columns = select.Columns;

            bool wasRetained = retainAllColumns;
            retainAllColumns = false;

            List<ColumnDeclaration> alternate = null;
            for (int i = 0, n = select.Columns.Count; i < n; i++)
            {
                ColumnDeclaration decl = select.Columns[i];
                if (wasRetained || select.IsDistinct || IsColumnUsed(select.Alias, decl.Name))
                {
                    Expression expr = Visit(decl.Expression);
                    if (expr != decl.Expression)
                    {
                        decl = new ColumnDeclaration(decl.Name, expr, decl.QueryType);
                    }
                }
                else
                {
                    decl = null; // null means it gets omitted
                }
                if (decl != select.Columns[i] && alternate == null)
                {
                    alternate = new List<ColumnDeclaration>();
                    for (int j = 0; j < i; j++)
                    {
                        alternate.Add(select.Columns[j]);
                    }
                }
                if (decl != null && alternate != null)
                {
                    alternate.Add(decl);
                }
            }
            if (alternate != null)
            {
                columns = alternate.AsReadOnly();
            }

            Expression take = Visit(select.Take);
            Expression skip = Visit(select.Skip);
            ReadOnlyCollection<Expression> groupbys = VisitExpressionList(select.GroupBy);
            ReadOnlyCollection<OrderExpression> orderbys = VisitOrderBy(select.OrderBy);
            Expression where = Visit(select.Where);

            Expression from = Visit(select.From);

            ClearColumnsUsed(select.Alias);

            if (columns != select.Columns || take != select.Take || skip != select.Skip || orderbys != select.OrderBy
                || groupbys != select.GroupBy || where != select.Where || from != select.From)
            {
                select = new SelectExpression(
                    select.Alias, columns, from, where, orderbys, groupbys, select.IsDistinct, skip, take,
                    select.IsReverse);
            }

            retainAllColumns = wasRetained;

            return select;
        }

        protected override Expression VisitAggregate(AggregateExpression aggregate)
        {
            // COUNT(*) forces all columns to be retained in subquery
            if (aggregate.AggregateName == "Count" && aggregate.Argument == null)
            {
                retainAllColumns = true;
            }
            return base.VisitAggregate(aggregate);
        }

        protected override Expression VisitProjection(ProjectionExpression projection)
        {
            // visit mapping in reverse order
            Expression projector = Visit(projection.Projector);
            var select = (SelectExpression) Visit(projection.Select);
            return UpdateProjection(projection, select, projector, projection.Aggregator);
        }

        protected override Expression VisitClientJoin(ClientJoinExpression join)
        {
            ReadOnlyCollection<Expression> innerKey = VisitExpressionList(join.InnerKey);
            ReadOnlyCollection<Expression> outerKey = VisitExpressionList(join.OuterKey);
            var projection = (ProjectionExpression) Visit(join.Projection);
            if (projection != join.Projection || innerKey != join.InnerKey || outerKey != join.OuterKey)
            {
                return new ClientJoinExpression(projection, outerKey, innerKey);
            }
            return join;
        }

        protected override Expression VisitJoin(JoinExpression join)
        {
            if (join.Join == JoinType.SingletonLeftOuter)
            {
                // first visit right side w/o looking at condition
                Expression right = Visit(join.Right);
                var ax = right as AliasedExpression;
                if (ax != null && !allColumnsUsed.ContainsKey(ax.Alias))
                {
                    // if nothing references the alias on the right, then the join is redundant
                    return Visit(join.Left);
                }
                // otherwise do it the right way
                Expression cond = Visit(join.Condition);
                Expression left = Visit(join.Left);
                right = Visit(join.Right);
                return UpdateJoin(join, join.Join, left, right, cond);
            }
            else
            {
                // visit join in reverse order
                Expression condition = Visit(join.Condition);
                Expression right = VisitSource(join.Right);
                Expression left = VisitSource(join.Left);
                return UpdateJoin(join, join.Join, left, right, condition);
            }
        }
    }
}