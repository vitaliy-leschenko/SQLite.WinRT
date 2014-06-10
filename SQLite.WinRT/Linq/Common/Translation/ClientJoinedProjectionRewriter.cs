// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using SQLite.WinRT.Linq.Base;
using SQLite.WinRT.Linq.Common.Expressions;
using SQLite.WinRT.Linq.Common.Language;

namespace SQLite.WinRT.Linq.Common.Translation
{
    /// <summary>
    ///     rewrites nested projections into client-side joins
    /// </summary>
    public class ClientJoinedProjectionRewriter : DbExpressionVisitor
    {
        private readonly EntityPolicy policy;

        private bool canJoinOnClient = true;
        private MemberInfo currentMember;
        private SelectExpression currentSelect;
        private bool isTopLevel = true;

        private ClientJoinedProjectionRewriter(EntityPolicy policy)
        {
            this.policy = policy;
        }

        public static Expression Rewrite(EntityPolicy policy, Expression expression)
        {
            return new ClientJoinedProjectionRewriter(policy).Visit(expression);
        }

        protected override MemberAssignment VisitMemberAssignment(MemberAssignment assignment)
        {
            MemberInfo saveMember = currentMember;
            currentMember = assignment.Member;
            Expression e = Visit(assignment.Expression);
            currentMember = saveMember;
            return UpdateMemberAssignment(assignment, assignment.Member, e);
        }

        protected override Expression VisitMemberAndExpression(MemberInfo member, Expression expression)
        {
            MemberInfo saveMember = currentMember;
            currentMember = member;
            Expression e = Visit(expression);
            currentMember = saveMember;
            return e;
        }

        protected override Expression VisitProjection(ProjectionExpression proj)
        {
            SelectExpression save = currentSelect;
            currentSelect = proj.Select;
            try
            {
                if (!isTopLevel)
                {
                    if (CanJoinOnClient(currentSelect))
                    {
                        // make a query that combines all the constraints from the outer queries into a single select
                        var newOuterSelect = (SelectExpression) QueryDuplicator.Duplicate(save);

                        // remap any references to the outer select to the new alias;
                        var newInnerSelect =
                            (SelectExpression) ColumnMapper.Map(proj.Select, newOuterSelect.Alias, save.Alias);
                        // add outer-join test
                        ProjectionExpression newInnerProjection =
                            QueryLanguage.AddOuterJoinTest(new ProjectionExpression(newInnerSelect, proj.Projector));
                        newInnerSelect = newInnerProjection.Select;
                        Expression newProjector = newInnerProjection.Projector;

                        var newAlias = new TableAlias();
                        ProjectedColumns pc = ColumnProjector.ProjectColumns(newProjector, null, newAlias,
                            newOuterSelect.Alias, newInnerSelect.Alias);

                        var join = new JoinExpression(JoinType.OuterApply, newOuterSelect, newInnerSelect, null);
                        var joinedSelect = new SelectExpression(
                            newAlias, pc.Columns, join, null, null, null, proj.IsSingleton, null, null, false);

                        // apply client-join treatment recursively
                        currentSelect = joinedSelect;
                        newProjector = Visit(pc.Projector);

                        // compute keys (this only works if join condition was a single column comparison)
                        var outerKeys = new List<Expression>();
                        var innerKeys = new List<Expression>();
                        if (GetEquiJoinKeyExpressions(newInnerSelect.Where, newOuterSelect.Alias, outerKeys, innerKeys))
                        {
                            // outerKey needs to refer to the outer-scope's alias
                            IEnumerable<Expression> outerKey =
                                outerKeys.Select(k => ColumnMapper.Map(k, save.Alias, newOuterSelect.Alias));
                            // innerKey needs to refer to the new alias for the select with the new join
                            IEnumerable<Expression> innerKey =
                                innerKeys.Select(
                                    k => ColumnMapper.Map(k, joinedSelect.Alias, ((ColumnExpression) k).Alias));
                            var newProjection = new ProjectionExpression(joinedSelect, newProjector, proj.Aggregator);
                            return new ClientJoinExpression(newProjection, outerKey, innerKey);
                        }
                    }
                    else
                    {
                        bool saveJoin = canJoinOnClient;
                        canJoinOnClient = false;
                        Expression result = base.VisitProjection(proj);
                        canJoinOnClient = saveJoin;
                        return result;
                    }
                }
                else
                {
                    isTopLevel = false;
                }
                return base.VisitProjection(proj);
            }
            finally
            {
                currentSelect = save;
            }
        }

        private bool CanJoinOnClient(SelectExpression select)
        {
            // can add singleton (1:0,1) join if no grouping/aggregates or distinct
            return canJoinOnClient && currentMember != null && !policy.IsDeferLoaded(currentMember)
                   && !select.IsDistinct && (select.GroupBy == null || select.GroupBy.Count == 0)
                   && !AggregateChecker.HasAggregates(select);
        }

        private bool GetEquiJoinKeyExpressions(
            Expression predicate, TableAlias outerAlias, List<Expression> outerExpressions,
            List<Expression> innerExpressions)
        {
            if (predicate.NodeType == ExpressionType.Equal)
            {
                var b = (BinaryExpression) predicate;
                ColumnExpression leftCol = GetColumnExpression(b.Left);
                ColumnExpression rightCol = GetColumnExpression(b.Right);
                if (leftCol != null && rightCol != null)
                {
                    if (leftCol.Alias == outerAlias)
                    {
                        outerExpressions.Add(b.Left);
                        innerExpressions.Add(b.Right);
                        return true;
                    }
                    if (rightCol.Alias == outerAlias)
                    {
                        innerExpressions.Add(b.Left);
                        outerExpressions.Add(b.Right);
                        return true;
                    }
                }
            }

            bool hadKey = false;
            Expression[] parts = predicate.Split(ExpressionType.And, ExpressionType.AndAlso);
            if (parts.Length > 1)
            {
                foreach (Expression part in parts)
                {
                    bool hasOuterAliasReference = ReferencedAliasGatherer.Gather(part).Contains(outerAlias);
                    if (hasOuterAliasReference)
                    {
                        if (!GetEquiJoinKeyExpressions(part, outerAlias, outerExpressions, innerExpressions))
                        {
                            return false;
                        }
                        hadKey = true;
                    }
                }
            }

            return hadKey;
        }

        private ColumnExpression GetColumnExpression(Expression expression)
        {
            // ignore converions 
            while (expression.NodeType == ExpressionType.Convert || expression.NodeType == ExpressionType.ConvertChecked)
            {
                expression = ((UnaryExpression) expression).Operand;
            }
            return expression as ColumnExpression;
        }

        protected override Expression VisitSubquery(SubqueryExpression subquery)
        {
            return subquery;
        }

        protected override Expression VisitCommand(CommandExpression command)
        {
            isTopLevel = true;
            return base.VisitCommand(command);
        }
    }
}