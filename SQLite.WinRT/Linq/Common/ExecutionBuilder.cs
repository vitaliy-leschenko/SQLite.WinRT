﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using SQLite.WinRT.Linq.Base;
using SQLite.WinRT.Linq.Common.Expressions;
using SQLite.WinRT.Linq.Common.Language;
using SQLite.WinRT.Linq.Common.Mapping;
using SQLite.WinRT.Linq.Common.Translation;

namespace SQLite.WinRT.Linq.Common
{
    /// <summary>
    ///     Builds an execution plan for a query expression
    /// </summary>
    public class ExecutionBuilder : DbExpressionVisitor
    {
        private readonly Expression executor;
        private readonly List<Expression> initializers = new List<Expression>();
        private readonly EntityPolicy policy;
        private readonly List<ParameterExpression> variables = new List<ParameterExpression>();

        private bool isTop = true;
        private int nLookup;

        private int nReaders;
        private MemberInfo receivingMember;
        private Scope scope;

        private Dictionary<string, Expression> variableMap = new Dictionary<string, Expression>();

        private ExecutionBuilder(EntityPolicy policy, Expression executor)
        {
            this.policy = policy;
            this.executor = executor;
        }

        public static Expression Build(EntityPolicy policy, Expression expression, Expression provider)
        {
            ParameterExpression executor = Expression.Parameter(typeof (EntityProvider.Executor), "executor");
            var builder = new ExecutionBuilder(policy, executor);
            builder.variables.Add(executor);
            builder.initializers.Add(
                Expression.Call(Expression.Convert(provider, typeof (EntityProvider)), "CreateExecutor", null, null));
            Expression result = builder.Build(expression);
            return result;
        }

        private Expression Build(Expression expression)
        {
            expression = Visit(expression);
            expression = AddVariables(expression);
            return expression;
        }

        private Expression AddVariables(Expression expression)
        {
            // add variable assignments up front
            if (variables.Count > 0)
            {
                var exprs = new List<Expression>();
                for (int i = 0, n = variables.Count; i < n; i++)
                {
                    exprs.Add(MakeAssign(variables[i], initializers[i]));
                }
                exprs.Add(expression);

                expression = MakeSequence(exprs); // yields last expression value

                ConstantExpression[] nulls = variables.Select(v => Expression.Constant(null, v.Type)).ToArray();

                // use invoke/lambda to create variables via parameters in scope
                expression = Expression.Invoke(Expression.Lambda(expression, variables.ToArray()), nulls);
            }

            return expression;
        }

        private static Expression MakeSequence(IList<Expression> expressions)
        {
            Expression last = expressions[expressions.Count - 1];
            expressions =
                expressions.Select(e => e.Type.GetTypeInfo().IsValueType ? Expression.Convert(e, typeof (object)) : e)
                    .ToList();
            return
                Expression.Convert(
                    Expression.Call(typeof (ExecutionBuilder), "Sequence", null,
                        Expression.NewArrayInit(typeof (object), expressions)),
                    last.Type);
        }

        public static object Sequence(params object[] values)
        {
            return values[values.Length - 1];
        }

        public static IEnumerable<R> Batch<T, R>(IEnumerable<T> items, Func<T, R> selector, bool stream)
        {
            IEnumerable<R> result = items.Select(selector);
            if (!stream)
            {
                return result.ToList();
            }
            return new EnumerateOnce<R>(result);
        }

        private static Expression MakeAssign(ParameterExpression variable, Expression value)
        {
            return Expression.Call(typeof (ExecutionBuilder), "Assign", new[] {variable.Type}, variable, value);
        }

        public static T Assign<T>(ref T variable, T value)
        {
            variable = value;
            return value;
        }

        private Expression BuildInner(Expression expression)
        {
            var eb = new ExecutionBuilder(policy, executor);
            eb.scope = scope;
            eb.receivingMember = receivingMember;
            eb.nReaders = nReaders;
            eb.nLookup = nLookup;
            eb.variableMap = variableMap;
            return eb.Build(expression);
        }

        protected override MemberBinding VisitBinding(MemberBinding binding)
        {
            MemberInfo save = receivingMember;
            receivingMember = binding.Member;
            MemberBinding result = base.VisitBinding(binding);
            receivingMember = save;
            return result;
        }

        private Expression MakeJoinKey(IList<Expression> key)
        {
            if (key.Count == 1)
            {
                return key[0];
            }
            return Expression.New(
                typeof (CompoundKey).GetConstructors()[0],
                Expression.NewArrayInit(typeof (object),
                    key.Select(k => (Expression) Expression.Convert(k, typeof (object)))));
        }

        protected override Expression VisitClientJoin(ClientJoinExpression join)
        {
            // convert client join into a up-front lookup table builder & replace client-join in tree with lookup accessor

            // 1) lookup = query.Select(e => new KVP(key: inner, value: e)).ToLookup(kvp => kvp.Key, kvp => kvp.Value)
            Expression innerKey = MakeJoinKey(join.InnerKey);
            Expression outerKey = MakeJoinKey(join.OuterKey);

            ConstructorInfo kvpConstructor =
                typeof (KeyValuePair<,>).MakeGenericType(innerKey.Type, join.Projection.Projector.Type).GetConstructor(
                    new[] {innerKey.Type, join.Projection.Projector.Type});
            Expression constructKVPair = Expression.New(kvpConstructor, innerKey, join.Projection.Projector);
            var newProjection = new ProjectionExpression(join.Projection.Select, constructKVPair);

            int iLookup = ++nLookup;
            Expression execution = ExecuteProjection(newProjection, false);

            ParameterExpression kvp = Expression.Parameter(constructKVPair.Type, "kvp");

            // filter out nulls
            if (join.Projection.Projector.NodeType == (ExpressionType) DbExpressionType.OuterJoined)
            {
                LambdaExpression pred =
                    Expression.Lambda(
                        Expression.PropertyOrField(kvp, "Value")
                            .NotEqual(TypeHelper.GetNullConstant(join.Projection.Projector.Type)), kvp);
                execution = Expression.Call(typeof (Enumerable), "Where", new[] {kvp.Type}, execution, pred);
            }

            // make lookup
            LambdaExpression keySelector = Expression.Lambda(Expression.PropertyOrField(kvp, "Key"), kvp);
            LambdaExpression elementSelector = Expression.Lambda(Expression.PropertyOrField(kvp, "Value"), kvp);
            Expression toLookup = Expression.Call(
                typeof (Enumerable),
                "ToLookup",
                new[] {kvp.Type, outerKey.Type, join.Projection.Projector.Type},
                execution,
                keySelector,
                elementSelector);

            // 2) agg(lookup[outer])
            ParameterExpression lookup = Expression.Parameter(toLookup.Type, "lookup" + iLookup);
            PropertyInfo property = lookup.Type.GetProperty("Item");
            Expression access = Expression.Call(lookup, property.GetMethod, Visit(outerKey));
            if (join.Projection.Aggregator != null)
            {
                // apply aggregator
                access = DbExpressionReplacer.Replace(
                    join.Projection.Aggregator.Body, join.Projection.Aggregator.Parameters[0], access);
            }

            variables.Add(lookup);
            initializers.Add(toLookup);

            return access;
        }

        protected override Expression VisitProjection(ProjectionExpression projection)
        {
            if (isTop)
            {
                isTop = false;
                return ExecuteProjection(projection, scope != null);
            }
            return BuildInner(projection);
        }

        private Expression Parameterize(Expression expression)
        {
            if (variableMap.Count > 0)
            {
                expression = VariableSubstitutor.Substitute(variableMap, expression);
            }
            return QueryLinguist.Parameterize(expression);
        }

        private Expression ExecuteProjection(ProjectionExpression projection, bool okayToDefer)
        {
            // parameterize query
            projection = (ProjectionExpression) Parameterize(projection);

            if (scope != null)
            {
                // also convert references to outer alias to named values!  these become SQL parameters too
                projection = (ProjectionExpression) OuterParameterizer.Parameterize(scope.Alias, projection);
            }

            string commandText = QueryLinguist.Format(projection.Select);
            ReadOnlyCollection<NamedValueExpression> namedValues = NamedValueGatherer.Gather(projection.Select);
            var command = new QueryCommand(
                commandText, namedValues.Select(v => new QueryParameter(v.Name, v.Type, v.QueryType)));
            Expression[] values = namedValues.Select(v => Expression.Convert(Visit(v.Value), typeof (object))).ToArray();

            return ExecuteProjection(projection, okayToDefer, command, values);
        }

        private Expression ExecuteProjection(
            ProjectionExpression projection, bool okayToDefer, QueryCommand command, Expression[] values)
        {
            okayToDefer &= (receivingMember != null && policy.IsDeferLoaded(receivingMember));

            Scope saveScope = scope;
            ParameterExpression reader = Expression.Parameter(typeof (FieldReader), "r" + nReaders++);
            scope = new Scope(scope, reader, projection.Select.Alias, projection.Select.Columns);
            LambdaExpression projector = Expression.Lambda(Visit(projection.Projector), reader);
            scope = saveScope;

            MappingEntity entity = EntityFinder.Find(projection.Projector);

            string methExecute = okayToDefer ? "ExecuteDeferred" : "Execute";

            // call low-level execute directly on supplied DbQueryProvider
            Expression result = Expression.Call(
                executor,
                methExecute,
                new[] {projector.Body.Type},
                Expression.Constant(command),
                projector,
                Expression.Constant(entity, typeof (MappingEntity)),
                Expression.NewArrayInit(typeof (object), values));

            if (projection.Aggregator != null)
            {
                // apply aggregator
                result = DbExpressionReplacer.Replace(projection.Aggregator.Body, projection.Aggregator.Parameters[0],
                    result);
            }
            return result;
        }

        protected override Expression VisitBatch(BatchExpression batch)
        {
            if (QueryLanguage.AllowsMultipleCommands || !IsMultipleCommands(batch.Operation.Body as CommandExpression))
            {
                return BuildExecuteBatch(batch);
            }
            Expression source = Visit(batch.Input);
            Expression op = Visit(batch.Operation.Body);
            LambdaExpression fn = Expression.Lambda(op, batch.Operation.Parameters[1]);
            return Expression.Call(
                GetType(),
                "Batch",
                new[] {TypeHelper.GetElementType(source.Type), batch.Operation.Body.Type},
                source,
                fn,
                batch.Stream);
        }

        private Expression BuildExecuteBatch(BatchExpression batch)
        {
            // parameterize query
            Expression operation = Parameterize(batch.Operation.Body);

            string commandText = QueryLinguist.Format(operation);
            ReadOnlyCollection<NamedValueExpression> namedValues = NamedValueGatherer.Gather(operation);
            var command = new QueryCommand(
                commandText, namedValues.Select(v => new QueryParameter(v.Name, v.Type, v.QueryType)));
            Expression[] values = namedValues.Select(v => Expression.Convert(Visit(v.Value), typeof (object))).ToArray();

            Expression paramSets = Expression.Call(
                typeof (Enumerable),
                "Select",
                new[] {batch.Operation.Parameters[1].Type, typeof (object[])},
                batch.Input,
                Expression.Lambda(Expression.NewArrayInit(typeof (object), values),
                    new[] {batch.Operation.Parameters[1]}));

            Expression plan = null;

            ProjectionExpression projection = ProjectionFinder.FindProjection(operation);
            if (projection != null)
            {
                Scope saveScope = scope;
                ParameterExpression reader = Expression.Parameter(typeof (FieldReader), "r" + nReaders++);
                scope = new Scope(scope, reader, projection.Select.Alias, projection.Select.Columns);
                LambdaExpression projector = Expression.Lambda(Visit(projection.Projector), reader);
                scope = saveScope;

                MappingEntity entity = EntityFinder.Find(projection.Projector);
                command = new QueryCommand(command.CommandText, command.Parameters);

                plan = Expression.Call(
                    executor,
                    "ExecuteBatch",
                    new[] {projector.Body.Type},
                    Expression.Constant(command),
                    paramSets,
                    projector,
                    Expression.Constant(entity, typeof (MappingEntity)),
                    batch.BatchSize,
                    batch.Stream);
            }
            else
            {
                plan = Expression.Call(
                    executor, "ExecuteBatch", null, Expression.Constant(command), paramSets, batch.BatchSize,
                    batch.Stream);
            }

            return plan;
        }

        protected override Expression VisitCommand(CommandExpression command)
        {
            if (QueryLanguage.AllowsMultipleCommands || !IsMultipleCommands(command))
            {
                return BuildExecuteCommand(command);
            }
            return base.VisitCommand(command);
        }

        private bool IsMultipleCommands(CommandExpression command)
        {
            if (command == null)
            {
                return false;
            }
            switch ((DbExpressionType) command.NodeType)
            {
                case DbExpressionType.Insert:
                case DbExpressionType.Delete:
                case DbExpressionType.Update:
                    return false;
                default:
                    return true;
            }
        }

        protected override Expression VisitInsert(InsertCommand insert)
        {
            return BuildExecuteCommand(insert);
        }

        protected override Expression VisitUpdate(UpdateCommand update)
        {
            return BuildExecuteCommand(update);
        }

        protected override Expression VisitDelete(DeleteCommand delete)
        {
            return BuildExecuteCommand(delete);
        }

        protected override Expression VisitBlock(BlockCommand block)
        {
            return MakeSequence(VisitExpressionList(block.Commands));
        }

        protected override Expression VisitIf(IFCommand ifx)
        {
            ConditionalExpression test = Expression.Condition(
                ifx.Check,
                ifx.IfTrue,
                ifx.IfFalse != null
                    ? ifx.IfFalse
                    : ifx.IfTrue.Type == typeof (int)
                        ? Expression.Property(executor, "RowsAffected")
                        : (Expression) Expression.Constant(TypeHelper.GetDefault(ifx.IfTrue.Type), ifx.IfTrue.Type));
            return Visit(test);
        }

        protected override Expression VisitFunction(FunctionExpression func)
        {
            if (QueryLanguage.IsRowsAffectedExpressions(func))
            {
                return Expression.Property(executor, "RowsAffected");
            }
            return base.VisitFunction(func);
        }

        protected override Expression VisitExists(ExistsExpression exists)
        {
            // how did we get here? Translate exists into count query
            DbQueryType colType = DbTypeSystem.GetColumnType(typeof (int));
            SelectExpression newSelect =
                exists.Select.SetColumns(
                    new[]
                    {
                        new ColumnDeclaration("value", new AggregateExpression(typeof (int), "Count", null, false),
                            colType)
                    });

            var projection = new ProjectionExpression(
                newSelect,
                new ColumnExpression(typeof (int), colType, newSelect.Alias, "value"),
                Aggregator.GetAggregator(typeof (int), typeof (IEnumerable<int>)));

            Expression expression = projection.GreaterThan(Expression.Constant(0));

            return Visit(expression);
        }

        protected override Expression VisitDeclaration(DeclarationCommand decl)
        {
            if (decl.Source != null)
            {
                // make query that returns all these declared values as an object[]
                var projection = new ProjectionExpression(
                    decl.Source,
                    Expression.NewArrayInit(
                        typeof (object),
                        decl.Variables.Select(
                            v =>
                                v.Expression.Type.GetTypeInfo().IsValueType
                                    ? Expression.Convert(v.Expression, typeof (object))
                                    : v.Expression).ToArray()),
                    Aggregator.GetAggregator(typeof (object[]), typeof (IEnumerable<object[]>)));

                // create execution variable to hold the array of declared variables
                ParameterExpression vars = Expression.Parameter(typeof (object[]), "vars");
                variables.Add(vars);
                initializers.Add(Expression.Constant(null, typeof (object[])));

                // create subsitution for each variable (so it will find the variable value in the new vars array)
                for (int i = 0, n = decl.Variables.Count; i < n; i++)
                {
                    VariableDeclaration v = decl.Variables[i];
                    var nv = new NamedValueExpression(
                        v.Name, v.QueryType,
                        Expression.Convert(Expression.ArrayIndex(vars, Expression.Constant(i)), v.Expression.Type));
                    variableMap.Add(v.Name, nv);
                }

                // make sure the execution of the select stuffs the results into the new vars array
                return MakeAssign(vars, Visit(projection));
            }

            // probably bad if we get here since we must not allow mulitple commands
            throw new InvalidOperationException("Declaration query not allowed for this langauge");
        }

        private Expression BuildExecuteCommand(CommandExpression command)
        {
            // parameterize query
            Expression expression = Parameterize(command);

            string commandText = QueryLinguist.Format(expression);
            ReadOnlyCollection<NamedValueExpression> namedValues = NamedValueGatherer.Gather(expression);
            var qc = new QueryCommand(
                commandText, namedValues.Select(v => new QueryParameter(v.Name, v.Type, v.QueryType)));
            Expression[] values = namedValues.Select(v => Expression.Convert(Visit(v.Value), typeof (object))).ToArray();

            ProjectionExpression projection = ProjectionFinder.FindProjection(expression);
            if (projection != null)
            {
                return ExecuteProjection(projection, false, qc, values);
            }

            Expression plan = Expression.Call(
                executor, "ExecuteCommand", null, Expression.Constant(qc),
                Expression.NewArrayInit(typeof (object), values));

            return plan;
        }

        protected override Expression VisitEntity(EntityExpression entity)
        {
            return Visit(entity.Expression);
        }

        protected override Expression VisitOuterJoined(OuterJoinedExpression outer)
        {
            Expression expr = Visit(outer.Expression);
            var column = (ColumnExpression) outer.Test;
            ParameterExpression reader;
            int iOrdinal;
            if (scope.TryGetValue(column, out reader, out iOrdinal))
            {
                return Expression.Condition(
                    Expression.Call(reader, "IsDbNull", null, Expression.Constant(iOrdinal)),
                    Expression.Constant(TypeHelper.GetDefault(outer.Type), outer.Type),
                    expr);
            }
            return expr;
        }

        protected override Expression VisitColumn(ColumnExpression column)
        {
            ParameterExpression fieldReader;
            int iOrdinal;
            if (scope != null && scope.TryGetValue(column, out fieldReader, out iOrdinal))
            {
                MethodInfo method = FieldReader.GetReaderMethod(column.Type);
                return Expression.Call(fieldReader, method, Expression.Constant(iOrdinal));
            }
            Debug.Assert(false, string.Format("column not in scope: {0}", column));
            return column;
        }

        private class ColumnGatherer : DbExpressionVisitor
        {
            private readonly Dictionary<string, ColumnExpression> columns = new Dictionary<string, ColumnExpression>();

            internal static IEnumerable<ColumnExpression> Gather(Expression expression)
            {
                var gatherer = new ColumnGatherer();
                gatherer.Visit(expression);
                return gatherer.columns.Values;
            }

            protected override Expression VisitColumn(ColumnExpression column)
            {
                if (!columns.ContainsKey(column.Name))
                {
                    columns.Add(column.Name, column);
                }
                return column;
            }
        }

        private class EntityFinder : DbExpressionVisitor
        {
            private MappingEntity entity;

            public static MappingEntity Find(Expression expression)
            {
                var finder = new EntityFinder();
                finder.Visit(expression);
                return finder.entity;
            }

            protected override Expression Visit(Expression exp)
            {
                if (entity == null)
                {
                    return base.Visit(exp);
                }
                return exp;
            }

            protected override Expression VisitEntity(EntityExpression entity)
            {
                if (this.entity == null)
                {
                    this.entity = entity.Entity;
                }
                return entity;
            }

            protected override NewExpression VisitNew(NewExpression nex)
            {
                return nex;
            }

            protected override Expression VisitMemberInit(MemberInitExpression init)
            {
                return init;
            }
        }

        /// <summary>
        ///     columns referencing the outer alias are turned into special named-value parameters
        /// </summary>
        private class OuterParameterizer : DbExpressionVisitor
        {
            private readonly Dictionary<ColumnExpression, NamedValueExpression> map =
                new Dictionary<ColumnExpression, NamedValueExpression>();

            private int iParam;

            private TableAlias outerAlias;

            internal static Expression Parameterize(TableAlias outerAlias, Expression expr)
            {
                var op = new OuterParameterizer();
                op.outerAlias = outerAlias;
                return op.Visit(expr);
            }

            protected override Expression VisitProjection(ProjectionExpression proj)
            {
                var select = (SelectExpression) Visit(proj.Select);
                return UpdateProjection(proj, select, proj.Projector, proj.Aggregator);
            }

            protected override Expression VisitColumn(ColumnExpression column)
            {
                if (column.Alias == outerAlias)
                {
                    NamedValueExpression nv;
                    if (!map.TryGetValue(column, out nv))
                    {
                        nv = new NamedValueExpression("n" + (iParam++), column.QueryType, column);
                        map.Add(column, nv);
                    }
                    return nv;
                }
                return column;
            }
        }

        private class ProjectionFinder : DbExpressionVisitor
        {
            private ProjectionExpression found;

            internal static ProjectionExpression FindProjection(Expression expression)
            {
                var finder = new ProjectionFinder();
                finder.Visit(expression);
                return finder.found;
            }

            protected override Expression VisitProjection(ProjectionExpression proj)
            {
                found = proj;
                return proj;
            }
        }

        private class Scope
        {
            private readonly ParameterExpression fieldReader;

            private readonly Dictionary<string, int> nameMap;
            private readonly Scope outer;

            internal Scope(
                Scope outer, ParameterExpression fieldReader, TableAlias alias, IEnumerable<ColumnDeclaration> columns)
            {
                this.outer = outer;
                this.fieldReader = fieldReader;
                Alias = alias;
                nameMap = columns.Select((c, i) => new {c, i}).ToDictionary(x => x.c.Name, x => x.i);
            }

            internal TableAlias Alias { get; private set; }

            internal bool TryGetValue(ColumnExpression column, out ParameterExpression fieldReader, out int ordinal)
            {
                for (Scope s = this; s != null; s = s.outer)
                {
                    if (column.Alias == s.Alias && nameMap.TryGetValue(column.Name, out ordinal))
                    {
                        fieldReader = this.fieldReader;
                        return true;
                    }
                }
                fieldReader = null;
                ordinal = 0;
                return false;
            }
        }

        private class VariableSubstitutor : DbExpressionVisitor
        {
            private readonly Dictionary<string, Expression> map;

            private VariableSubstitutor(Dictionary<string, Expression> map)
            {
                this.map = map;
            }

            public static Expression Substitute(Dictionary<string, Expression> map, Expression expression)
            {
                return new VariableSubstitutor(map).Visit(expression);
            }

            protected override Expression VisitVariable(VariableExpression vex)
            {
                Expression sub;
                if (map.TryGetValue(vex.Name, out sub))
                {
                    return sub;
                }
                return vex;
            }
        }
    }
}