// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using SQLite.WinRT.Linq.Base;
using SQLite.WinRT.Linq.Common.Expressions;

namespace SQLite.WinRT.Linq.Common.Translation
{
    public class NamedValueGatherer : DbExpressionVisitor
    {
        private readonly HashSet<NamedValueExpression> namedValues =
            new HashSet<NamedValueExpression>(new NamedValueComparer());

        private NamedValueGatherer()
        {
        }

        public static ReadOnlyCollection<NamedValueExpression> Gather(Expression expr)
        {
            var gatherer = new NamedValueGatherer();
            gatherer.Visit(expr);
            return gatherer.namedValues.ToList().AsReadOnly();
        }

        protected override Expression VisitNamedValue(NamedValueExpression value)
        {
            var constant = value.Value as ConstantExpression;
            if (constant != null)
            {
                var type = constant.Value.GetType();
                var elementType = type.GetElementType() ?? (type.GenericTypeArguments.Length == 1 ? type.GenericTypeArguments[0] : null);
                if (elementType != null)
                {
                    var collection = typeof(ICollection<>).MakeGenericType(elementType);
                    if (collection.IsAssignableFrom(type))
                    {
                        return value;
                    }
                }
            }

            namedValues.Add(value);
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