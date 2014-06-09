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
    ///     Attempt to rewrite cross joins as inner joins
    /// </summary>
    public class CrossJoinRewriter : DbExpressionVisitor
    {
        private Expression currentWhere;

        public static Expression Rewrite(Expression expression)
        {
            return new CrossJoinRewriter().Visit(expression);
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            Expression saveWhere = currentWhere;
            try
            {
                currentWhere = select.Where;
                var result = (SelectExpression) base.VisitSelect(select);
                if (currentWhere != result.Where)
                {
                    return result.SetWhere(currentWhere);
                }
                return result;
            }
            finally
            {
                currentWhere = saveWhere;
            }
        }

        protected override Expression VisitJoin(JoinExpression join)
        {
            join = (JoinExpression) base.VisitJoin(join);
            if (join.Join == JoinType.CrossJoin && currentWhere != null)
            {
                // try to figure out which parts of the current where expression can be used for a join condition
                HashSet<TableAlias> declaredLeft = DeclaredAliasGatherer.Gather(join.Left);
                HashSet<TableAlias> declaredRight = DeclaredAliasGatherer.Gather(join.Right);
                var declared = new HashSet<TableAlias>(declaredLeft.Union(declaredRight));
                Expression[] exprs = currentWhere.Split(ExpressionType.And, ExpressionType.AndAlso);
                List<Expression> good =
                    exprs.Where(e => CanBeJoinCondition(e, declaredLeft, declaredRight, declared)).ToList();
                if (good.Count > 0)
                {
                    Expression condition = good.Join(ExpressionType.And);
                    join = UpdateJoin(join, JoinType.InnerJoin, join.Left, join.Right, condition);
                    Expression newWhere = exprs.Where(e => !good.Contains(e)).Join(ExpressionType.And);
                    currentWhere = newWhere;
                }
            }
            return join;
        }

        private bool CanBeJoinCondition(
            Expression expression, HashSet<TableAlias> left, HashSet<TableAlias> right, HashSet<TableAlias> all)
        {
            // an expression is good if it has at least one reference to an alias from both left & right sets and does
            // not have any additional references that are not in both left & right sets
            HashSet<TableAlias> referenced = ReferencedAliasGatherer.Gather(expression);
            bool leftOkay = referenced.Intersect(left).Any();
            bool rightOkay = referenced.Intersect(right).Any();
            bool subset = referenced.IsSubsetOf(all);
            return leftOkay && rightOkay && subset;
        }
    }
}