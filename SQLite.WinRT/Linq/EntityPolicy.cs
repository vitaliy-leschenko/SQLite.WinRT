// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using SQLite.WinRT.Linq.Base;
using SQLite.WinRT.Linq.Common;
using SQLite.WinRT.Linq.Common.Expressions;
using SQLite.WinRT.Linq.Common.Translation;
using ExpressionVisitor = SQLite.WinRT.Linq.Base.ExpressionVisitor;

namespace SQLite.WinRT.Linq
{
    /// <summary>
    ///     Defines query execution & materialization policies.
    /// </summary>
    public class EntityPolicy
    {
        public static readonly EntityPolicy Default = new EntityPolicy();
        private readonly HashSet<MemberInfo> deferred = new HashSet<MemberInfo>();
        private readonly HashSet<MemberInfo> included = new HashSet<MemberInfo>();

        private readonly Dictionary<MemberInfo, List<LambdaExpression>> operations =
            new Dictionary<MemberInfo, List<LambdaExpression>>();

        public void Include(MemberInfo member)
        {
            Include(member, false);
        }

        public void Include(MemberInfo member, bool deferLoad)
        {
            included.Add(member);
            if (deferLoad)
            {
                Defer(member);
            }
        }

        public void IncludeWith(LambdaExpression fnMember)
        {
            IncludeWith(fnMember, false);
        }

        public void IncludeWith(LambdaExpression fnMember, bool deferLoad)
        {
            MemberExpression rootMember = RootMemberFinder.Find(fnMember, fnMember.Parameters[0]);
            if (rootMember == null)
            {
                throw new InvalidOperationException("Subquery does not originate with a member access");
            }
            Include(rootMember.Member, deferLoad);
            if (rootMember != fnMember.Body)
            {
                AssociateWith(fnMember);
            }
        }

        public void IncludeWith<TEntity>(Expression<Func<TEntity, object>> fnMember)
        {
            IncludeWith((LambdaExpression) fnMember, false);
        }

        public void IncludeWith<TEntity>(Expression<Func<TEntity, object>> fnMember, bool deferLoad)
        {
            IncludeWith((LambdaExpression) fnMember, deferLoad);
        }

        private void Defer(MemberInfo member)
        {
            Type mType = TypeHelper.GetMemberType(member);
            if (mType.GetTypeInfo().IsGenericType)
            {
                Type gType = mType.GetGenericTypeDefinition();
                if (gType != typeof (IEnumerable<>) && gType != typeof (IList<>) &&
                    !typeof (IDeferLoadable).IsAssignableFrom(mType))
                {
                    throw new InvalidOperationException(
                        string.Format("The member '{0}' cannot be deferred due to its type.", member));
                }
            }
            deferred.Add(member);
        }

        public void AssociateWith(LambdaExpression memberQuery)
        {
            MemberExpression rootMember = RootMemberFinder.Find(memberQuery, memberQuery.Parameters[0]);
            if (rootMember == null)
            {
                throw new InvalidOperationException("Subquery does not originate with a member access");
            }
            if (rootMember != memberQuery.Body)
            {
                ParameterExpression memberParam = Expression.Parameter(rootMember.Type, "root");
                Expression newBody = ExpressionReplacer.Replace(memberQuery.Body, rootMember, memberParam);
                AddOperation(rootMember.Member, Expression.Lambda(newBody, memberParam));
            }
        }

        private void AddOperation(MemberInfo member, LambdaExpression operation)
        {
            List<LambdaExpression> memberOps;
            if (!operations.TryGetValue(member, out memberOps))
            {
                memberOps = new List<LambdaExpression>();
                operations.Add(member, memberOps);
            }
            memberOps.Add(operation);
        }

        public void AssociateWith<TEntity>(Expression<Func<TEntity, IEnumerable>> memberQuery)
        {
            AssociateWith((LambdaExpression) memberQuery);
        }

        /// <summary>
        ///     Determines if a relationship property is to be included in the results of the query
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public bool IsIncluded(MemberInfo member)
        {
            return included.Contains(member);
        }

        /// <summary>
        ///     Determines if a relationship property is included, but the query for the related data is
        ///     deferred until the property is first accessed.
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public bool IsDeferLoaded(MemberInfo member)
        {
            return deferred.Contains(member);
        }

        public QueryPolice CreatePolice(QueryTranslator translator)
        {
            return new QueryPolice(this, translator);
        }

        public class QueryPolice
        {
            public QueryPolice(EntityPolicy policy, QueryTranslator translator)
            {
                Policy = policy;
                Translator = translator;
            }

            public EntityPolicy Policy { get; private set; }

            public QueryTranslator Translator { get; private set; }

            public Expression ApplyPolicy(Expression expression, MemberInfo member)
            {
                List<LambdaExpression> ops;
                if (Policy.operations.TryGetValue(member, out ops))
                {
                    Expression result = expression;
                    foreach (LambdaExpression fnOp in ops)
                    {
                        Expression pop = PartialEvaluator.Eval(fnOp, Translator.Mapper.Mapping.CanBeEvaluatedLocally);
                        result = Translator.Mapper.ApplyMapping(Expression.Invoke(pop, result));
                    }
                    var projection = (ProjectionExpression) result;
                    if (projection.Type != expression.Type)
                    {
                        LambdaExpression fnAgg = Aggregator.GetAggregator(expression.Type, projection.Type);
                        projection = new ProjectionExpression(projection.Select, projection.Projector, fnAgg);
                    }
                    return projection;
                }
                return expression;
            }

            /// <summary>
            ///     Provides policy specific query translations.  This is where choices about inclusion of related objects and how
            ///     heirarchies are materialized affect the definition of the queries.
            /// </summary>
            /// <param name="expression"></param>
            /// <returns></returns>
            public Expression Translate(Expression expression)
            {
                // add included relationships to client projection
                Expression rewritten = RelationshipIncluder.Include(Translator.Mapper, expression);
                if (rewritten != expression)
                {
                    expression = rewritten;
                    expression = UnusedColumnRemover.Remove(expression);
                    expression = RedundantColumnRemover.Remove(expression);
                    expression = RedundantSubqueryRemover.Remove(expression);
                    expression = RedundantJoinRemover.Remove(expression);
                }

                // convert any singleton (1:1 or n:1) projections into server-side joins (cardinality is preserved)
                rewritten = SingletonProjectionRewriter.Rewrite(expression);
                if (rewritten != expression)
                {
                    expression = rewritten;
                    expression = UnusedColumnRemover.Remove(expression);
                    expression = RedundantColumnRemover.Remove(expression);
                    expression = RedundantSubqueryRemover.Remove(expression);
                    expression = RedundantJoinRemover.Remove(expression);
                }

                // convert projections into client-side joins
                rewritten = ClientJoinedProjectionRewriter.Rewrite(Policy, expression);
                if (rewritten != expression)
                {
                    expression = rewritten;
                    expression = UnusedColumnRemover.Remove(expression);
                    expression = RedundantColumnRemover.Remove(expression);
                    expression = RedundantSubqueryRemover.Remove(expression);
                    expression = RedundantJoinRemover.Remove(expression);
                }

                return expression;
            }

            /// <summary>
            ///     Converts a query into an execution plan.  The plan is an function that executes the query and builds the
            ///     resulting objects.
            /// </summary>
            public Expression BuildExecutionPlan(Expression query, Expression provider)
            {
                return ExecutionBuilder.Build(Policy, query, provider);
            }
        }

        private class RootMemberFinder : ExpressionVisitor
        {
            private readonly ParameterExpression parameter;
            private MemberExpression found;

            private RootMemberFinder(ParameterExpression parameter)
            {
                this.parameter = parameter;
            }

            public static MemberExpression Find(Expression query, ParameterExpression parameter)
            {
                var finder = new RootMemberFinder(parameter);
                finder.Visit(query);
                return finder.found;
            }

            protected override Expression VisitMethodCall(MethodCallExpression m)
            {
                if (m.Object != null)
                {
                    Visit(m.Object);
                }
                else if (m.Arguments.Count > 0)
                {
                    Visit(m.Arguments[0]);
                }
                return m;
            }

            protected override Expression VisitMemberAccess(MemberExpression m)
            {
                if (m.Expression == parameter)
                {
                    found = m;
                    return m;
                }
                return base.VisitMemberAccess(m);
            }
        }
    }
}