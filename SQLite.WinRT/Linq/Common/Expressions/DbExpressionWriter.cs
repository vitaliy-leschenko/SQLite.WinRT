// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using SQLite.WinRT.Linq.Base;
using SQLite.WinRT.Linq.Common.Language;

namespace SQLite.WinRT.Linq.Common.Expressions
{
    /// <summary>
    ///     Writes out an expression tree (including DbExpression nodes) in a C#-ish syntax
    /// </summary>
    public class DbExpressionWriter : ExpressionWriter
    {
        private readonly Dictionary<TableAlias, int> aliasMap = new Dictionary<TableAlias, int>();

        protected DbExpressionWriter(TextWriter writer)
            : base(writer)
        {
        }

        public new static void Write(TextWriter writer, Expression expression)
        {
            new DbExpressionWriter(writer).Visit(expression);
        }

        public new static string WriteToString(Expression expression)
        {
            var sw = new StringWriter();
            Write(sw, expression);
            return sw.ToString();
        }

        protected override Expression Visit(Expression exp)
        {
            if (exp == null)
            {
                return null;
            }

            switch ((DbExpressionType) exp.NodeType)
            {
                case DbExpressionType.Projection:
                    return VisitProjection((ProjectionExpression) exp);
                case DbExpressionType.ClientJoin:
                    return VisitClientJoin((ClientJoinExpression) exp);
                case DbExpressionType.Select:
                    return VisitSelect((SelectExpression) exp);
                case DbExpressionType.OuterJoined:
                    return VisitOuterJoined((OuterJoinedExpression) exp);
                case DbExpressionType.Column:
                    return VisitColumn((ColumnExpression) exp);
                case DbExpressionType.Insert:
                case DbExpressionType.Update:
                case DbExpressionType.Delete:
                case DbExpressionType.If:
                case DbExpressionType.Block:
                case DbExpressionType.Declaration:
                    return VisitCommand((CommandExpression) exp);
                case DbExpressionType.Batch:
                    return VisitBatch((BatchExpression) exp);
                case DbExpressionType.Function:
                    return VisitFunction((FunctionExpression) exp);
                case DbExpressionType.Entity:
                    return VisitEntity((EntityExpression) exp);
                default:
                    if (exp is DbExpression)
                    {
                        Write(FormatQuery(exp));
                        return exp;
                    }
                    return base.Visit(exp);
            }
        }

        protected void AddAlias(TableAlias alias)
        {
            if (!aliasMap.ContainsKey(alias))
            {
                aliasMap.Add(alias, aliasMap.Count);
            }
        }

        protected virtual Expression VisitProjection(ProjectionExpression projection)
        {
            AddAlias(projection.Select.Alias);
            Write("Project(");
            WriteLine(Indentation.Inner);
            Write("@\"");
            Visit(projection.Select);
            Write("\",");
            WriteLine(Indentation.Same);
            Visit(projection.Projector);
            Write(",");
            WriteLine(Indentation.Same);
            Visit(projection.Aggregator);
            WriteLine(Indentation.Outer);
            Write(")");
            return projection;
        }

        protected virtual Expression VisitClientJoin(ClientJoinExpression join)
        {
            AddAlias(join.Projection.Select.Alias);
            Write("ClientJoin(");
            WriteLine(Indentation.Inner);
            Write("OuterKey(");
            VisitExpressionList(join.OuterKey);
            Write("),");
            WriteLine(Indentation.Same);
            Write("InnerKey(");
            VisitExpressionList(join.InnerKey);
            Write("),");
            WriteLine(Indentation.Same);
            Visit(join.Projection);
            WriteLine(Indentation.Outer);
            Write(")");
            return join;
        }

        protected virtual Expression VisitOuterJoined(OuterJoinedExpression outer)
        {
            Write("Outer(");
            WriteLine(Indentation.Inner);
            Visit(outer.Test);
            Write(", ");
            WriteLine(Indentation.Same);
            Visit(outer.Expression);
            WriteLine(Indentation.Outer);
            Write(")");
            return outer;
        }

        protected virtual Expression VisitSelect(SelectExpression select)
        {
            Write(select.QueryText);
            return select;
        }

        protected virtual Expression VisitCommand(CommandExpression command)
        {
            Write(FormatQuery(command));
            return command;
        }

        protected virtual string FormatQuery(Expression query)
        {
            return SqlFormatter.Format(query, true);
        }

        protected virtual Expression VisitBatch(BatchExpression batch)
        {
            Write("Batch(");
            WriteLine(Indentation.Inner);
            Visit(batch.Input);
            Write(",");
            WriteLine(Indentation.Same);
            Visit(batch.Operation);
            Write(",");
            WriteLine(Indentation.Same);
            Visit(batch.BatchSize);
            Write(", ");
            Visit(batch.Stream);
            WriteLine(Indentation.Outer);
            Write(")");
            return batch;
        }

        protected virtual Expression VisitVariable(VariableExpression vex)
        {
            Write(FormatQuery(vex));
            return vex;
        }

        protected virtual Expression VisitFunction(FunctionExpression function)
        {
            Write("FUNCTION ");
            Write(function.Name);
            if (function.Arguments.Count > 0)
            {
                Write("(");
                VisitExpressionList(function.Arguments);
                Write(")");
            }
            return function;
        }

        protected virtual Expression VisitEntity(EntityExpression entity)
        {
            Visit(entity.Expression);
            return entity;
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            if (c.Type == typeof (QueryCommand))
            {
                var qc = (QueryCommand) c.Value;
                Write("new QueryCommand {");
                WriteLine(Indentation.Inner);
                Write("\"" + qc.CommandText + "\"");
                Write(",");
                WriteLine(Indentation.Same);
                Visit(Expression.Constant(qc.Parameters));
                Write(")");
                WriteLine(Indentation.Outer);
                return c;
            }
            return base.VisitConstant(c);
        }

        protected virtual Expression VisitColumn(ColumnExpression column)
        {
            int iAlias;
            string aliasName = aliasMap.TryGetValue(column.Alias, out iAlias)
                ? "A" + iAlias
                : "A" + (column.Alias != null ? column.Alias.GetHashCode().ToString() : "") + "?";

            Write(aliasName);
            Write(".");
            Write("Column(\"");
            Write(column.Name);
            Write("\")");
            return column;
        }
    }
}