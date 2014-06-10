// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using SQLite.WinRT.Linq.Common.Expressions;

namespace SQLite.WinRT.Linq.Common.Translation
{
    /// <summary>
    ///     Rewrite aggregate expressions, moving them into same select expression that has the group-by clause
    /// </summary>
    public class AggregateRewriter : DbExpressionVisitor
    {
        private readonly ILookup<TableAlias, AggregateSubqueryExpression> lookup;

        private readonly Dictionary<AggregateSubqueryExpression, Expression> map;

        private AggregateRewriter(Expression expr)
        {
            map = new Dictionary<AggregateSubqueryExpression, Expression>();
            lookup = AggregateGatherer.Gather(expr).ToLookup(a => a.GroupByAlias);
        }

        public static Expression Rewrite(Expression expr)
        {
            return new AggregateRewriter(expr).Visit(expr);
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            select = (SelectExpression) base.VisitSelect(select);
            if (lookup.Contains(select.Alias))
            {
                var aggColumns = new List<ColumnDeclaration>(select.Columns);
                foreach (AggregateSubqueryExpression ae in lookup[select.Alias])
                {
                    string name = "agg" + aggColumns.Count;
                    DbQueryType colType = DbTypeSystem.GetColumnType(ae.Type);
                    var cd = new ColumnDeclaration(name, ae.AggregateInGroupSelect, colType);
                    map.Add(ae, new ColumnExpression(ae.Type, colType, ae.GroupByAlias, name));
                    aggColumns.Add(cd);
                }
                return new SelectExpression(
                    select.Alias,
                    aggColumns,
                    select.From,
                    select.Where,
                    select.OrderBy,
                    select.GroupBy,
                    select.IsDistinct,
                    select.Skip,
                    select.Take,
                    select.IsReverse);
            }
            return select;
        }

        protected override Expression VisitAggregateSubquery(AggregateSubqueryExpression aggregate)
        {
            Expression mapped;
            if (map.TryGetValue(aggregate, out mapped))
            {
                return mapped;
            }
            return Visit(aggregate.AggregateAsSubquery);
        }

        private class AggregateGatherer : DbExpressionVisitor
        {
            private readonly List<AggregateSubqueryExpression> aggregates = new List<AggregateSubqueryExpression>();

            private AggregateGatherer()
            {
            }

            internal static List<AggregateSubqueryExpression> Gather(Expression expression)
            {
                var gatherer = new AggregateGatherer();
                gatherer.Visit(expression);
                return gatherer.aggregates;
            }

            protected override Expression VisitAggregateSubquery(AggregateSubqueryExpression aggregate)
            {
                aggregates.Add(aggregate);
                return base.VisitAggregateSubquery(aggregate);
            }
        }
    }
}