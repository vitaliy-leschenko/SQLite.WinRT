// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Linq.Expressions;
using SQLite.WinRT.Linq.Base;
using SQLite.WinRT.Linq.Common.Language;
using SQLite.WinRT.Linq.Common.Mapping;

namespace SQLite.WinRT.Linq.Common
{
	/// <summary>
	/// Defines query execution & materialization policies. 
	/// </summary>
	public class QueryTranslator
	{
		public QueryTranslator(QueryMapping mapping, EntityPolicy policy)
		{
			this.Mapper = mapping.CreateMapper(this);
			this.Police = policy.CreatePolice(this);
		}

		public QueryMapper Mapper { get; private set; }

		public EntityPolicy.QueryPolice Police { get; private set; }

		public Expression Translate(Expression expression)
		{
			// pre-evaluate local sub-trees
			expression = PartialEvaluator.Eval(expression, this.Mapper.Mapping.CanBeEvaluatedLocally);

			// apply mapping (binds LINQ operators too)
			expression = this.Mapper.Translate(expression);

			// any policy specific translations or validations
			expression = this.Police.Translate(expression);

			// any language specific translations or validations
			expression = QueryLinguist.Translate(expression);

			return expression;
		}
	}
}