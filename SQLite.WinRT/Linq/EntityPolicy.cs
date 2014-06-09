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

namespace SQLite.WinRT.Linq
{
    /// <summary>
	/// Defines query execution & materialization policies. 
	/// </summary>
	public class EntityPolicy
	{
		private readonly HashSet<MemberInfo> included = new HashSet<MemberInfo>();

		private readonly HashSet<MemberInfo> deferred = new HashSet<MemberInfo>();

		private readonly Dictionary<MemberInfo, List<LambdaExpression>> operations =
			new Dictionary<MemberInfo, List<LambdaExpression>>();

		public static readonly EntityPolicy Default = new EntityPolicy();

		public void Include(MemberInfo member)
		{
			this.Include(member, false);
		}

		public void Include(MemberInfo member, bool deferLoad)
		{
			this.included.Add(member);
			if (deferLoad)
			{
				this.Defer(member);
			}
		}

		public void IncludeWith(LambdaExpression fnMember)
		{
			this.IncludeWith(fnMember, false);
		}

		public void IncludeWith(LambdaExpression fnMember, bool deferLoad)
		{
			var rootMember = RootMemberFinder.Find(fnMember, fnMember.Parameters[0]);
			if (rootMember == null)
			{
				throw new InvalidOperationException("Subquery does not originate with a member access");
			}
			this.Include(rootMember.Member, deferLoad);
			if (rootMember != fnMember.Body)
			{
				this.AssociateWith(fnMember);
			}
		}

		public void IncludeWith<TEntity>(Expression<Func<TEntity, object>> fnMember)
		{
			this.IncludeWith((LambdaExpression)fnMember, false);
		}

		public void IncludeWith<TEntity>(Expression<Func<TEntity, object>> fnMember, bool deferLoad)
		{
			this.IncludeWith((LambdaExpression)fnMember, deferLoad);
		}

		private void Defer(MemberInfo member)
		{
			Type mType = TypeHelper.GetMemberType(member);
			if (mType.GetTypeInfo().IsGenericType)
			{
				var gType = mType.GetGenericTypeDefinition();
				if (gType != typeof(IEnumerable<>) && gType != typeof(IList<>) && !typeof(IDeferLoadable).IsAssignableFrom(mType))
				{
					throw new InvalidOperationException(string.Format("The member '{0}' cannot be deferred due to its type.", member));
				}
			}
			this.deferred.Add(member);
		}

		public void AssociateWith(LambdaExpression memberQuery)
		{
			var rootMember = RootMemberFinder.Find(memberQuery, memberQuery.Parameters[0]);
			if (rootMember == null)
			{
				throw new InvalidOperationException("Subquery does not originate with a member access");
			}
			if (rootMember != memberQuery.Body)
			{
				var memberParam = Expression.Parameter(rootMember.Type, "root");
				var newBody = ExpressionReplacer.Replace(memberQuery.Body, rootMember, memberParam);
				this.AddOperation(rootMember.Member, Expression.Lambda(newBody, memberParam));
			}
		}

		private void AddOperation(MemberInfo member, LambdaExpression operation)
		{
			List<LambdaExpression> memberOps;
			if (!this.operations.TryGetValue(member, out memberOps))
			{
				memberOps = new List<LambdaExpression>();
				this.operations.Add(member, memberOps);
			}
			memberOps.Add(operation);
		}

		public void AssociateWith<TEntity>(Expression<Func<TEntity, IEnumerable>> memberQuery)
		{
			this.AssociateWith((LambdaExpression)memberQuery);
		}

		private class RootMemberFinder : Base.ExpressionVisitor
		{
			private MemberExpression found;

			private ParameterExpression parameter;

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
					this.Visit(m.Object);
				}
				else if (m.Arguments.Count > 0)
				{
					this.Visit(m.Arguments[0]);
				}
				return m;
			}

			protected override Expression VisitMemberAccess(MemberExpression m)
			{
				if (m.Expression == this.parameter)
				{
					this.found = m;
					return m;
				}
				else
				{
					return base.VisitMemberAccess(m);
				}
			}
		}

		/// <summary>
		/// Determines if a relationship property is to be included in the results of the query
		/// </summary>
		/// <param name="member"></param>
		/// <returns></returns>
		public bool IsIncluded(MemberInfo member)
		{
			return this.included.Contains(member);
		}

		/// <summary>
		/// Determines if a relationship property is included, but the query for the related data is 
		/// deferred until the property is first accessed.
		/// </summary>
		/// <param name="member"></param>
		/// <returns></returns>
		public bool IsDeferLoaded(MemberInfo member)
		{
			return this.deferred.Contains(member);
		}

		public QueryPolice CreatePolice(QueryTranslator translator)
		{
			return new QueryPolice(this, translator);
		}

		public class QueryPolice
		{
			public QueryPolice(EntityPolicy policy, QueryTranslator translator)
			{
				this.Policy = policy;
				this.Translator = translator;
			}

			public EntityPolicy Policy { get; private set; }

			public QueryTranslator Translator { get; private set; }

			public Expression ApplyPolicy(Expression expression, MemberInfo member)
			{
				List<LambdaExpression> ops;
				if (this.Policy.operations.TryGetValue(member, out ops))
				{
					var result = expression;
					foreach (var fnOp in ops)
					{
						var pop = PartialEvaluator.Eval(fnOp, this.Translator.Mapper.Mapping.CanBeEvaluatedLocally);
						result = this.Translator.Mapper.ApplyMapping(Expression.Invoke(pop, result));
					}
					var projection = (ProjectionExpression)result;
					if (projection.Type != expression.Type)
					{
						var fnAgg = Aggregator.GetAggregator(expression.Type, projection.Type);
						projection = new ProjectionExpression(projection.Select, projection.Projector, fnAgg);
					}
					return projection;
				}
				return expression;
			}

			/// <summary>
			/// Provides policy specific query translations.  This is where choices about inclusion of related objects and how
			/// heirarchies are materialized affect the definition of the queries.
			/// </summary>
			/// <param name="expression"></param>
			/// <returns></returns>
			public Expression Translate(Expression expression)
			{
				// add included relationships to client projection
				var rewritten = RelationshipIncluder.Include(this.Translator.Mapper, expression);
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
				rewritten = ClientJoinedProjectionRewriter.Rewrite(this.Policy, expression);
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
			/// Converts a query into an execution plan.  The plan is an function that executes the query and builds the
			/// resulting objects.
			/// </summary>
			public Expression BuildExecutionPlan(Expression query, Expression provider)
			{
				return ExecutionBuilder.Build(this.Policy, query, provider);
			}
		}
	}
}