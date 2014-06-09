// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using SQLite.WinRT.Linq.Base;
using SQLite.WinRT.Linq.Common.Language;
using SQLite.WinRT.Linq.Common.Mapping;

namespace SQLite.WinRT.Linq.Common.Expressions
{
	/// <summary>
	/// Extended node types for custom expressions
	/// </summary>
	public enum DbExpressionType
	{
		Table = 1000, // make sure these don't overlap with ExpressionType
		ClientJoin,

		Column,

		Select,

		Projection,

		Entity,

		Join,

		Aggregate,

		Scalar,

		Exists,

		In,

		Grouping,

		AggregateSubquery,

		IsNull,

		Between,

		RowCount,

		NamedValue,

		OuterJoined,

		Insert,

		Update,

		Delete,

		Batch,

		Function,

		Block,

		If,

		Declaration,

		Variable
	}

	public static class DbExpressionTypeExtensions
	{
		public static bool IsDbExpression(this ExpressionType et)
		{
			return ((int)et) >= 1000;
		}
	}

	public abstract class DbExpression : Expression
	{
		private readonly Type type;

		protected DbExpression(Type type)
		{
			this.type = type;
		}

		public override Type Type
		{
			get
			{
				return this.type;
			}
		}

		public override string ToString()
		{
			return DbExpressionWriter.WriteToString(this);
		}
	}

	/// <summary>
	/// A base class for expressions that declare table aliases.
	/// </summary>
	public abstract class AliasedExpression : DbExpression
	{
		private readonly TableAlias alias;

		protected AliasedExpression(Type type, TableAlias alias)
			: base(type)
		{
			this.alias = alias;
		}

		public TableAlias Alias
		{
			get
			{
				return this.alias;
			}
		}
	}

	/// <summary>
	/// A custom expression node that represents a table reference in a SQL query
	/// </summary>
	public class TableExpression : AliasedExpression
	{
		private readonly MappingEntity entity;

		private readonly string name;

		public TableExpression(TableAlias alias, MappingEntity entity, string name)
			: base(typeof(void), alias)
		{
			this.entity = entity;
			this.name = name;
		}

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)DbExpressionType.Table;
			}
		}

		public MappingEntity Entity
		{
			get
			{
				return this.entity;
			}
		}

		public string Name
		{
			get
			{
				return this.name;
			}
		}

		public override string ToString()
		{
			return "T(" + this.Name + ")";
		}
	}

	/// <summary>
	/// An expression node that introduces an entity mapping.
	/// </summary>
	public class EntityExpression : DbExpression
	{
		private readonly MappingEntity entity;

		private readonly Expression expression;

		public EntityExpression(MappingEntity entity, Expression expression)
			: base(expression.Type)
		{
			this.entity = entity;
			this.expression = expression;
		}

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)DbExpressionType.Entity;
			}
		}

		public MappingEntity Entity
		{
			get
			{
				return this.entity;
			}
		}

		public Expression Expression
		{
			get
			{
				return this.expression;
			}
		}
	}

	/// <summary>
	/// A custom expression node that represents a reference to a column in a SQL query
	/// </summary>
	public class ColumnExpression : DbExpression, IEquatable<ColumnExpression>
	{
		public ColumnExpression(Type type, DbQueryType queryType, TableAlias alias, string name)
			: base(type)
		{
			if (queryType == null)
			{
				throw new ArgumentNullException("queryType");
			}
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			this.Alias = alias;
			this.Name = name;
			this.QueryType = queryType;
		}

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)DbExpressionType.Column;
			}
		}

		public TableAlias Alias { get; private set; }

		public string Name { get; private set; }

		public DbQueryType QueryType { get; private set; }

		public override string ToString()
		{
			return this.Alias.ToString() + ".C(" + this.Name + ")";
		}

		public override int GetHashCode()
		{
			return this.Alias.GetHashCode() + this.Name.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as ColumnExpression);
		}

		public bool Equals(ColumnExpression other)
		{
			return other != null && ((object)this) == (object)other || (this.Alias == other.Alias && this.Name == other.Name);
		}
	}

	/// <summary>
	/// An alias for a table.
	/// </summary>
	public class TableAlias
	{
		public override string ToString()
		{
			return "A:" + this.GetHashCode();
		}
	}

	/// <summary>
	/// A declaration of a column in a SQL SELECT expression
	/// </summary>
	public class ColumnDeclaration
	{
		public ColumnDeclaration(string name, Expression expression, DbQueryType queryType)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			if (expression == null)
			{
				throw new ArgumentNullException("expression");
			}
			if (queryType == null)
			{
				throw new ArgumentNullException("queryType");
			}
			this.Name = name;
			this.Expression = expression;
			this.QueryType = queryType;
		}

		public string Name { get; private set; }

		public Expression Expression { get; private set; }

		public DbQueryType QueryType { get; private set; }
	}

	/// <summary>
	/// An SQL OrderBy order type 
	/// </summary>
	public enum OrderType
	{
		Ascending,

		Descending
	}

	/// <summary>
	/// A pairing of an expression and an order type for use in a SQL Order By clause
	/// </summary>
	public class OrderExpression
	{
		private readonly OrderType orderType;

		private readonly Expression expression;

		public OrderExpression(OrderType orderType, Expression expression)
		{
			this.orderType = orderType;
			this.expression = expression;
		}

		public OrderType OrderType
		{
			get
			{
				return this.orderType;
			}
		}

		public Expression Expression
		{
			get
			{
				return this.expression;
			}
		}
	}

	/// <summary>
	/// A custom expression node used to represent a SQL SELECT expression
	/// </summary>
	public class SelectExpression : AliasedExpression
	{
		private readonly ReadOnlyCollection<ColumnDeclaration> columns;

		private readonly bool isDistinct;

		private readonly Expression from;

		private readonly Expression where;

		private readonly ReadOnlyCollection<OrderExpression> orderBy;

		private readonly ReadOnlyCollection<Expression> groupBy;

		private readonly Expression take;

		private readonly Expression skip;

		private readonly bool reverse;

		public SelectExpression(
			TableAlias alias,
			IEnumerable<ColumnDeclaration> columns,
			Expression from,
			Expression where,
			IEnumerable<OrderExpression> orderBy,
			IEnumerable<Expression> groupBy,
			bool isDistinct,
			Expression skip,
			Expression take,
			bool reverse)
			: base(typeof(void), alias)
		{
			this.columns = columns.ToReadOnly();
			this.isDistinct = isDistinct;
			this.from = from;
			this.where = where;
			this.orderBy = orderBy.ToReadOnly();
			this.groupBy = groupBy.ToReadOnly();
			this.take = take;
			this.skip = skip;
			this.reverse = reverse;
		}

		public SelectExpression(
			TableAlias alias,
			IEnumerable<ColumnDeclaration> columns,
			Expression from,
			Expression where,
			IEnumerable<OrderExpression> orderBy,
			IEnumerable<Expression> groupBy)
			: this(alias, columns, from, where, orderBy, groupBy, false, null, null, false)
		{
		}

		public SelectExpression(TableAlias alias, IEnumerable<ColumnDeclaration> columns, Expression from, Expression where)
			: this(alias, columns, from, where, null, null)
		{
		}

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)DbExpressionType.Select;
			}
		}

		public ReadOnlyCollection<ColumnDeclaration> Columns
		{
			get
			{
				return this.columns;
			}
		}

		public Expression From
		{
			get
			{
				return this.from;
			}
		}

		public Expression Where
		{
			get
			{
				return this.where;
			}
		}

		public ReadOnlyCollection<OrderExpression> OrderBy
		{
			get
			{
				return this.orderBy;
			}
		}

		public ReadOnlyCollection<Expression> GroupBy
		{
			get
			{
				return this.groupBy;
			}
		}

		public bool IsDistinct
		{
			get
			{
				return this.isDistinct;
			}
		}

		public Expression Skip
		{
			get
			{
				return this.skip;
			}
		}

		public Expression Take
		{
			get
			{
				return this.take;
			}
		}

		public bool IsReverse
		{
			get
			{
				return this.reverse;
			}
		}

		public string QueryText
		{
			get
			{
				return SqlFormatter.Format(this, true);
			}
		}
	}

	/// <summary>
	/// A kind of SQL join
	/// </summary>
	public enum JoinType
	{
		CrossJoin,

		InnerJoin,

		CrossApply,

		OuterApply,

		LeftOuter,

		SingletonLeftOuter
	}

	/// <summary>
	/// A SQL join clause expression
	/// </summary>
	public class JoinExpression : DbExpression
	{
		private readonly JoinType joinType;

		private readonly Expression left;

		private readonly Expression right;

		private readonly Expression condition;

		public JoinExpression(JoinType joinType, Expression left, Expression right, Expression condition)
			: base(typeof(void))
		{
			this.joinType = joinType;
			this.left = left;
			this.right = right;
			this.condition = condition;
		}

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)DbExpressionType.Join;
			}
		}

		public JoinType Join
		{
			get
			{
				return this.joinType;
			}
		}

		public Expression Left
		{
			get
			{
				return this.left;
			}
		}

		public Expression Right
		{
			get
			{
				return this.right;
			}
		}

		public new Expression Condition
		{
			get
			{
				return this.condition;
			}
		}
	}

	/// <summary>
	/// A wrapper around and expression that is part of an outer joined projection
	/// including a test expression to determine if the expression ought to be considered null.
	/// </summary>
	public class OuterJoinedExpression : DbExpression
	{
		private readonly Expression test;

		private readonly Expression expression;

		public OuterJoinedExpression(Expression test, Expression expression)
			: base(expression.Type)
		{
			this.test = test;
			this.expression = expression;
		}

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)DbExpressionType.OuterJoined;
			}
		}

		public Expression Test
		{
			get
			{
				return this.test;
			}
		}

		public Expression Expression
		{
			get
			{
				return this.expression;
			}
		}
	}

	/// <summary>
	/// An base class for SQL subqueries.
	/// </summary>
	public abstract class SubqueryExpression : DbExpression
	{
		private readonly SelectExpression select;

		protected SubqueryExpression(Type type, SelectExpression select)
			: base(type)
		{
			this.select = select;
		}

		public SelectExpression Select
		{
			get
			{
				return this.select;
			}
		}
	}

	/// <summary>
	/// A SQL scalar subquery expression:
	///   exists(select x from y where z)
	/// </summary>
	public class ScalarExpression : SubqueryExpression
	{
		public ScalarExpression(Type type, SelectExpression select)
			: base(type, select)
		{
		}

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)DbExpressionType.Scalar;
			}
		}
	}

	/// <summary>
	/// A SQL Exists subquery expression.
	/// </summary>
	public class ExistsExpression : SubqueryExpression
	{
		public ExistsExpression(SelectExpression select)
			: base(typeof(bool), select)
		{
		}

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)DbExpressionType.Exists;
			}
		}
	}

	/// <summary>
	/// A SQL 'In' subquery:
	///   expr in (select x from y where z)
	///   expr in (a, b, c)
	/// </summary>
	public class InExpression : SubqueryExpression
	{
		private readonly Expression expression;

		private readonly ReadOnlyCollection<Expression> values; // either select or expressions are assigned

		public InExpression(Expression expression, SelectExpression select)
			: base(typeof(bool), select)
		{
			this.expression = expression;
		}

		public InExpression(Expression expression, IEnumerable<Expression> values)
			: base(typeof(bool), null)
		{
			this.expression = expression;
			this.values = values.ToReadOnly();
		}

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)DbExpressionType.In;
			}
		}

		public Expression Expression
		{
			get
			{
				return this.expression;
			}
		}

		public ReadOnlyCollection<Expression> Values
		{
			get
			{
				return this.values;
			}
		}
	}

	/// <summary>
	/// An SQL Aggregate expression:
	///     MIN, MAX, AVG, COUNT
	/// </summary>
	public class AggregateExpression : DbExpression
	{
		private readonly string aggregateName;

		private readonly Expression argument;

		private readonly bool isDistinct;

		public AggregateExpression(Type type, string aggregateName, Expression argument, bool isDistinct)
			: base(type)
		{
			this.aggregateName = aggregateName;
			this.argument = argument;
			this.isDistinct = isDistinct;
		}

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)DbExpressionType.Aggregate;
			}
		}

		public string AggregateName
		{
			get
			{
				return this.aggregateName;
			}
		}

		public Expression Argument
		{
			get
			{
				return this.argument;
			}
		}

		public bool IsDistinct
		{
			get
			{
				return this.isDistinct;
			}
		}
	}

	public class AggregateSubqueryExpression : DbExpression
	{
		private readonly TableAlias groupByAlias;

		private readonly Expression aggregateInGroupSelect;

		private readonly ScalarExpression aggregateAsSubquery;

		public AggregateSubqueryExpression(
			TableAlias groupByAlias, Expression aggregateInGroupSelect, ScalarExpression aggregateAsSubquery)
			: base(aggregateAsSubquery.Type)
		{
			this.aggregateInGroupSelect = aggregateInGroupSelect;
			this.groupByAlias = groupByAlias;
			this.aggregateAsSubquery = aggregateAsSubquery;
		}

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)DbExpressionType.AggregateSubquery;
			}
		}

		public TableAlias GroupByAlias
		{
			get
			{
				return this.groupByAlias;
			}
		}

		public Expression AggregateInGroupSelect
		{
			get
			{
				return this.aggregateInGroupSelect;
			}
		}

		public ScalarExpression AggregateAsSubquery
		{
			get
			{
				return this.aggregateAsSubquery;
			}
		}
	}

	/// <summary>
	/// Allows is-null tests against value-types like int and float
	/// </summary>
	public class IsNullExpression : DbExpression
	{
		private readonly Expression expression;

		public IsNullExpression(Expression expression)
			: base(typeof(bool))
		{
			this.expression = expression;
		}

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)DbExpressionType.IsNull;
			}
		}

		public Expression Expression
		{
			get
			{
				return this.expression;
			}
		}
	}

	public class BetweenExpression : DbExpression
	{
		private readonly Expression expression;

		private readonly Expression lower;

		private readonly Expression upper;

		public BetweenExpression(Expression expression, Expression lower, Expression upper)
			: base(expression.Type)
		{
			this.expression = expression;
			this.lower = lower;
			this.upper = upper;
		}

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)DbExpressionType.Between;
			}
		}

		public Expression Expression
		{
			get
			{
				return this.expression;
			}
		}

		public Expression Lower
		{
			get
			{
				return this.lower;
			}
		}

		public Expression Upper
		{
			get
			{
				return this.upper;
			}
		}
	}

	public class RowNumberExpression : DbExpression
	{
		private readonly ReadOnlyCollection<OrderExpression> orderBy;

		public RowNumberExpression(IEnumerable<OrderExpression> orderBy)
			: base(typeof(int))
		{
			this.orderBy = orderBy.ToReadOnly();
		}

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)DbExpressionType.RowCount;
			}
		}

		public ReadOnlyCollection<OrderExpression> OrderBy
		{
			get
			{
				return this.orderBy;
			}
		}
	}

	public class NamedValueExpression : DbExpression
	{
		public NamedValueExpression(string name, DbQueryType queryType, Expression value)
			: base(value.Type)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			this.Name = name;
			this.QueryType = queryType;
			this.Value = value;
		}

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)DbExpressionType.NamedValue;
			}
		}

		public string Name { get; private set; }

		public DbQueryType QueryType { get; private set; }

		public Expression Value { get; private set; }
	}

	/// <summary>
	/// A custom expression representing the construction of one or more result objects from a 
	/// SQL select expression
	/// </summary>
	public class ProjectionExpression : DbExpression
	{
		private readonly SelectExpression select;

		private readonly Expression projector;

		private readonly LambdaExpression aggregator;

		public ProjectionExpression(SelectExpression source, Expression projector)
			: this(source, projector, null)
		{
		}

		public ProjectionExpression(SelectExpression source, Expression projector, LambdaExpression aggregator)
			: base(aggregator != null ? aggregator.Body.Type : typeof(IEnumerable<>).MakeGenericType(projector.Type))
		{
			this.select = source;
			this.projector = projector;
			this.aggregator = aggregator;
		}

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)DbExpressionType.Projection;
			}
		}

		public SelectExpression Select
		{
			get
			{
				return this.select;
			}
		}

		public Expression Projector
		{
			get
			{
				return this.projector;
			}
		}

		public LambdaExpression Aggregator
		{
			get
			{
				return this.aggregator;
			}
		}

		public bool IsSingleton
		{
			get
			{
				return this.aggregator != null && this.aggregator.Body.Type == projector.Type;
			}
		}

		public override string ToString()
		{
			return DbExpressionWriter.WriteToString(this);
		}

		public string QueryText
		{
			get
			{
				return SqlFormatter.Format(select, true);
			}
		}
	}

	public class ClientJoinExpression : DbExpression
	{
		private readonly ReadOnlyCollection<Expression> outerKey;

		private readonly ReadOnlyCollection<Expression> innerKey;

		private readonly ProjectionExpression projection;

		public ClientJoinExpression(
			ProjectionExpression projection, IEnumerable<Expression> outerKey, IEnumerable<Expression> innerKey)
			: base(projection.Type)
		{
			this.outerKey = outerKey.ToReadOnly();
			this.innerKey = innerKey.ToReadOnly();
			this.projection = projection;
		}

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)DbExpressionType.ClientJoin;
			}
		}

		public ReadOnlyCollection<Expression> OuterKey
		{
			get
			{
				return this.outerKey;
			}
		}

		public ReadOnlyCollection<Expression> InnerKey
		{
			get
			{
				return this.innerKey;
			}
		}

		public ProjectionExpression Projection
		{
			get
			{
				return this.projection;
			}
		}
	}

	public class BatchExpression : Expression
	{
		private readonly Type type;

		private readonly Expression input;

		private readonly LambdaExpression operation;

		private readonly Expression batchSize;

		private readonly Expression stream;

		public BatchExpression(Expression input, LambdaExpression operation, Expression batchSize, Expression stream)
		{
			this.input = input;
			this.operation = operation;
			this.batchSize = batchSize;
			this.stream = stream;
			this.type = typeof(IEnumerable<>).MakeGenericType(operation.Body.Type);
		}

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)DbExpressionType.Batch;
			}
		}

		public override Type Type
		{
			get
			{
				return this.type;
			}
		}

		public Expression Input
		{
			get
			{
				return this.input;
			}
		}

		public LambdaExpression Operation
		{
			get
			{
				return this.operation;
			}
		}

		public Expression BatchSize
		{
			get
			{
				return this.batchSize;
			}
		}

		public Expression Stream
		{
			get
			{
				return this.stream;
			}
		}
	}

	public class FunctionExpression : DbExpression
	{
		private readonly string name;

		private readonly ReadOnlyCollection<Expression> arguments;

		public FunctionExpression(Type type, string name, IEnumerable<Expression> arguments)
			: base(type)
		{
			this.name = name;
			this.arguments = arguments.ToReadOnly();
		}

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)DbExpressionType.Function;
			}
		}

		public string Name
		{
			get
			{
				return this.name;
			}
		}

		public ReadOnlyCollection<Expression> Arguments
		{
			get
			{
				return this.arguments;
			}
		}
	}

	public abstract class CommandExpression : DbExpression
	{
		protected CommandExpression(Type type)
			: base(type)
		{
		}
	}

	public class InsertCommand : CommandExpression
	{
		private readonly TableExpression table;

		private readonly ReadOnlyCollection<ColumnAssignment> assignments;

		public InsertCommand(TableExpression table, IEnumerable<ColumnAssignment> assignments)
			: base(typeof(int))
		{
			this.table = table;
			this.assignments = assignments.ToReadOnly();
		}

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)DbExpressionType.Insert;
			}
		}

		public TableExpression Table
		{
			get
			{
				return this.table;
			}
		}

		public ReadOnlyCollection<ColumnAssignment> Assignments
		{
			get
			{
				return this.assignments;
			}
		}
	}

	public class ColumnAssignment
	{
		private readonly ColumnExpression column;

		private readonly Expression expression;

		public ColumnAssignment(ColumnExpression column, Expression expression)
		{
			this.column = column;
			this.expression = expression;
		}

		public ColumnExpression Column
		{
			get
			{
				return this.column;
			}
		}

		public Expression Expression
		{
			get
			{
				return this.expression;
			}
		}
	}

	public class UpdateCommand : CommandExpression
	{
		private readonly TableExpression table;

		private readonly Expression where;

		private readonly ReadOnlyCollection<ColumnAssignment> assignments;

		public UpdateCommand(TableExpression table, Expression where, IEnumerable<ColumnAssignment> assignments)
			: base(typeof(int))
		{
			this.table = table;
			this.where = where;
			this.assignments = assignments.ToReadOnly();
		}

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)DbExpressionType.Update;
			}
		}

		public TableExpression Table
		{
			get
			{
				return this.table;
			}
		}

		public Expression Where
		{
			get
			{
				return this.where;
			}
		}

		public ReadOnlyCollection<ColumnAssignment> Assignments
		{
			get
			{
				return this.assignments;
			}
		}
	}

	public class DeleteCommand : CommandExpression
	{
		private readonly TableExpression table;

		private readonly Expression where;

		public DeleteCommand(TableExpression table, Expression where)
			: base(typeof(int))
		{
			this.table = table;
			this.where = where;
		}

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)DbExpressionType.Delete;
			}
		}

		public TableExpression Table
		{
			get
			{
				return this.table;
			}
		}

		public Expression Where
		{
			get
			{
				return this.where;
			}
		}
	}

	public class IFCommand : CommandExpression
	{
		private readonly Expression check;

		private readonly Expression ifTrue;

		private readonly Expression ifFalse;

		public IFCommand(Expression check, Expression ifTrue, Expression ifFalse)
			: base(ifTrue.Type)
		{
			this.check = check;
			this.ifTrue = ifTrue;
			this.ifFalse = ifFalse;
		}

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)DbExpressionType.If;
			}
		}

		public Expression Check
		{
			get
			{
				return this.check;
			}
		}

		public Expression IfTrue
		{
			get
			{
				return this.ifTrue;
			}
		}

		public Expression IfFalse
		{
			get
			{
				return this.ifFalse;
			}
		}
	}

	public class BlockCommand : CommandExpression
	{
		private readonly ReadOnlyCollection<Expression> commands;

		public BlockCommand(IList<Expression> commands)
			: base(commands[commands.Count - 1].Type)
		{
			this.commands = commands.ToReadOnly();
		}

		public BlockCommand(params Expression[] commands)
			: this((IList<Expression>)commands)
		{
		}

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)DbExpressionType.Block;
			}
		}

		public ReadOnlyCollection<Expression> Commands
		{
			get
			{
				return this.commands;
			}
		}
	}

	public class DeclarationCommand : CommandExpression
	{
		private readonly ReadOnlyCollection<VariableDeclaration> variables;

		private readonly SelectExpression source;

		public DeclarationCommand(IEnumerable<VariableDeclaration> variables, SelectExpression source)
			: base(typeof(void))
		{
			this.variables = variables.ToReadOnly();
			this.source = source;
		}

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)DbExpressionType.Declaration;
			}
		}

		public ReadOnlyCollection<VariableDeclaration> Variables
		{
			get
			{
				return this.variables;
			}
		}

		public SelectExpression Source
		{
			get
			{
				return this.source;
			}
		}
	}

	public class VariableDeclaration
	{
		public VariableDeclaration(string name, DbQueryType type, Expression expression)
		{
			this.Name = name;
			this.QueryType = type;
			this.Expression = expression;
		}

		public string Name { get; private set; }

		public DbQueryType QueryType { get; private set; }

		public Expression Expression { get; private set; }
	}

	public class VariableExpression : Expression
	{
		private readonly Type type;

		public VariableExpression(string name, Type type, DbQueryType queryType)
		{
			this.Name = name;
			this.type = type;
			this.QueryType = queryType;
		}

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)DbExpressionType.Variable;
			}
		}

		public override Type Type
		{
			get
			{
				return this.type;
			}
		}

		public string Name { get; private set; }

		public DbQueryType QueryType { get; private set; }
	}
}