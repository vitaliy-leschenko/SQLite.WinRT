﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using SQLite.WinRT.Linq.Base;
using SQLite.WinRT.Linq.Common.Expressions;

namespace SQLite.WinRT.Linq.Common.Translation
{
	public class NamedValueGatherer : DbExpressionVisitor
	{
		private HashSet<NamedValueExpression> namedValues = new HashSet<NamedValueExpression>(new NamedValueComparer());

		private NamedValueGatherer()
		{
		}

		public static ReadOnlyCollection<NamedValueExpression> Gather(Expression expr)
		{
			NamedValueGatherer gatherer = new NamedValueGatherer();
			gatherer.Visit(expr);
			return gatherer.namedValues.ToList().AsReadOnly();
		}

		protected override Expression VisitNamedValue(NamedValueExpression value)
		{
			this.namedValues.Add(value);
			return value;
		}

		private class NamedValueComparer : IEqualityComparer<NamedValueExpression>
		{
			public bool Equals(NamedValueExpression x, NamedValueExpression y)
			{
				return x.Name == y.Name;
			}

			public int GetHashCode(NamedValueExpression obj)
			{
				return obj.Name.GetHashCode();
			}
		}
	}
}