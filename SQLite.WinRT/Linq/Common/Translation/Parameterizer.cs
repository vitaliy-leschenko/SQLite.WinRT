// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using SQLite.WinRT.Linq.Base;
using SQLite.WinRT.Linq.Common.Expressions;

namespace SQLite.WinRT.Linq.Common.Translation
{
    /// <summary>
    ///     Converts user arguments into named-value parameters
    /// </summary>
    public class Parameterizer : DbExpressionVisitor
    {
        private readonly Dictionary<TypeAndValue, NamedValueExpression> map =
            new Dictionary<TypeAndValue, NamedValueExpression>();

        private readonly Dictionary<HashedExpression, NamedValueExpression> pmap =
            new Dictionary<HashedExpression, NamedValueExpression>();

        private int iParam;

        private Parameterizer()
        {
        }

        public static Expression Parameterize(Expression expression)
        {
            return new Parameterizer().Visit(expression);
        }

        protected override Expression VisitProjection(ProjectionExpression proj)
        {
            // don't parameterize the projector or aggregator!
            var select = (SelectExpression) Visit(proj.Select);
            return UpdateProjection(proj, select, proj.Projector, proj.Aggregator);
        }

        protected override Expression VisitUnary(UnaryExpression u)
        {
            if (u.NodeType == ExpressionType.Convert && u.Operand.NodeType == ExpressionType.ArrayIndex)
            {
                var b = (BinaryExpression) u.Operand;
                if (IsConstantOrParameter(b.Left) && IsConstantOrParameter(b.Right))
                {
                    return GetNamedValue(u);
                }
            }
            return base.VisitUnary(u);
        }

        private static bool IsConstantOrParameter(Expression e)
        {
            return e != null && e.NodeType == ExpressionType.Constant || e.NodeType == ExpressionType.Parameter;
        }

        protected override Expression VisitBinary(BinaryExpression b)
        {
            Expression left = Visit(b.Left);
            Expression right = Visit(b.Right);
            if (left.NodeType == (ExpressionType) DbExpressionType.NamedValue
                && right.NodeType == (ExpressionType) DbExpressionType.Column)
            {
                var nv = (NamedValueExpression) left;
                var c = (ColumnExpression) right;
                left = new NamedValueExpression(nv.Name, c.QueryType, nv.Value);
            }
            else if (b.Right.NodeType == (ExpressionType) DbExpressionType.NamedValue
                     && b.Left.NodeType == (ExpressionType) DbExpressionType.Column)
            {
                var nv = (NamedValueExpression) right;
                var c = (ColumnExpression) left;
                right = new NamedValueExpression(nv.Name, c.QueryType, nv.Value);
            }
            return UpdateBinary(b, left, right, b.Conversion, b.IsLiftedToNull, b.Method);
        }

        protected override ColumnAssignment VisitColumnAssignment(ColumnAssignment ca)
        {
            ca = base.VisitColumnAssignment(ca);
            Expression expression = ca.Expression;
            var nv = expression as NamedValueExpression;
            if (nv != null)
            {
                expression = new NamedValueExpression(nv.Name, ca.Column.QueryType, nv.Value);
            }
            return UpdateColumnAssignment(ca, ca.Column, expression);
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            if (c.Value != null && !IsNumeric(c.Value.GetType()))
            {
                NamedValueExpression nv;
                var tv = new TypeAndValue(c.Type, c.Value);
                if (!map.TryGetValue(tv, out nv))
                {
                    // re-use same name-value if same type & value
                    string name = "p" + (iParam++);
                    nv = new NamedValueExpression(name, DbTypeSystem.GetColumnType(c.Type), c);
                    map.Add(tv, nv);
                }
                return nv;
            }
            return c;
        }

        protected override Expression VisitParameter(ParameterExpression p)
        {
            return GetNamedValue(p);
        }

        protected override Expression VisitMemberAccess(MemberExpression m)
        {
            m = (MemberExpression) base.VisitMemberAccess(m);
            var nv = m.Expression as NamedValueExpression;
            if (nv != null)
            {
                Expression x = Expression.MakeMemberAccess(nv.Value, m.Member);
                return GetNamedValue(x);
            }
            return m;
        }

        private Expression GetNamedValue(Expression e)
        {
            NamedValueExpression nv;
            var he = new HashedExpression(e);
            if (!pmap.TryGetValue(he, out nv))
            {
                string name = "p" + (iParam++);
                nv = new NamedValueExpression(name, DbTypeSystem.GetColumnType(e.Type), e);
                pmap.Add(he, nv);
            }
            return nv;
        }

        private bool IsNumeric(Type type)
        {
            switch (TypeHelper.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                case TypeCode.Byte:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
                default:
                    return false;
            }
        }

        private struct HashedExpression : IEquatable<HashedExpression>
        {
            private readonly Expression expression;

            private readonly int hashCode;

            public HashedExpression(Expression expression)
            {
                this.expression = expression;
                hashCode = Hasher.ComputeHash(expression);
            }

            public bool Equals(HashedExpression other)
            {
                return hashCode == other.hashCode && DbExpressionComparer.AreEqual(expression, other.expression);
            }

            public override bool Equals(object obj)
            {
                if (!(obj is HashedExpression))
                {
                    return false;
                }
                return Equals((HashedExpression) obj);
            }

            public override int GetHashCode()
            {
                return hashCode;
            }

            private class Hasher : DbExpressionVisitor
            {
                private int hc;

                internal static int ComputeHash(Expression expression)
                {
                    var hasher = new Hasher();
                    hasher.Visit(expression);
                    return hasher.hc;
                }

                protected override Expression VisitConstant(ConstantExpression c)
                {
                    hc = hc + ((c.Value != null) ? c.Value.GetHashCode() : 0);
                    return c;
                }
            }
        }

        private struct TypeAndValue : IEquatable<TypeAndValue>
        {
            private readonly int hash;
            private readonly Type type;

            private readonly object value;

            public TypeAndValue(Type type, object value)
            {
                this.type = type;
                this.value = value;
                hash = type.GetHashCode() + (value != null ? value.GetHashCode() : 0);
            }

            public bool Equals(TypeAndValue vt)
            {
                return vt.type == type && Equals(vt.value, value);
            }

            public override bool Equals(object obj)
            {
                if (!(obj is TypeAndValue))
                {
                    return false;
                }
                return Equals((TypeAndValue) obj);
            }

            public override int GetHashCode()
            {
                return hash;
            }
        }
    }
}