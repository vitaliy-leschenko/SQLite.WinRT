// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using SQLite.WinRT.Linq.Base;
using SQLite.WinRT.Linq.Common.Expressions;

namespace SQLite.WinRT.Linq.Common.Language
{
    /// <summary>
    ///     Formats a query expression into common SQL language syntax
    /// </summary>
    public class SqlFormatter : DbExpressionVisitor
    {
        private readonly Dictionary<TableAlias, string> aliases;
        private readonly bool forDebug;
        private readonly StringBuilder sb;

        private int depth;
        private int indent = 2;

        private bool isNested;

        private SqlFormatter(bool forDebug)
        {
            sb = new StringBuilder();
            aliases = new Dictionary<TableAlias, string>();
            this.forDebug = forDebug;
        }

        private SqlFormatter()
            : this(false)
        {
        }

        private bool HideColumnAliases { get; set; }

        private bool HideTableAliases { get; set; }

        private bool IsNested
        {
            get { return isNested; }
            set { isNested = value; }
        }

        private bool ForDebug
        {
            get { return forDebug; }
        }

        public int IndentationWidth
        {
            get { return indent; }
            set { indent = value; }
        }

        public static string Format(Expression expression, bool forDebug)
        {
            var formatter = new SqlFormatter(forDebug);
            formatter.Visit(expression);
            return formatter.ToString();
        }

        public static string Format(Expression expression)
        {
            var formatter = new SqlFormatter();
            formatter.Visit(expression);
            return formatter.ToString();
        }

        public override string ToString()
        {
            return sb.ToString();
        }

        private void Write(object value)
        {
            sb.Append(value);
        }

        private void WriteParameterName(string name)
        {
            Write("@" + name);
        }

        private void WriteVariableName(string name)
        {
            WriteParameterName(name);
        }

        private void WriteAsAliasName(string aliasName)
        {
            Write("AS ");
            WriteAliasName(aliasName);
        }

        private void WriteAliasName(string aliasName)
        {
            Write(aliasName);
        }

        private void WriteAsColumnName(string columnName)
        {
            Write("AS ");
            WriteColumnName(columnName);
        }

        private void WriteColumnName(string columnName)
        {
            string name = QueryLanguage.Quote(columnName);
            Write(name);
        }

        private void WriteTableName(string tableName)
        {
            string name = QueryLanguage.Quote(tableName);
            Write(name);
        }

        private void WriteLine(Indentation style)
        {
            sb.AppendLine();
            Indent(style);
            for (int i = 0, n = depth * indent; i < n; i++)
            {
                Write(" ");
            }
        }

        private void Indent(Indentation style)
        {
            if (style == Indentation.Inner)
            {
                depth++;
            }
            else if (style == Indentation.Outer)
            {
                depth--;
                Debug.Assert(depth >= 0);
            }
        }

        private string GetAliasName(TableAlias alias)
        {
            string name;
            if (!aliases.TryGetValue(alias, out name))
            {
                name = "ut" + aliases.Count;
                aliases.Add(alias, name);
            }
            return name;
        }

        private void AddAlias(TableAlias alias)
        {
            string name;
            if (!aliases.TryGetValue(alias, out name))
            {
                name = "t" + aliases.Count;
                aliases.Add(alias, name);
            }
        }

        private void AddAliases(Expression expr)
        {
            var ax = expr as AliasedExpression;
            if (ax != null)
            {
                AddAlias(ax.Alias);
            }
            else
            {
                var jx = expr as JoinExpression;
                if (jx != null)
                {
                    AddAliases(jx.Left);
                    AddAliases(jx.Right);
                }
            }
        }

        protected override Expression Visit(Expression exp)
        {
            if (exp == null)
            {
                return null;
            }

            // check for supported node types first 
            // non-supported ones should not be visited (as they would produce bad SQL)
            switch (exp.NodeType)
            {
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                case ExpressionType.Not:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.UnaryPlus:
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                case ExpressionType.Divide:
                case ExpressionType.Modulo:
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.Coalesce:
                case ExpressionType.RightShift:
                case ExpressionType.LeftShift:
                case ExpressionType.ExclusiveOr:
                case ExpressionType.Power:
                case ExpressionType.Conditional:
                case ExpressionType.Constant:
                case ExpressionType.MemberAccess:
                case ExpressionType.Call:
                case ExpressionType.New:
                case (ExpressionType) DbExpressionType.Table:
                case (ExpressionType) DbExpressionType.Column:
                case (ExpressionType) DbExpressionType.Select:
                case (ExpressionType) DbExpressionType.Join:
                case (ExpressionType) DbExpressionType.Aggregate:
                case (ExpressionType) DbExpressionType.Scalar:
                case (ExpressionType) DbExpressionType.Exists:
                case (ExpressionType) DbExpressionType.In:
                case (ExpressionType) DbExpressionType.AggregateSubquery:
                case (ExpressionType) DbExpressionType.IsNull:
                case (ExpressionType) DbExpressionType.Between:
                case (ExpressionType) DbExpressionType.RowCount:
                case (ExpressionType) DbExpressionType.Projection:
                case (ExpressionType) DbExpressionType.NamedValue:
                case (ExpressionType) DbExpressionType.Insert:
                case (ExpressionType) DbExpressionType.Update:
                case (ExpressionType) DbExpressionType.Delete:
                case (ExpressionType) DbExpressionType.Block:
                case (ExpressionType) DbExpressionType.If:
                case (ExpressionType) DbExpressionType.Declaration:
                case (ExpressionType) DbExpressionType.Variable:
                case (ExpressionType) DbExpressionType.Function:
                    return base.Visit(exp);

                case ExpressionType.ArrayLength:
                case ExpressionType.Quote:
                case ExpressionType.TypeAs:
                case ExpressionType.ArrayIndex:
                case ExpressionType.TypeIs:
                case ExpressionType.Parameter:
                case ExpressionType.Lambda:
                case ExpressionType.NewArrayInit:
                case ExpressionType.NewArrayBounds:
                case ExpressionType.Invoke:
                case ExpressionType.MemberInit:
                case ExpressionType.ListInit:
                default:
                    if (!forDebug)
                    {
                        throw new NotSupportedException(
                            string.Format("The LINQ expression node of type {0} is not supported", exp.NodeType));
                    }
                    Write(string.Format("?{0}?(", exp.NodeType));
                    base.Visit(exp);
                    Write(")");
                    return exp;
            }
        }

        protected override Expression VisitMemberAccess(MemberExpression m)
        {
            if (m.Member.DeclaringType == typeof (string))
            {
                switch (m.Member.Name)
                {
                    case "Length":
                        Write("LENGTH(");
                        Visit(m.Expression);
                        Write(")");
                        return m;
                }
            }
            else if (m.Member.DeclaringType == typeof (DateTime) || m.Member.DeclaringType == typeof (DateTimeOffset))
            {
                switch (m.Member.Name)
                {
                    case "Day":
                        Write("STRFTIME('%d', ");
                        Visit(m.Expression);
                        Write(")");
                        return m;
                    case "Month":
                        Write("STRFTIME('%m', ");
                        Visit(m.Expression);
                        Write(")");
                        return m;
                    case "Year":
                        Write("STRFTIME('%Y', ");
                        Visit(m.Expression);
                        Write(")");
                        return m;
                    case "Hour":
                        Write("STRFTIME('%H', ");
                        Visit(m.Expression);
                        Write(")");
                        return m;
                    case "Minute":
                        Write("STRFTIME('%M', ");
                        Visit(m.Expression);
                        Write(")");
                        return m;
                    case "Second":
                        Write("STRFTIME('%S', ");
                        Visit(m.Expression);
                        Write(")");
                        return m;
                    case "Millisecond":
                        Write("STRFTIME('%f', ");
                        Visit(m.Expression);
                        Write(")");
                        return m;
                    case "DayOfWeek":
                        Write("STRFTIME('%w', ");
                        Visit(m.Expression);
                        Write(")");
                        return m;
                    case "DayOfYear":
                        Write("(STRFTIME('%j', ");
                        Visit(m.Expression);
                        Write(") - 1)");
                        return m;
                }
            }

            if (forDebug)
            {
                Visit(m.Expression);
                Write(".");
                Write(m.Member.Name);
                return m;
            }
            throw new NotSupportedException(string.Format("The member access '{0}' is not supported", m.Member));
        }

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (m.Method.DeclaringType == typeof (string))
            {
                switch (m.Method.Name)
                {
                    case "StartsWith":
                        Write("Like(");
                        Visit(m.Arguments[0]);
                        Write(" || '%', ");
                        Visit(m.Object);
                        Write(")");
                        return m;
                    case "EndsWith":
                        Write("Like('%'+");
                        Visit(m.Arguments[0]);
                        Write(", ");
                        Visit(m.Object);
                        Write(")");
                        return m;
                    case "Contains":
                        Write("Like('%'||");
                        Visit(m.Arguments[0]);
                        Write("||'%', ");
                        Visit(m.Object);
                        Write(")");
                        return m;
                    case "Concat":
                        IList<Expression> args = m.Arguments;
                        if (args.Count == 1 && args[0].NodeType == ExpressionType.NewArrayInit)
                        {
                            args = ((NewArrayExpression) args[0]).Expressions;
                        }
                        for (int i = 0, n = args.Count; i < n; i++)
                        {
                            if (i > 0)
                            {
                                Write(" || ");
                            }
                            Visit(args[i]);
                        }
                        return m;
                    case "IsNullOrEmpty":
                        Write("(");
                        Visit(m.Arguments[0]);
                        Write(" IS NULL OR ");
                        Visit(m.Arguments[0]);
                        Write(" = '')");
                        return m;
                    case "ToUpper":
                        Write("UPPER(");
                        Visit(m.Object);
                        Write(")");
                        return m;
                    case "ToLower":
                        Write("LOWER(");
                        Visit(m.Object);
                        Write(")");
                        return m;
                    case "Replace":
                        Write("REPLACE(");
                        Visit(m.Object);
                        Write(", ");
                        Visit(m.Arguments[0]);
                        Write(", ");
                        Visit(m.Arguments[1]);
                        Write(")");
                        return m;
                    case "Substring":
                        Write("SUBSTR(");
                        Visit(m.Object);
                        Write(", ");
                        Visit(m.Arguments[0]);
                        Write(" + 1, ");
                        if (m.Arguments.Count == 2)
                        {
                            Visit(m.Arguments[1]);
                        }
                        else
                        {
                            Write("8000");
                        }
                        Write(")");
                        return m;
                    case "Remove":
                        if (m.Arguments.Count == 1)
                        {
                            Write("SUBSTR(");
                            Visit(m.Object);
                            Write(", 1, ");
                            Visit(m.Arguments[0]);
                            Write(")");
                        }
                        else
                        {
                            Write("SUBSTR(");
                            Visit(m.Object);
                            Write(", 1, ");
                            Visit(m.Arguments[0]);
                            Write(") + SUBSTR(");
                            Visit(m.Object);
                            Write(", ");
                            Visit(m.Arguments[0]);
                            Write(" + ");
                            Visit(m.Arguments[1]);
                            Write(")");
                        }
                        return m;
                    case "Trim":
                        Write("TRIM(");
                        Visit(m.Object);
                        Write(")");
                        return m;
                }
            }
            else if (m.Method.DeclaringType == typeof (DateTime))
            {
                switch (m.Method.Name)
                {
                    case "op_Subtract":
                        if (m.Arguments[1].Type == typeof (DateTime))
                        {
                            Write("DATEDIFF(");
                            Visit(m.Arguments[0]);
                            Write(", ");
                            Visit(m.Arguments[1]);
                            Write(")");
                            return m;
                        }
                        break;
                }
            }
            else if (m.Method.DeclaringType == typeof (Decimal))
            {
                switch (m.Method.Name)
                {
                    case "Add":
                    case "Subtract":
                    case "Multiply":
                    case "Divide":
                    case "Remainder":
                        Write("(");
                        VisitValue(m.Arguments[0]);
                        Write(" ");
                        Write(GetOperator(m.Method.Name));
                        Write(" ");
                        VisitValue(m.Arguments[1]);
                        Write(")");
                        return m;
                    case "Negate":
                        Write("-");
                        Visit(m.Arguments[0]);
                        Write("");
                        return m;
                    case "Round":
                        if (m.Arguments.Count == 1)
                        {
                            Write("ROUND(");
                            Visit(m.Arguments[0]);
                            Write(", 0)");
                            return m;
                        }
                        if (m.Arguments.Count == 2 && m.Arguments[1].Type == typeof (int))
                        {
                            Write("ROUND(");
                            Visit(m.Arguments[0]);
                            Write(", ");
                            Visit(m.Arguments[1]);
                            Write(")");
                            return m;
                        }
                        break;
                }
            }
            else if (m.Method.DeclaringType == typeof (Math))
            {
                switch (m.Method.Name)
                {
                    case "Abs":
                    case "Acos":
                    case "Asin":
                    case "Atan":
                    case "Cos":
                    case "Exp":
                    case "Log10":
                    case "Sin":
                    case "Tan":
                    case "Sqrt":
                    case "Sign":
                        Write(m.Method.Name.ToUpper());
                        Write("(");
                        Visit(m.Arguments[0]);
                        Write(")");
                        return m;
                    case "Atan2":
                        Write("ATN2(");
                        Visit(m.Arguments[0]);
                        Write(", ");
                        Visit(m.Arguments[1]);
                        Write(")");
                        return m;
                    case "Log":
                        if (m.Arguments.Count == 1)
                        {
                            goto case "Log10";
                        }
                        break;
                    case "Pow":
                        Write("POWER(");
                        Visit(m.Arguments[0]);
                        Write(", ");
                        Visit(m.Arguments[1]);
                        Write(")");
                        return m;
                    case "Round":
                        if (m.Arguments.Count == 1)
                        {
                            Write("ROUND(");
                            Visit(m.Arguments[0]);
                            Write(", 0)");
                            return m;
                        }
                        if (m.Arguments.Count == 2 && m.Arguments[1].Type == typeof (int))
                        {
                            Write("ROUND(");
                            Visit(m.Arguments[0]);
                            Write(", ");
                            Visit(m.Arguments[1]);
                            Write(")");
                            return m;
                        }
                        break;
                }
            }
            if (m.Method.Name == "ToString")
            {
                // no-op
                Visit(m.Object);
                return m;
            }
            if (!m.Method.IsStatic && m.Method.Name == "CompareTo" && m.Method.ReturnType == typeof (int)
                && m.Arguments.Count == 1)
            {
                Write("(CASE WHEN ");
                Visit(m.Object);
                Write(" = ");
                Visit(m.Arguments[0]);
                Write(" THEN 0 WHEN ");
                Visit(m.Object);
                Write(" < ");
                Visit(m.Arguments[0]);
                Write(" THEN -1 ELSE 1 END)");
                return m;
            }
            if (m.Method.IsStatic && m.Method.Name == "Compare" && m.Method.ReturnType == typeof (int) &&
                m.Arguments.Count == 2)
            {
                Write("(CASE WHEN ");
                Visit(m.Arguments[0]);
                Write(" = ");
                Visit(m.Arguments[1]);
                Write(" THEN 0 WHEN ");
                Visit(m.Arguments[0]);
                Write(" < ");
                Visit(m.Arguments[1]);
                Write(" THEN -1 ELSE 1 END)");
                return m;
            }

            if (m.Method.DeclaringType == typeof (Decimal))
            {
                switch (m.Method.Name)
                {
                    case "Add":
                    case "Subtract":
                    case "Multiply":
                    case "Divide":
                    case "Remainder":
                        Write("(");
                        VisitValue(m.Arguments[0]);
                        Write(" ");
                        Write(GetOperator(m.Method.Name));
                        Write(" ");
                        VisitValue(m.Arguments[1]);
                        Write(")");
                        return m;
                    case "Negate":
                        Write("-");
                        Visit(m.Arguments[0]);
                        Write("");
                        return m;
                    case "Compare":
                        Visit(
                            Expression.Condition(
                                Expression.Equal(m.Arguments[0], m.Arguments[1]),
                                Expression.Constant(0),
                                Expression.Condition(
                                    Expression.LessThan(m.Arguments[0], m.Arguments[1]), Expression.Constant(-1),
                                    Expression.Constant(1))));
                        return m;
                }
            }
            else if (m.Method.Name == "ToString" && m.Object.Type == typeof (string))
            {
                return Visit(m.Object); // no op
            }
            else if (m.Method.Name == "Equals")
            {
                if (m.Method.IsStatic && m.Method.DeclaringType == typeof (object))
                {
                    Write("(");
                    Visit(m.Arguments[0]);
                    Write(" = ");
                    Visit(m.Arguments[1]);
                    Write(")");
                    return m;
                }
                if (!m.Method.IsStatic && m.Arguments.Count == 1 && m.Arguments[0].Type == m.Object.Type)
                {
                    Write("(");
                    Visit(m.Object);
                    Write(" = ");
                    Visit(m.Arguments[0]);
                    Write(")");
                    return m;
                }
            }
            if (forDebug)
            {
                if (m.Object != null)
                {
                    Visit(m.Object);
                    Write(".");
                }
                Write(string.Format("?{0}?", m.Method.Name));
                Write("(");
                for (int i = 0; i < m.Arguments.Count; i++)
                {
                    if (i > 0)
                    {
                        Write(", ");
                    }
                    Visit(m.Arguments[i]);
                }
                Write(")");
                return m;
            }
            throw new NotSupportedException(string.Format("The method '{0}' is not supported", m.Method.Name));
        }

        private bool IsInteger(Type type)
        {
            return TypeHelper.IsInteger(type);
        }

        protected override NewExpression VisitNew(NewExpression nex)
        {
            if (nex.Constructor.DeclaringType == typeof (DateTime))
            {
                if (nex.Arguments.Count == 3)
                {
                    Write("(");
                    Visit(nex.Arguments[0]);
                    Write(" || '-' || (CASE WHEN ");
                    Visit(nex.Arguments[1]);
                    Write(" < 10 THEN '0' || ");
                    Visit(nex.Arguments[1]);
                    Write(" ELSE ");
                    Visit(nex.Arguments[1]);
                    Write(" END)");
                    Write(" || '-' || (CASE WHEN ");
                    Visit(nex.Arguments[2]);
                    Write(" < 10 THEN '0' || ");
                    Visit(nex.Arguments[2]);
                    Write(" ELSE ");
                    Visit(nex.Arguments[2]);
                    Write(" END)");
                    Write(")");
                    return nex;
                }
                if (nex.Arguments.Count == 6)
                {
                    Write("(");
                    Visit(nex.Arguments[0]);
                    Write(" || '-' || ");
                    Visit(nex.Arguments[1]);
                    Write(" || '-' || ");
                    Visit(nex.Arguments[2]);
                    Write(" || ' ' || ");
                    Visit(nex.Arguments[3]);
                    Write(" || ':' || ");
                    Visit(nex.Arguments[4]);
                    Write(" || ':' || ");
                    Visit(nex.Arguments[5]);
                    Write(")");
                    return nex;
                }
            }

            if (forDebug)
            {
                Write("?new?");
                Write(nex.Type.Name);
                Write("(");
                for (int i = 0; i < nex.Arguments.Count; i++)
                {
                    if (i > 0)
                    {
                        Write(", ");
                    }
                    Visit(nex.Arguments[i]);
                }
                Write(")");
                return nex;
            }
            throw new NotSupportedException(
                string.Format("The construtor for '{0}' is not supported", nex.Constructor.DeclaringType));
        }

        protected override Expression VisitUnary(UnaryExpression u)
        {
            string op = GetOperator(u);
            switch (u.NodeType)
            {
                case ExpressionType.Not:
                    if (IsBoolean(u.Operand.Type) || op.Length > 1)
                    {
                        Write(op);
                        Write(" ");
                        VisitPredicate(u.Operand);
                    }
                    else
                    {
                        Write(op);
                        VisitValue(u.Operand);
                    }
                    break;
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                    Write(op);
                    VisitValue(u.Operand);
                    break;
                case ExpressionType.UnaryPlus:
                    VisitValue(u.Operand);
                    break;
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                    // ignore conversions for now
                    Visit(u.Operand);
                    break;
                default:
                    if (forDebug)
                    {
                        Write(string.Format("?{0}?", u.NodeType));
                        Write("(");
                        Visit(u.Operand);
                        Write(")");
                        return u;
                    }
                    throw new NotSupportedException(string.Format("The unary operator '{0}' is not supported",
                        u.NodeType));
            }
            return u;
        }

        protected override Expression VisitBinary(BinaryExpression b)
        {
            if (b.NodeType == ExpressionType.Power)
            {
                Write("POWER(");
                VisitValue(b.Left);
                Write(", ");
                VisitValue(b.Right);
                Write(")");
                return b;
            }
            if (b.NodeType == ExpressionType.Coalesce)
            {
                Write("COALESCE(");
                VisitValue(b.Left);
                Write(", ");
                Expression r = b.Right;
                while (r.NodeType == ExpressionType.Coalesce)
                {
                    var rb = (BinaryExpression) r;
                    VisitValue(rb.Left);
                    Write(", ");
                    r = rb.Right;
                }
                VisitValue(r);
                Write(")");
                return b;
            }
            if (b.NodeType == ExpressionType.ExclusiveOr)
            {
                // SQLite does not have XOR (^).. Use translation:  ((A & ~B) | (~A & B))
                Write("((");
                VisitValue(b.Left);
                Write(" & ~");
                VisitValue(b.Right);
                Write(") | (~");
                VisitValue(b.Left);
                Write(" & ");
                VisitValue(b.Right);
                Write("))");
                return b;
            }

            string op = GetOperator(b);
            Expression left = b.Left;
            Expression right = b.Right;

            Write("(");
            switch (b.NodeType)
            {
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    if (IsBoolean(left.Type))
                    {
                        VisitPredicate(left);
                        Write(" ");
                        Write(op);
                        Write(" ");
                        VisitPredicate(right);
                    }
                    else
                    {
                        VisitValue(left);
                        Write(" ");
                        Write(op);
                        Write(" ");
                        VisitValue(right);
                    }
                    break;
                case ExpressionType.Equal:
                    if (right.NodeType == ExpressionType.Constant)
                    {
                        var ce = (ConstantExpression) right;
                        if (ce.Value == null)
                        {
                            Visit(left);
                            Write(" IS NULL");
                            break;
                        }
                    }
                    else if (left.NodeType == ExpressionType.Constant)
                    {
                        var ce = (ConstantExpression) left;
                        if (ce.Value == null)
                        {
                            Visit(right);
                            Write(" IS NULL");
                            break;
                        }
                    }
                    goto case ExpressionType.LessThan;
                case ExpressionType.NotEqual:
                    if (right.NodeType == ExpressionType.Constant)
                    {
                        var ce = (ConstantExpression) right;
                        if (ce.Value == null)
                        {
                            Visit(left);
                            Write(" IS NOT NULL");
                            break;
                        }
                    }
                    else if (left.NodeType == ExpressionType.Constant)
                    {
                        var ce = (ConstantExpression) left;
                        if (ce.Value == null)
                        {
                            Visit(right);
                            Write(" IS NOT NULL");
                            break;
                        }
                    }
                    goto case ExpressionType.LessThan;
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                    // check for special x.CompareTo(y) && type.Compare(x,y)
                    if (left.NodeType == ExpressionType.Call && right.NodeType == ExpressionType.Constant)
                    {
                        var mc = (MethodCallExpression) left;
                        var ce = (ConstantExpression) right;
                        if (ce.Value != null && ce.Value.GetType() == typeof (int) && ((int) ce.Value) == 0)
                        {
                            if (mc.Method.Name == "CompareTo" && !mc.Method.IsStatic && mc.Arguments.Count == 1)
                            {
                                left = mc.Object;
                                right = mc.Arguments[0];
                            }
                            else if ((mc.Method.DeclaringType == typeof (string) ||
                                      mc.Method.DeclaringType == typeof (decimal))
                                     && mc.Method.Name == "Compare" && mc.Method.IsStatic && mc.Arguments.Count == 2)
                            {
                                left = mc.Arguments[0];
                                right = mc.Arguments[1];
                            }
                        }
                    }
                    goto case ExpressionType.Add;
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                case ExpressionType.Divide:
                case ExpressionType.Modulo:
                case ExpressionType.ExclusiveOr:
                case ExpressionType.LeftShift:
                case ExpressionType.RightShift:
                    VisitValue(left);
                    Write(" ");
                    Write(op);
                    Write(" ");
                    VisitValue(right);
                    break;
                default:
                    if (forDebug)
                    {
                        Write(string.Format("?{0}?", b.NodeType));
                        Write("(");
                        Visit(b.Left);
                        Write(", ");
                        Visit(b.Right);
                        Write(")");
                        return b;
                    }
                    throw new NotSupportedException(string.Format("The binary operator '{0}' is not supported",
                        b.NodeType));
            }
            Write(")");
            return b;
        }

        private string GetOperator(string methodName)
        {
            switch (methodName)
            {
                case "Add":
                    return "+";
                case "Subtract":
                    return "-";
                case "Multiply":
                    return "*";
                case "Divide":
                    return "/";
                case "Negate":
                    return "-";
                case "Remainder":
                    return "%";
                default:
                    return null;
            }
        }

        private string GetOperator(UnaryExpression u)
        {
            switch (u.NodeType)
            {
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                    return "-";
                case ExpressionType.UnaryPlus:
                    return "+";
                case ExpressionType.Not:
                    return IsBoolean(u.Operand.Type) ? "NOT" : "~";
                default:
                    return "";
            }
        }

        private string GetOperator(BinaryExpression b)
        {
            if (b.NodeType == ExpressionType.Add && b.Type == typeof (string))
            {
                return "||";
            }

            switch (b.NodeType)
            {
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    return (IsBoolean(b.Left.Type)) ? "AND" : "&";
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    return (IsBoolean(b.Left.Type) ? "OR" : "|");
                case ExpressionType.Equal:
                    return "=";
                case ExpressionType.NotEqual:
                    return "<>";
                case ExpressionType.LessThan:
                    return "<";
                case ExpressionType.LessThanOrEqual:
                    return "<=";
                case ExpressionType.GreaterThan:
                    return ">";
                case ExpressionType.GreaterThanOrEqual:
                    return ">=";
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                    return "+";
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    return "-";
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                    return "*";
                case ExpressionType.Divide:
                    return "/";
                case ExpressionType.Modulo:
                    return "%";
                case ExpressionType.ExclusiveOr:
                    return "^";
                case ExpressionType.LeftShift:
                    return "<<";
                case ExpressionType.RightShift:
                    return ">>";
                default:
                    return "";
            }
        }

        private bool IsBoolean(Type type)
        {
            return type == typeof (bool) || type == typeof (bool?);
        }

        private bool IsPredicate(Expression expr)
        {
            switch (expr.NodeType)
            {
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    return IsBoolean(expr.Type);
                case ExpressionType.Not:
                    return IsBoolean(expr.Type);
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case (ExpressionType) DbExpressionType.IsNull:
                case (ExpressionType) DbExpressionType.Between:
                case (ExpressionType) DbExpressionType.Exists:
                case (ExpressionType) DbExpressionType.In:
                    return true;
                case ExpressionType.Call:
                    return IsBoolean(expr.Type);
                default:
                    return false;
            }
        }

        private Expression VisitPredicate(Expression expr)
        {
            Visit(expr);
            if (!IsPredicate(expr))
            {
                Write(" <> 0");
            }
            return expr;
        }

        private Expression VisitValue(Expression expr)
        {
            if (IsPredicate(expr))
            {
                Write("CASE WHEN (");
                Visit(expr);
                Write(") THEN 1 ELSE 0 END");
                return expr;
            }

            return Visit(expr);
        }

        protected override Expression VisitConditional(ConditionalExpression c)
        {
            if (IsPredicate(c.Test))
            {
                Write("(CASE WHEN ");
                VisitPredicate(c.Test);
                Write(" THEN ");
                VisitValue(c.IfTrue);
                Expression ifFalse = c.IfFalse;
                while (ifFalse != null && ifFalse.NodeType == ExpressionType.Conditional)
                {
                    var fc = (ConditionalExpression) ifFalse;
                    Write(" WHEN ");
                    VisitPredicate(fc.Test);
                    Write(" THEN ");
                    VisitValue(fc.IfTrue);
                    ifFalse = fc.IfFalse;
                }
                if (ifFalse != null)
                {
                    Write(" ELSE ");
                    VisitValue(ifFalse);
                }
                Write(" END)");
            }
            else
            {
                Write("(CASE ");
                VisitValue(c.Test);
                Write(" WHEN 0 THEN ");
                VisitValue(c.IfFalse);
                Write(" ELSE ");
                VisitValue(c.IfTrue);
                Write(" END)");
            }
            return c;
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            WriteValue(c.Value);
            return c;
        }

        private void WriteValue(object value)
        {
            if (value == null)
            {
                Write("NULL");
            }
            else if (value.GetType().GetTypeInfo().IsEnum)
            {
                Write(Convert.ChangeType(value, Enum.GetUnderlyingType(value.GetType()), CultureInfo.InvariantCulture));
            }
            else
            {
                switch (TypeHelper.GetTypeCode(value.GetType()))
                {
                    case TypeCode.Boolean:
                        Write(((bool) value) ? 1 : 0);
                        break;
                    case TypeCode.String:
                        Write("'");
                        Write(value);
                        Write("'");
                        break;
                    case TypeCode.Object:
                        throw new NotSupportedException(string.Format("The constant for '{0}' is not supported", value));
                    case TypeCode.Single:
                    case TypeCode.Double:
                    case TypeCode.Decimal:
                        string str = string.Format(CultureInfo.InvariantCulture, "{0:0.#}", value);
                        if (!str.OfType<char>().Contains('.'))
                        {
                            str += ".0";
                        }
                        Write(str);
                        break;
                    default:
                        Write(value);
                        break;
                }
            }
        }

        protected override Expression VisitColumn(ColumnExpression column)
        {
            if (column.Alias != null && !HideColumnAliases)
            {
                WriteAliasName(GetAliasName(column.Alias));
                Write(".");
            }
            WriteColumnName(column.Name);
            return column;
        }

        protected override Expression VisitProjection(ProjectionExpression proj)
        {
            // treat these like scalar subqueries
            if ((proj.Projector is ColumnExpression) || forDebug)
            {
                Write("(");
                WriteLine(Indentation.Inner);
                Visit(proj.Select);
                Write(")");
                Indent(Indentation.Outer);
            }
            else
            {
                throw new NotSupportedException("Non-scalar projections cannot be translated to SQL.");
            }
            return proj;
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            AddAliases(select.From);
            Write("SELECT ");
            if (select.IsDistinct)
            {
                Write("DISTINCT ");
            }
            WriteColumns(select.Columns);
            if (select.From != null)
            {
                WriteLine(Indentation.Same);
                Write("FROM ");
                VisitSource(select.From);
            }
            if (select.Where != null)
            {
                WriteLine(Indentation.Same);
                Write("WHERE ");
                VisitPredicate(select.Where);
            }
            if (select.GroupBy != null && select.GroupBy.Count > 0)
            {
                WriteLine(Indentation.Same);
                Write("GROUP BY ");
                for (int i = 0, n = select.GroupBy.Count; i < n; i++)
                {
                    if (i > 0)
                    {
                        Write(", ");
                    }
                    VisitValue(select.GroupBy[i]);
                }
            }
            if (select.OrderBy != null && select.OrderBy.Count > 0)
            {
                WriteLine(Indentation.Same);
                Write("ORDER BY ");
                for (int i = 0, n = select.OrderBy.Count; i < n; i++)
                {
                    OrderExpression exp = select.OrderBy[i];
                    if (i > 0)
                    {
                        Write(", ");
                    }
                    VisitValue(exp.Expression);
                    if (exp.OrderType != OrderType.Ascending)
                    {
                        Write(" DESC");
                    }
                }
            }
            if (select.Take != null)
            {
                WriteLine(Indentation.Same);
                Write("LIMIT ");
                if (select.Skip == null)
                {
                    Write("0");
                }
                else
                {
                    Write(select.Skip);
                }
                Write(", ");
                Visit(select.Take);
            }
            return select;
        }

        private void WriteTopClause(Expression expression)
        {
            Write("TOP (");
            Visit(expression);
            Write(") ");
        }

        private void WriteColumns(ReadOnlyCollection<ColumnDeclaration> columns)
        {
            if (columns.Count == 0)
            {
                Write("0");
                if (IsNested)
                {
                    Write(" AS ");
                    WriteColumnName("tmp");
                    Write(" ");
                }
            }
            else if (columns.Count > 0)
            {
                for (int i = 0, n = columns.Count; i < n; i++)
                {
                    ColumnDeclaration column = columns[i];
                    if (i > 0)
                    {
                        Write(", ");
                    }
                    var c = VisitValue(column.Expression) as ColumnExpression;
                    if (!string.IsNullOrEmpty(column.Name) && (c == null || c.Name != column.Name))
                    {
                        Write(" ");
                        WriteAsColumnName(column.Name);
                    }
                }
            }
            else
            {
                Write("NULL ");
                if (isNested)
                {
                    WriteAsColumnName("tmp");
                    Write(" ");
                }
            }
        }

        protected override Expression VisitSource(Expression source)
        {
            bool saveIsNested = isNested;
            isNested = true;
            switch ((DbExpressionType) source.NodeType)
            {
                case DbExpressionType.Table:
                    var table = (TableExpression) source;
                    WriteTableName(table.Name);
                    if (!HideTableAliases)
                    {
                        Write(" ");
                        WriteAsAliasName(GetAliasName(table.Alias));
                    }
                    break;
                case DbExpressionType.Select:
                    var select = (SelectExpression) source;
                    Write("(");
                    WriteLine(Indentation.Inner);
                    Visit(select);
                    WriteLine(Indentation.Same);
                    Write(") ");
                    WriteAsAliasName(GetAliasName(select.Alias));
                    Indent(Indentation.Outer);
                    break;
                case DbExpressionType.Join:
                    VisitJoin((JoinExpression) source);
                    break;
                default:
                    throw new InvalidOperationException("Select source is not valid type");
            }
            isNested = saveIsNested;
            return source;
        }

        protected override Expression VisitJoin(JoinExpression join)
        {
            VisitJoinLeft(join.Left);
            WriteLine(Indentation.Same);
            switch (join.Join)
            {
                case JoinType.CrossJoin:
                    Write("CROSS JOIN ");
                    break;
                case JoinType.InnerJoin:
                    Write("INNER JOIN ");
                    break;
                case JoinType.CrossApply:
                    Write("CROSS APPLY ");
                    break;
                case JoinType.OuterApply:
                    Write("OUTER APPLY ");
                    break;
                case JoinType.LeftOuter:
                case JoinType.SingletonLeftOuter:
                    Write("LEFT OUTER JOIN ");
                    break;
            }
            VisitJoinRight(join.Right);
            if (join.Condition != null)
            {
                WriteLine(Indentation.Inner);
                Write("ON ");
                VisitPredicate(join.Condition);
                Indent(Indentation.Outer);
            }
            return join;
        }

        private Expression VisitJoinLeft(Expression source)
        {
            return VisitSource(source);
        }

        private Expression VisitJoinRight(Expression source)
        {
            return VisitSource(source);
        }

        private void WriteAggregateName(string aggregateName)
        {
            switch (aggregateName)
            {
                case "Average":
                    Write("AVG");
                    break;
                case "LongCount":
                    Write("COUNT");
                    break;
                default:
                    Write(aggregateName.ToUpper());
                    break;
            }
        }

        private bool RequiresAsteriskWhenNoArgument(string aggregateName)
        {
            return aggregateName == "Count" || aggregateName == "LongCount";
        }

        protected override Expression VisitAggregate(AggregateExpression aggregate)
        {
            WriteAggregateName(aggregate.AggregateName);
            Write("(");
            if (aggregate.IsDistinct)
            {
                Write("DISTINCT ");
            }
            if (aggregate.Argument != null)
            {
                VisitValue(aggregate.Argument);
            }
            else if (RequiresAsteriskWhenNoArgument(aggregate.AggregateName))
            {
                Write("*");
            }
            Write(")");
            return aggregate;
        }

        protected override Expression VisitIsNull(IsNullExpression isnull)
        {
            VisitValue(isnull.Expression);
            Write(" IS NULL");
            return isnull;
        }

        protected override Expression VisitBetween(BetweenExpression between)
        {
            VisitValue(between.Expression);
            Write(" BETWEEN ");
            VisitValue(between.Lower);
            Write(" AND ");
            VisitValue(between.Upper);
            return between;
        }

        protected override Expression VisitRowNumber(RowNumberExpression rowNumber)
        {
            throw new NotSupportedException();
        }

        protected override Expression VisitScalar(ScalarExpression subquery)
        {
            Write("(");
            WriteLine(Indentation.Inner);
            Visit(subquery.Select);
            WriteLine(Indentation.Same);
            Write(")");
            Indent(Indentation.Outer);
            return subquery;
        }

        protected override Expression VisitExists(ExistsExpression exists)
        {
            Write("EXISTS(");
            WriteLine(Indentation.Inner);
            Visit(exists.Select);
            WriteLine(Indentation.Same);
            Write(")");
            Indent(Indentation.Outer);
            return exists;
        }

        protected override Expression VisitIn(InExpression @in)
        {
            if (@in.Values != null)
            {
                if (@in.Values.Count == 0)
                {
                    Write("0 <> 0");
                }
                else
                {
                    VisitValue(@in.Expression);
                    Write(" IN (");
                    for (int i = 0, n = @in.Values.Count; i < n; i++)
                    {
                        if (i > 0)
                        {
                            Write(", ");
                        }
                        VisitValue(@in.Values[i]);
                    }
                    Write(")");
                }
            }
            else
            {
                VisitValue(@in.Expression);
                Write(" IN (");
                WriteLine(Indentation.Inner);
                Visit(@in.Select);
                WriteLine(Indentation.Same);
                Write(")");
                Indent(Indentation.Outer);
            }
            return @in;
        }

        protected override Expression VisitNamedValue(NamedValueExpression value)
        {
            Write("?");
            //this.WriteParameterName(value.Name);
            return value;
        }

        protected override Expression VisitInsert(InsertCommand insert)
        {
            Write("INSERT INTO ");
            WriteTableName(insert.Table.Name);
            Write("(");
            for (int i = 0, n = insert.Assignments.Count; i < n; i++)
            {
                ColumnAssignment ca = insert.Assignments[i];
                if (i > 0)
                {
                    Write(", ");
                }
                WriteColumnName(ca.Column.Name);
            }
            Write(")");
            WriteLine(Indentation.Same);
            Write("VALUES (");
            for (int i = 0, n = insert.Assignments.Count; i < n; i++)
            {
                ColumnAssignment ca = insert.Assignments[i];
                if (i > 0)
                {
                    Write(", ");
                }
                Visit(ca.Expression);
            }
            Write(")");
            return insert;
        }

        protected override Expression VisitUpdate(UpdateCommand update)
        {
            Write("UPDATE ");
            WriteTableName(update.Table.Name);
            WriteLine(Indentation.Same);
            bool saveHide = HideColumnAliases;
            HideColumnAliases = true;
            Write("SET ");
            for (int i = 0, n = update.Assignments.Count; i < n; i++)
            {
                ColumnAssignment ca = update.Assignments[i];
                if (i > 0)
                {
                    Write(", ");
                }
                Visit(ca.Column);
                Write(" = ");
                Visit(ca.Expression);
            }
            if (update.Where != null)
            {
                WriteLine(Indentation.Same);
                Write("WHERE ");
                VisitPredicate(update.Where);
            }
            HideColumnAliases = saveHide;
            return update;
        }

        protected override Expression VisitDelete(DeleteCommand delete)
        {
            Write("DELETE FROM ");
            bool saveHideTable = HideTableAliases;
            bool saveHideColumn = HideColumnAliases;
            HideTableAliases = true;
            HideColumnAliases = true;
            VisitSource(delete.Table);
            if (delete.Where != null)
            {
                WriteLine(Indentation.Same);
                Write("WHERE ");
                VisitPredicate(delete.Where);
            }
            HideTableAliases = saveHideTable;
            HideColumnAliases = saveHideColumn;
            return delete;
        }

        protected override Expression VisitIf(IFCommand ifx)
        {
            throw new NotSupportedException();
        }

        protected override Expression VisitBlock(BlockCommand block)
        {
            throw new NotSupportedException();
        }

        protected override Expression VisitDeclaration(DeclarationCommand decl)
        {
            throw new NotSupportedException();
        }

        protected override Expression VisitVariable(VariableExpression vex)
        {
            WriteVariableName(vex.Name);
            return vex;
        }

        private void VisitStatement(Expression expression)
        {
            var p = expression as ProjectionExpression;
            if (p != null)
            {
                Visit(p.Select);
            }
            else
            {
                Visit(expression);
            }
        }

        protected override Expression VisitFunction(FunctionExpression func)
        {
            Write(func.Name);
            if (func.Arguments.Count > 0)
            {
                Write("(");
                for (int i = 0, n = func.Arguments.Count; i < n; i++)
                {
                    if (i > 0)
                    {
                        Write(", ");
                    }
                    Visit(func.Arguments[i]);
                }
                Write(")");
            }
            return func;
        }

        private enum Indentation
        {
            Same,

            Inner,

            Outer
        }
    }
}