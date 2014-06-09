// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using SQLite.WinRT.Linq.Base;
using SQLite.WinRT.Linq.Common;
using SQLite.WinRT.Linq.Common.Expressions;
using SQLite.WinRT.Linq.Common.Mapping;
using SQLite.WinRT.Linq.Mapping;

namespace SQLite.WinRT.Linq
{

	/// <summary>
	/// A LINQ IQueryable query provider that executes database queries over a SqliteConnection
	/// </summary>
	public class EntityProvider : IAsyncQueryProvider, IEntityProvider, IQueryProvider
	{
	    private readonly SQLiteConnection connection;
	    private EntityPolicy policy;

		IQueryable<S> IQueryProvider.CreateQuery<S>(Expression expression)
		{
			return new Query<S>(this, expression);
		}

		IQueryable IQueryProvider.CreateQuery(Expression expression)
		{
			Type elementType = TypeHelper.GetElementType(expression.Type);
			try
			{
				return
					(IQueryable)
					Activator.CreateInstance(typeof(Query<>).MakeGenericType(elementType), new object[] { this, expression });
			}
			catch (TargetInvocationException tie)
			{
				throw tie.InnerException;
			}
		}

		S IQueryProvider.Execute<S>(Expression expression)
		{
			return (S)this.Execute(expression);
		}

		object IQueryProvider.Execute(Expression expression)
		{
			return Execute(expression);
		}

		public virtual Task<object> ExecuteAsync(Expression expression)
		{
			return Task.Run(() => Execute(expression));
		}

		public virtual Task<TS> ExecuteAsync<TS>(Expression expression)
		{
			return Task.Run(() => (TS)Execute(expression));
		}

		private readonly Dictionary<MappingEntity, IEntityTable> tables;

		public EntityProvider(SQLiteConnection connection)
		{
		    this.connection = connection;
		    Mapping = new ImplicitMapping();
			Policy = new EntityPolicy();
			tables = new Dictionary<MappingEntity, IEntityTable>();
			ActionOpenedConnection = false;
		}

		public QueryMapping Mapping { get; private set; }

		public EntityPolicy Policy
		{
			get
			{
				return this.policy;
			}
			set
			{
				this.policy = value ?? EntityPolicy.Default;
			}
		}

		public TextWriter Log { get; set; }

		public IEntityTable GetTable(MappingEntity entity)
		{
			IEntityTable table;
			if (!this.tables.TryGetValue(entity, out table))
			{
				table = this.CreateTable(entity);
				this.tables.Add(entity, table);
			}
			return table;
		}

		private IEntityTable CreateTable(MappingEntity entity)
		{
			return
				(IEntityTable)
				Activator.CreateInstance(typeof(EntityTable<>).MakeGenericType(entity.ElementType), new object[] { this, entity });
		}

		public IEntityTable<T> GetTable<T>()
		{
			return GetTable<T>(null);
		}

		public IEntityTable<T> GetTable<T>(string tableId)
		{
			return (IEntityTable<T>)this.GetTable(typeof(T), tableId);
		}

		public IEntityTable GetTable(Type type)
		{
			return GetTable(type, null);
		}

		public IEntityTable GetTable(Type type, string tableId)
		{
			return this.GetTable(this.Mapping.GetEntity(type, tableId));
		}

		public bool CanBeEvaluatedLocally(Expression expression)
		{
			return this.Mapping.CanBeEvaluatedLocally(expression);
		}

		public bool CanBeParameter(Expression expression)
		{
			Type type = TypeHelper.GetNonNullableType(expression.Type);
			switch (TypeHelper.GetTypeCode(type))
			{
				case TypeCode.Object:
					if (expression.Type == typeof(Byte[]) || expression.Type == typeof(Char[]))
					{
						return true;
					}
					return false;
				default:
					return true;
			}
		}

		public Executor CreateExecutor()
		{
			return new Executor(this);
		}

		public class EntityTable<T> : Query<T>, IEntityTable<T>, IHaveMappingEntity
		{
			private readonly MappingEntity entity;

			private readonly EntityProvider provider;

			public EntityTable(EntityProvider provider, MappingEntity entity)
				: base(provider, typeof(IEntityTable<T>))
			{
				this.provider = provider;
				this.entity = entity;
			}

			public MappingEntity Entity
			{
				get
				{
					return this.entity;
				}
			}

			public new IEntityProvider Provider
			{
				get
				{
					return this.provider;
				}
			}

			public string TableId
			{
				get
				{
					return this.entity.TableId;
				}
			}

			public Type EntityType
			{
				get
				{
					return this.entity.EntityType;
				}
			}

			public T GetById(object id)
			{
				var dbProvider = this.Provider;
				if (dbProvider != null)
				{
					IEnumerable<object> keys = id as IEnumerable<object>;
					if (keys == null)
					{
						keys = new object[] { id };
					}
					Expression query = ((EntityProvider)dbProvider).Mapping.GetPrimaryKeyQuery(
						this.entity, this.Expression, keys.Select(v => Expression.Constant(v)).ToArray());
					return this.Provider.Execute<T>(query);
				}
				return default(T);
			}

			object IEntityTable.GetById(object id)
			{
				return this.GetById(id);
			}
		}

		public virtual string GetQueryText(Expression expression)
		{
			Expression plan = this.GetExecutionPlan(expression);
			var commands = CommandGatherer.Gather(plan).Select(c => c.CommandText).ToArray();
			return string.Join("\n\n", commands);
		}

		private class CommandGatherer : DbExpressionVisitor
		{
			private readonly List<QueryCommand> commands = new List<QueryCommand>();

			public static ReadOnlyCollection<QueryCommand> Gather(Expression expression)
			{
				var gatherer = new CommandGatherer();
				gatherer.Visit(expression);
                return new ReadOnlyCollection<QueryCommand>(gatherer.commands);
			}

			protected override Expression VisitConstant(ConstantExpression c)
			{
				QueryCommand qc = c.Value as QueryCommand;
				if (qc != null)
				{
					this.commands.Add(qc);
				}
				return c;
			}
		}

		public string GetQueryPlan(Expression expression)
		{
			Expression plan = this.GetExecutionPlan(expression);
			return DbExpressionWriter.WriteToString(plan);
		}

		private QueryTranslator CreateTranslator()
		{
			return new QueryTranslator(this.Mapping, this.policy);
		}

		/// <summary>
		/// Execute the query expression (does translation, etc.)
		/// </summary>
		/// <param name="expression"></param>
		/// <returns></returns>
		public virtual object Execute(Expression expression)
		{
			LambdaExpression lambda = expression as LambdaExpression;

			Expression plan = this.GetExecutionPlan(expression);

			if (lambda != null)
			{
				// compile & return the execution plan so it can be used multiple times
				LambdaExpression fn = Expression.Lambda(lambda.Type, plan, lambda.Parameters);
#if NOREFEMIT
                    return ExpressionEvaluator.CreateDelegate(fn);
#else
				return fn.Compile();
#endif
			}
			else
			{
				// compile the execution plan and invoke it
				Expression<Func<object>> efn = Expression.Lambda<Func<object>>(Expression.Convert(plan, typeof(object)));
#if NOREFEMIT
                    return ExpressionEvaluator.Eval(efn, new object[] { });
#else
				Func<object> fn = efn.Compile();
				return fn();
#endif
			}
		}

		/// <summary>
		/// Convert the query expression into an execution plan
		/// </summary>
		/// <param name="expression"></param>
		/// <returns></returns>
		public Expression GetExecutionPlan(Expression expression)
		{
			// strip off lambda for now
			LambdaExpression lambda = expression as LambdaExpression;
			if (lambda != null)
			{
				expression = lambda.Body;
			}

			QueryTranslator translator = this.CreateTranslator();

			// translate query into client & server parts
			Expression translation = translator.Translate(expression);

			var parameters = lambda != null ? lambda.Parameters : null;
			Expression provider = this.Find(expression, parameters, typeof(EntityProvider));
			if (provider == null)
			{
				Expression rootQueryable = this.Find(expression, parameters, typeof(IQueryable));
				provider = Expression.Property(rootQueryable, typeof(IQueryable).GetTypeInfo().GetDeclaredProperty("Provider"));
			}

			return translator.Police.BuildExecutionPlan(translation, provider);
		}

		private Expression Find(Expression expression, IList<ParameterExpression> parameters, Type type)
		{
			if (parameters != null)
			{
				Expression found = parameters.FirstOrDefault(p => type.GetTypeInfo().IsAssignableFrom(p.Type.GetTypeInfo()));
				if (found != null)
				{
					return found;
				}
			}
			return TypedSubtreeFinder.Find(expression, type);
		}

		public static QueryMapping GetMapping(string mappingId)
		{
			return new ImplicitMapping();
		}

		private bool ActionOpenedConnection { get; set; }

	    public SQLiteConnection Connection
	    {
	        get { return connection; }
	    }

	    public sealed class Executor
		{
			public Executor(EntityProvider provider)
			{
				this.Provider = provider;
			}

			public EntityProvider Provider { get; private set; }

			public int RowsAffected { get; private set; }

			private bool ActionOpenedConnection
			{
				get
				{
					return this.Provider.ActionOpenedConnection;
				}
			}

			public object Convert(object value, Type type)
			{
				if (value == null)
				{
					return TypeHelper.GetDefault(type);
				}
				type = TypeHelper.GetNonNullableType(type);
				Type vtype = value.GetType();
				if (type != vtype)
				{
					if (type.GetTypeInfo().IsEnum)
					{
						if (vtype == typeof(string))
						{
							return Enum.Parse(type, (string)value, true);
						}
						else
						{
							Type utype = Enum.GetUnderlyingType(type);
							if (utype != vtype)
							{
								value = System.Convert.ChangeType(value, utype, CultureInfo.InvariantCulture);
							}
							return Enum.ToObject(type, value);
						}
					}
					return System.Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
				}
				return value;
			}

            public IEnumerable<T> Execute<T>(QueryCommand command, Func<FieldReader, T> fnProjector, MappingEntity entity, object[] paramValues)
            {
                return Provider.Connection.LinqQuery(command.CommandText, paramValues, fnProjector);
            }
		}
	}
}