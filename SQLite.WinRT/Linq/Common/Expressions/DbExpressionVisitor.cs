// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using SQLite.WinRT.Linq.Base;
using ExpressionVisitor = SQLite.WinRT.Linq.Base.ExpressionVisitor;

namespace SQLite.WinRT.Linq.Common.Expressions
{
    /// <summary>
    ///     An extended expression visitor including custom DbExpression nodes
    /// </summary>
    public abstract class DbExpressionVisitor : ExpressionVisitor
    {
        protected override Expression Visit(Expression exp)
        {
            if (exp == null)
            {
                return null;
            }
            switch ((DbExpressionType) exp.NodeType)
            {
                case DbExpressionType.Table:
                    return VisitTable((TableExpression) exp);
                case DbExpressionType.Column:
                    return VisitColumn((ColumnExpression) exp);
                case DbExpressionType.Select:
                    return VisitSelect((SelectExpression) exp);
                case DbExpressionType.Join:
                    return VisitJoin((JoinExpression) exp);
                case DbExpressionType.OuterJoined:
                    return VisitOuterJoined((OuterJoinedExpression) exp);
                case DbExpressionType.Aggregate:
                    return VisitAggregate((AggregateExpression) exp);
                case DbExpressionType.Scalar:
                case DbExpressionType.Exists:
                case DbExpressionType.In:
                    return VisitSubquery((SubqueryExpression) exp);
                case DbExpressionType.AggregateSubquery:
                    return VisitAggregateSubquery((AggregateSubqueryExpression) exp);
                case DbExpressionType.IsNull:
                    return VisitIsNull((IsNullExpression) exp);
                case DbExpressionType.Between:
                    return VisitBetween((BetweenExpression) exp);
                case DbExpressionType.RowCount:
                    return VisitRowNumber((RowNumberExpression) exp);
                case DbExpressionType.Projection:
                    return VisitProjection((ProjectionExpression) exp);
                case DbExpressionType.NamedValue:
                    return VisitNamedValue((NamedValueExpression) exp);
                case DbExpressionType.ClientJoin:
                    return VisitClientJoin((ClientJoinExpression) exp);
                case DbExpressionType.Insert:
                case DbExpressionType.Update:
                case DbExpressionType.Delete:
                case DbExpressionType.If:
                case DbExpressionType.Block:
                case DbExpressionType.Declaration:
                    return VisitCommand((CommandExpression) exp);
                case DbExpressionType.Batch:
                    return VisitBatch((BatchExpression) exp);
                case DbExpressionType.Variable:
                    return VisitVariable((VariableExpression) exp);
                case DbExpressionType.Function:
                    return VisitFunction((FunctionExpression) exp);
                case DbExpressionType.Entity:
                    return VisitEntity((EntityExpression) exp);
                default:
                    return base.Visit(exp);
            }
        }

        protected virtual Expression VisitEntity(EntityExpression entity)
        {
            Expression exp = Visit(entity.Expression);
            return UpdateEntity(entity, exp);
        }

        protected EntityExpression UpdateEntity(EntityExpression entity, Expression expression)
        {
            if (expression != entity.Expression)
            {
                return new EntityExpression(entity.Entity, expression);
            }
            return entity;
        }

        protected virtual Expression VisitTable(TableExpression table)
        {
            return table;
        }

        protected virtual Expression VisitColumn(ColumnExpression column)
        {
            return column;
        }

        protected virtual Expression VisitSelect(SelectExpression select)
        {
            Expression from = VisitSource(select.From);
            Expression where = Visit(select.Where);
            ReadOnlyCollection<OrderExpression> orderBy = VisitOrderBy(select.OrderBy);
            ReadOnlyCollection<Expression> groupBy = VisitExpressionList(select.GroupBy);
            Expression skip = Visit(select.Skip);
            Expression take = Visit(select.Take);
            ReadOnlyCollection<ColumnDeclaration> columns = VisitColumnDeclarations(select.Columns);
            return UpdateSelect(
                select, from, where, orderBy, groupBy, skip, take, select.IsDistinct, select.IsReverse, columns);
        }

        protected SelectExpression UpdateSelect(
            SelectExpression select,
            Expression from,
            Expression where,
            IEnumerable<OrderExpression> orderBy,
            IEnumerable<Expression> groupBy,
            Expression skip,
            Expression take,
            bool isDistinct,
            bool isReverse,
            IEnumerable<ColumnDeclaration> columns)
        {
            if (from != select.From || where != select.Where || orderBy != select.OrderBy || groupBy != select.GroupBy
                || take != select.Take || skip != select.Skip || isDistinct != select.IsDistinct ||
                columns != select.Columns
                || isReverse != select.IsReverse)
            {
                return new SelectExpression(select.Alias, columns, from, where, orderBy, groupBy, isDistinct, skip, take,
                    isReverse);
            }
            return select;
        }

        protected virtual Expression VisitJoin(JoinExpression join)
        {
            Expression left = VisitSource(join.Left);
            Expression right = VisitSource(join.Right);
            Expression condition = Visit(join.Condition);
            return UpdateJoin(join, join.Join, left, right, condition);
        }

        protected JoinExpression UpdateJoin(
            JoinExpression join, JoinType joinType, Expression left, Expression right, Expression condition)
        {
            if (joinType != join.Join || left != join.Left || right != join.Right || condition != join.Condition)
            {
                return new JoinExpression(joinType, left, right, condition);
            }
            return join;
        }

        protected virtual Expression VisitOuterJoined(OuterJoinedExpression outer)
        {
            Expression test = Visit(outer.Test);
            Expression expression = Visit(outer.Expression);
            return UpdateOuterJoined(outer, test, expression);
        }

        protected OuterJoinedExpression UpdateOuterJoined(OuterJoinedExpression outer, Expression test,
            Expression expression)
        {
            if (test != outer.Test || expression != outer.Expression)
            {
                return new OuterJoinedExpression(test, expression);
            }
            return outer;
        }

        protected virtual Expression VisitAggregate(AggregateExpression aggregate)
        {
            Expression arg = Visit(aggregate.Argument);
            return UpdateAggregate(aggregate, aggregate.Type, aggregate.AggregateName, arg, aggregate.IsDistinct);
        }

        protected AggregateExpression UpdateAggregate(
            AggregateExpression aggregate, Type type, string aggType, Expression arg, bool isDistinct)
        {
            if (type != aggregate.Type || aggType != aggregate.AggregateName || arg != aggregate.Argument
                || isDistinct != aggregate.IsDistinct)
            {
                return new AggregateExpression(type, aggType, arg, isDistinct);
            }
            return aggregate;
        }

        protected virtual Expression VisitIsNull(IsNullExpression isnull)
        {
            Expression expr = Visit(isnull.Expression);
            return UpdateIsNull(isnull, expr);
        }

        protected IsNullExpression UpdateIsNull(IsNullExpression isnull, Expression expression)
        {
            if (expression != isnull.Expression)
            {
                return new IsNullExpression(expression);
            }
            return isnull;
        }

        protected virtual Expression VisitBetween(BetweenExpression between)
        {
            Expression expr = Visit(between.Expression);
            Expression lower = Visit(between.Lower);
            Expression upper = Visit(between.Upper);
            return UpdateBetween(between, expr, lower, upper);
        }

        protected BetweenExpression UpdateBetween(
            BetweenExpression between, Expression expression, Expression lower, Expression upper)
        {
            if (expression != between.Expression || lower != between.Lower || upper != between.Upper)
            {
                return new BetweenExpression(expression, lower, upper);
            }
            return between;
        }

        protected virtual Expression VisitRowNumber(RowNumberExpression rowNumber)
        {
            ReadOnlyCollection<OrderExpression> orderby = VisitOrderBy(rowNumber.OrderBy);
            return UpdateRowNumber(rowNumber, orderby);
        }

        protected RowNumberExpression UpdateRowNumber(RowNumberExpression rowNumber,
            IEnumerable<OrderExpression> orderBy)
        {
            if (orderBy != rowNumber.OrderBy)
            {
                return new RowNumberExpression(orderBy);
            }
            return rowNumber;
        }

        protected virtual Expression VisitNamedValue(NamedValueExpression value)
        {
            return value;
        }

        protected virtual Expression VisitSubquery(SubqueryExpression subquery)
        {
            switch ((DbExpressionType) subquery.NodeType)
            {
                case DbExpressionType.Scalar:
                    return VisitScalar((ScalarExpression) subquery);
                case DbExpressionType.Exists:
                    return VisitExists((ExistsExpression) subquery);
                case DbExpressionType.In:
                    return VisitIn((InExpression) subquery);
            }
            return subquery;
        }

        protected virtual Expression VisitScalar(ScalarExpression scalar)
        {
            var select = (SelectExpression) Visit(scalar.Select);
            return UpdateScalar(scalar, select);
        }

        protected ScalarExpression UpdateScalar(ScalarExpression scalar, SelectExpression select)
        {
            if (select != scalar.Select)
            {
                return new ScalarExpression(scalar.Type, select);
            }
            return scalar;
        }

        protected virtual Expression VisitExists(ExistsExpression exists)
        {
            var select = (SelectExpression) Visit(exists.Select);
            return UpdateExists(exists, select);
        }

        protected ExistsExpression UpdateExists(ExistsExpression exists, SelectExpression select)
        {
            if (select != exists.Select)
            {
                return new ExistsExpression(select);
            }
            return exists;
        }

        protected virtual Expression VisitIn(InExpression @in)
        {
            Expression expr = Visit(@in.Expression);
            var select = (SelectExpression) Visit(@in.Select);
            ReadOnlyCollection<Expression> values = VisitExpressionList(@in.Values);
            return UpdateIn(@in, expr, select, values);
        }

        protected InExpression UpdateIn(
            InExpression @in, Expression expression, SelectExpression select, IEnumerable<Expression> values)
        {
            if (expression != @in.Expression || select != @in.Select || values != @in.Values)
            {
                if (select != null)
                {
                    return new InExpression(expression, select);
                }
                return new InExpression(expression, values);
            }
            return @in;
        }

        protected virtual Expression VisitAggregateSubquery(AggregateSubqueryExpression aggregate)
        {
            var subquery = (ScalarExpression) Visit(aggregate.AggregateAsSubquery);
            return UpdateAggregateSubquery(aggregate, subquery);
        }

        protected AggregateSubqueryExpression UpdateAggregateSubquery(
            AggregateSubqueryExpression aggregate, ScalarExpression subquery)
        {
            if (subquery != aggregate.AggregateAsSubquery)
            {
                return new AggregateSubqueryExpression(aggregate.GroupByAlias, aggregate.AggregateInGroupSelect,
                    subquery);
            }
            return aggregate;
        }

        protected virtual Expression VisitSource(Expression source)
        {
            return Visit(source);
        }

        protected virtual Expression VisitProjection(ProjectionExpression proj)
        {
            var select = (SelectExpression) Visit(proj.Select);
            Expression projector = Visit(proj.Projector);
            return UpdateProjection(proj, select, projector, proj.Aggregator);
        }

        protected ProjectionExpression UpdateProjection(
            ProjectionExpression proj, SelectExpression select, Expression projector, LambdaExpression aggregator)
        {
            if (select != proj.Select || projector != proj.Projector || aggregator != proj.Aggregator)
            {
                return new ProjectionExpression(select, projector, aggregator);
            }
            return proj;
        }

        protected virtual Expression VisitClientJoin(ClientJoinExpression join)
        {
            var projection = (ProjectionExpression) Visit(join.Projection);
            ReadOnlyCollection<Expression> outerKey = VisitExpressionList(join.OuterKey);
            ReadOnlyCollection<Expression> innerKey = VisitExpressionList(join.InnerKey);
            return UpdateClientJoin(join, projection, outerKey, innerKey);
        }

        protected ClientJoinExpression UpdateClientJoin(
            ClientJoinExpression join,
            ProjectionExpression projection,
            IEnumerable<Expression> outerKey,
            IEnumerable<Expression> innerKey)
        {
            if (projection != join.Projection || outerKey != join.OuterKey || innerKey != join.InnerKey)
            {
                return new ClientJoinExpression(projection, outerKey, innerKey);
            }
            return join;
        }

        protected virtual Expression VisitCommand(CommandExpression command)
        {
            switch ((DbExpressionType) command.NodeType)
            {
                case DbExpressionType.Insert:
                    return VisitInsert((InsertCommand) command);
                case DbExpressionType.Update:
                    return VisitUpdate((UpdateCommand) command);
                case DbExpressionType.Delete:
                    return VisitDelete((DeleteCommand) command);
                case DbExpressionType.If:
                    return VisitIf((IFCommand) command);
                case DbExpressionType.Block:
                    return VisitBlock((BlockCommand) command);
                case DbExpressionType.Declaration:
                    return VisitDeclaration((DeclarationCommand) command);
                default:
                    return VisitUnknown(command);
            }
        }

        protected virtual Expression VisitInsert(InsertCommand insert)
        {
            var table = (TableExpression) Visit(insert.Table);
            ReadOnlyCollection<ColumnAssignment> assignments = VisitColumnAssignments(insert.Assignments);
            return UpdateInsert(insert, table, assignments);
        }

        protected InsertCommand UpdateInsert(
            InsertCommand insert, TableExpression table, IEnumerable<ColumnAssignment> assignments)
        {
            if (table != insert.Table || assignments != insert.Assignments)
            {
                return new InsertCommand(table, assignments);
            }
            return insert;
        }

        protected virtual Expression VisitUpdate(UpdateCommand update)
        {
            var table = (TableExpression) Visit(update.Table);
            Expression where = Visit(update.Where);
            ReadOnlyCollection<ColumnAssignment> assignments = VisitColumnAssignments(update.Assignments);
            return UpdateUpdate(update, table, where, assignments);
        }

        protected UpdateCommand UpdateUpdate(
            UpdateCommand update, TableExpression table, Expression where, IEnumerable<ColumnAssignment> assignments)
        {
            if (table != update.Table || where != update.Where || assignments != update.Assignments)
            {
                return new UpdateCommand(table, where, assignments);
            }
            return update;
        }

        protected virtual Expression VisitDelete(DeleteCommand delete)
        {
            var table = (TableExpression) Visit(delete.Table);
            Expression where = Visit(delete.Where);
            return UpdateDelete(delete, table, where);
        }

        protected DeleteCommand UpdateDelete(DeleteCommand delete, TableExpression table, Expression where)
        {
            if (table != delete.Table || where != delete.Where)
            {
                return new DeleteCommand(table, where);
            }
            return delete;
        }

        protected virtual Expression VisitBatch(BatchExpression batch)
        {
            var operation = (LambdaExpression) Visit(batch.Operation);
            Expression batchSize = Visit(batch.BatchSize);
            Expression stream = Visit(batch.Stream);
            return UpdateBatch(batch, batch.Input, operation, batchSize, stream);
        }

        protected BatchExpression UpdateBatch(
            BatchExpression batch, Expression input, LambdaExpression operation, Expression batchSize, Expression stream)
        {
            if (input != batch.Input || operation != batch.Operation || batchSize != batch.BatchSize ||
                stream != batch.Stream)
            {
                return new BatchExpression(input, operation, batchSize, stream);
            }
            return batch;
        }

        protected virtual Expression VisitIf(IFCommand ifx)
        {
            Expression check = Visit(ifx.Check);
            Expression ifTrue = Visit(ifx.IfTrue);
            Expression ifFalse = Visit(ifx.IfFalse);
            return UpdateIf(ifx, check, ifTrue, ifFalse);
        }

        protected IFCommand UpdateIf(IFCommand ifx, Expression check, Expression ifTrue, Expression ifFalse)
        {
            if (check != ifx.Check || ifTrue != ifx.IfTrue || ifFalse != ifx.IfFalse)
            {
                return new IFCommand(check, ifTrue, ifFalse);
            }
            return ifx;
        }

        protected virtual Expression VisitBlock(BlockCommand block)
        {
            ReadOnlyCollection<Expression> commands = VisitExpressionList(block.Commands);
            return UpdateBlock(block, commands);
        }

        protected BlockCommand UpdateBlock(BlockCommand block, IList<Expression> commands)
        {
            if (block.Commands != commands)
            {
                return new BlockCommand(commands);
            }
            return block;
        }

        protected virtual Expression VisitDeclaration(DeclarationCommand decl)
        {
            ReadOnlyCollection<VariableDeclaration> variables = VisitVariableDeclarations(decl.Variables);
            var source = (SelectExpression) Visit(decl.Source);
            return UpdateDeclaration(decl, variables, source);
        }

        protected DeclarationCommand UpdateDeclaration(
            DeclarationCommand decl, IEnumerable<VariableDeclaration> variables, SelectExpression source)
        {
            if (variables != decl.Variables || source != decl.Source)
            {
                return new DeclarationCommand(variables, source);
            }
            return decl;
        }

        protected virtual Expression VisitVariable(VariableExpression vex)
        {
            return vex;
        }

        protected virtual Expression VisitFunction(FunctionExpression func)
        {
            ReadOnlyCollection<Expression> arguments = VisitExpressionList(func.Arguments);
            return UpdateFunction(func, func.Name, arguments);
        }

        protected FunctionExpression UpdateFunction(FunctionExpression func, string name,
            IEnumerable<Expression> arguments)
        {
            if (name != func.Name || arguments != func.Arguments)
            {
                return new FunctionExpression(func.Type, name, arguments);
            }
            return func;
        }

        protected virtual ColumnAssignment VisitColumnAssignment(ColumnAssignment ca)
        {
            var c = (ColumnExpression) Visit(ca.Column);
            Expression e = Visit(ca.Expression);
            return UpdateColumnAssignment(ca, c, e);
        }

        protected ColumnAssignment UpdateColumnAssignment(ColumnAssignment ca, ColumnExpression c, Expression e)
        {
            if (c != ca.Column || e != ca.Expression)
            {
                return new ColumnAssignment(c, e);
            }
            return ca;
        }

        protected virtual ReadOnlyCollection<ColumnAssignment> VisitColumnAssignments(
            ReadOnlyCollection<ColumnAssignment> assignments)
        {
            List<ColumnAssignment> alternate = null;
            for (int i = 0, n = assignments.Count; i < n; i++)
            {
                ColumnAssignment assignment = VisitColumnAssignment(assignments[i]);
                if (alternate == null && assignment != assignments[i])
                {
                    alternate = assignments.Take(i).ToList();
                }
                if (alternate != null)
                {
                    alternate.Add(assignment);
                }
            }
            if (alternate != null)
            {
                return alternate.AsReadOnly();
            }
            return assignments;
        }

        protected virtual ReadOnlyCollection<ColumnDeclaration> VisitColumnDeclarations(
            ReadOnlyCollection<ColumnDeclaration> columns)
        {
            List<ColumnDeclaration> alternate = null;
            for (int i = 0, n = columns.Count; i < n; i++)
            {
                ColumnDeclaration column = columns[i];
                Expression e = Visit(column.Expression);
                if (alternate == null && e != column.Expression)
                {
                    alternate = columns.Take(i).ToList();
                }
                if (alternate != null)
                {
                    alternate.Add(new ColumnDeclaration(column.Name, e, column.QueryType));
                }
            }
            if (alternate != null)
            {
                return alternate.AsReadOnly();
            }
            return columns;
        }

        protected virtual ReadOnlyCollection<VariableDeclaration> VisitVariableDeclarations(
            ReadOnlyCollection<VariableDeclaration> decls)
        {
            List<VariableDeclaration> alternate = null;
            for (int i = 0, n = decls.Count; i < n; i++)
            {
                VariableDeclaration decl = decls[i];
                Expression e = Visit(decl.Expression);
                if (alternate == null && e != decl.Expression)
                {
                    alternate = decls.Take(i).ToList();
                }
                if (alternate != null)
                {
                    alternate.Add(new VariableDeclaration(decl.Name, decl.QueryType, e));
                }
            }
            if (alternate != null)
            {
                return alternate.AsReadOnly();
            }
            return decls;
        }

        protected virtual ReadOnlyCollection<OrderExpression> VisitOrderBy(
            ReadOnlyCollection<OrderExpression> expressions)
        {
            if (expressions != null)
            {
                List<OrderExpression> alternate = null;
                for (int i = 0, n = expressions.Count; i < n; i++)
                {
                    OrderExpression expr = expressions[i];
                    Expression e = Visit(expr.Expression);
                    if (alternate == null && e != expr.Expression)
                    {
                        alternate = expressions.Take(i).ToList();
                    }
                    if (alternate != null)
                    {
                        alternate.Add(new OrderExpression(expr.OrderType, e));
                    }
                }
                if (alternate != null)
                {
                    return alternate.AsReadOnly();
                }
            }
            return expressions;
        }
    }
}