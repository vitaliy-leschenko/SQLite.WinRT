// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using SQLite.WinRT.Linq.Base;
using SQLite.WinRT.Linq.Common.Expressions;

namespace SQLite.WinRT.Linq.Common.Translation
{
    /// <summary>
    ///     Rewrites queries with skip & take to use the nested queries with inverted ordering technique
    /// </summary>
    public class SkipToNestedOrderByRewriter : DbExpressionVisitor
    {
        private SkipToNestedOrderByRewriter()
        {
        }

        public static Expression Rewrite(Expression expression)
        {
            return new SkipToNestedOrderByRewriter().Visit(expression);
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            // select * from table order by x skip s take t 
            // =>
            // select * from (select top s * from (select top s + t from table order by x) order by -x) order by x

            select = (SelectExpression) base.VisitSelect(select);

            if (select.Skip != null && select.Take != null && select.OrderBy.Count > 0)
            {
                Expression skip = select.Skip;
                Expression take = select.Take;
                Expression skipPlusTake = PartialEvaluator.Eval(Expression.Add(skip, take));

                select = select.SetTake(skipPlusTake).SetSkip(null);
                select = select.AddRedundantSelect(new TableAlias());
                select = select.SetTake(take);

                // propogate order-bys to new layer
                select = (SelectExpression) OrderByRewriter.Rewrite(select);
                IEnumerable<OrderExpression> inverted =
                    select.OrderBy.Select(
                        ob =>
                            new OrderExpression(
                                ob.OrderType == OrderType.Ascending ? OrderType.Descending : OrderType.Ascending,
                                ob.Expression));
                select = select.SetOrderBy(inverted);

                select = select.AddRedundantSelect(new TableAlias());
                select = select.SetTake(Expression.Constant(0)); // temporary
                select = (SelectExpression) OrderByRewriter.Rewrite(select);
                IEnumerable<OrderExpression> reverted =
                    select.OrderBy.Select(
                        ob =>
                            new OrderExpression(
                                ob.OrderType == OrderType.Ascending ? OrderType.Descending : OrderType.Ascending,
                                ob.Expression));
                select = select.SetOrderBy(reverted);
                select = select.SetTake(null);
            }

            return select;
        }
    }
}