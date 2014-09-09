// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using SQLite.WinRT.Query;

namespace SQLite.WinRT.Linq
{
    /// <summary>
    ///     A LINQ IQueryable query provider that executes database queries over a SqliteConnection
    /// </summary>
    public class EntityProvider : IAsyncQueryProvider, IEntityProvider, IQueryProvider
    {
        private readonly SQLiteConnection connection;
        private readonly Dictionary<MappingEntity, IEntityTable> tables;
        private EntityPolicy policy;

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
            get { return policy; }
            set { policy = value ?? EntityPolicy.Default; }
        }

        public TextWriter Log { get; set; }
        private bool ActionOpenedConnection { get; set; }

        public SQLiteConnection Connection
        {
            get { return connection; }
        }

        public int CreateTable<T>()
        {
            return connection.CreateTable(typeof(T));
        }

        public void DropTable(string tableName)
        {
            connection.DropTable(tableName);
        }

        public Task<int> CreateTableAsync<T>()
        {
            return Task.Run(() =>
            {
                using (connection.Lock())
                {
                    return connection.CreateTable(typeof(T));
                }
            });
        }

        public Task DropTableAsync(string tableName)
        {
            return Task.Run(() =>
            {
                using (connection.Lock())
                {
                    connection.DropTable(tableName);
                }
            });
        }

        public int CreateTable(Type type)
        {
            return connection.CreateTable(type);
        }

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
                        Activator.CreateInstance(typeof (Query<>).MakeGenericType(elementType),
                            new object[] {this, expression});
            }
            catch (TargetInvocationException tie)
            {
                throw tie.InnerException;
            }
        }

        S IQueryProvider.Execute<S>(Expression expression)
        {
            return (S) Execute(expression);
        }

        object IQueryProvider.Execute(Expression expression)
        {
            return Execute(expression);
        }

        public virtual Task<object> ExecuteAsync(Expression expression)
        {
            return Task.Run(delegate
            {
                var conn = Connection;
                using (conn.Lock())
                {
                    return Execute(expression);
                }
            });
        }

        public virtual Task<TS> ExecuteAsync<TS>(Expression expression)
        {
            return Task.Run(delegate
            {
                var conn = Connection;
                using (conn.Lock())
                {
                    return (TS)Execute(expression);
                }
            });
        }

        public IEntityTable<T> GetTable<T>(string tableId = null)
        {
            return (IEntityTable<T>) GetTable(Mapping.GetEntity(typeof (T), tableId));
        }

        public IEntityTable GetTable(MappingEntity entity)
        {
            IEntityTable table;
            if (!tables.TryGetValue(entity, out table))
            {
                table = CreateTable(entity);
                tables.Add(entity, table);
            }
            return table;
        }

        private IEntityTable CreateTable(MappingEntity entity)
        {
            return (IEntityTable) Activator.CreateInstance(
                typeof (EntityTable<>).MakeGenericType(entity.ElementType),
                new object[] {this, entity});
        }

        public Executor CreateExecutor()
        {
            return new Executor(this);
        }

        public virtual string GetQueryText(Expression expression)
        {
            Expression plan = GetExecutionPlan(expression);
            string[] commands = CommandGatherer.Gather(plan).Select(c => c.CommandText).ToArray();
            return string.Join("\n\n", commands);
        }

        public string GetQueryPlan(Expression expression)
        {
            Expression plan = GetExecutionPlan(expression);
            return DbExpressionWriter.WriteToString(plan);
        }

        private QueryTranslator CreateTranslator()
        {
            return new QueryTranslator(Mapping, policy);
        }

        /// <summary>
        ///     Execute the query expression (does translation, etc.)
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public virtual object Execute(Expression expression)
        {
            var lambda = expression as LambdaExpression;

            Expression plan = GetExecutionPlan(expression);

            if (lambda != null)
            {
                // compile & return the execution plan so it can be used multiple times
                var fn = Expression.Lambda(lambda.Type, plan, lambda.Parameters);
                return fn.Compile();
            }
            else
            {
                // compile the execution plan and invoke it
                var efn = Expression.Lambda<Func<object>>(Expression.Convert(plan, typeof (object)));
                var fn = efn.Compile();
                return fn();
            }
        }

        /// <summary>
        ///     Convert the query expression into an execution plan
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public Expression GetExecutionPlan(Expression expression)
        {
            // strip off lambda for now
            var lambda = expression as LambdaExpression;
            if (lambda != null)
            {
                expression = lambda.Body;
            }

            QueryTranslator translator = CreateTranslator();

            // translate query into client & server parts
            Expression translation = translator.Translate(expression);

            ReadOnlyCollection<ParameterExpression> parameters = lambda != null ? lambda.Parameters : null;
            Expression provider = Find(expression, parameters, typeof (EntityProvider));
            if (provider == null)
            {
                Expression rootQueryable = Find(expression, parameters, typeof (IQueryable));
                provider = Expression.Property(rootQueryable,
                    typeof (IQueryable).GetTypeInfo().GetDeclaredProperty("Provider"));
            }

            return translator.Police.BuildExecutionPlan(translation, provider);
        }

        private Expression Find(Expression expression, IList<ParameterExpression> parameters, Type type)
        {
            if (parameters != null)
            {
                Expression found =
                    parameters.FirstOrDefault(p => type.GetTypeInfo().IsAssignableFrom(p.Type.GetTypeInfo()));
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

        private class CommandGatherer : DbExpressionVisitor
        {
            private readonly List<QueryCommand> commands = new List<QueryCommand>();

            public static IEnumerable<QueryCommand> Gather(Expression expression)
            {
                var gatherer = new CommandGatherer();
                gatherer.Visit(expression);
                return new ReadOnlyCollection<QueryCommand>(gatherer.commands);
            }

            protected override Expression VisitConstant(ConstantExpression c)
            {
                var qc = c.Value as QueryCommand;
                if (qc != null)
                {
                    commands.Add(qc);
                }
                return c;
            }
        }

        public class EntityTable<T> : Query<T>, IEntityTable<T>, IHaveMappingEntity where T: class
        {
            private readonly MappingEntity entity;
            private readonly EntityProvider provider;
            private readonly TableMapping mapping;

            public EntityTable(EntityProvider provider, MappingEntity entity)
                : base(provider, typeof (IEntityTable<T>))
            {
                mapping = provider.Connection.GetMapping<T>();
                this.provider = provider;
                this.entity = entity;
            }

            public Type EntityType
            {
                get { return entity.EntityType; }
            }

            public new IEntityProvider Provider
            {
                get { return provider; }
            }

            public string TableId
            {
                get { return entity.TableId; }
            }

            public Update<T> Update()
            {
                return new Update<T>(TableId, provider);
            }

            public Delete<T> Delete()
            {
                return new Delete<T>(TableId, provider);
            }

            public int Insert(T item)
            {
                if (item == null)
                {
                    return 0;
                }

                var conn = provider.Connection;
                var cmd = mapping.GetInsertCommand();
                var vals = mapping.ColumnValuesFunc(item);
                var count = cmd.ExecuteNonQuery(vals);

                if (mapping.HasAutoIncPK)
                {
                    var id = Platform.Current.SQLiteProvider.LastInsertRowid(conn.Handle);
                    mapping.SetAutoIncPK(item, id);
                }

                return count;
            }

            public int Update(T item)
            {
                if (item == null)
                {
                    return 0;
                }

                var vals = mapping.ColumnValuesFunc(item);
                var cmd = mapping.GetUpdateCommand(vals, mapping.PrimaryKeyFunc(item));
                return cmd.ExecuteNonQuery();
            }

            public int UpdateAll(IEnumerable<T> items)
            {
                var conn = provider.Connection;
                var count = 0;
                conn.RunInTransaction(
                    delegate
                    {
                        count = items.Aggregate(0, (s, i) => s + Update(i));
                    });
                return count;
            }

            public int InsertAll(IEnumerable<T> items)
            {
                var conn = provider.Connection;
                var cmd = mapping.GetInsertCommand();

                var count = 0;
                conn.RunInTransaction(
                    delegate
                    {
                        foreach (var item in items)
                        {
                            var vals = mapping.ColumnValuesFunc(item);
                            count += cmd.ExecuteNonQuery(vals);

                            if (mapping.HasAutoIncPK)
                            {
                                var id = Platform.Current.SQLiteProvider.LastInsertRowid(conn.Handle);
                                mapping.SetAutoIncPK(item, id);
                            }
                        }
                    });
                return count;
            }

            public Task<int> InsertAsync(T item)
            {
                return Task.Run(() =>
                {
                    var conn = provider.Connection;
                    using (conn.Lock())
                    {
                        return Insert(item);
                    }
                });
            }

            public Task<int> InsertAllAsync(IEnumerable<T> items)
            {
                return Task.Run(() =>
                {
                    var conn = provider.Connection;
                    using (conn.Lock())
                    {
                        return InsertAll(items);
                    }
                });
            }

            public Task<int> UpdateAsync(T item)
            {
                return Task.Run(() =>
                {
                    var conn = provider.Connection;
                    using (conn.Lock())
                    {
                        return Update(item);
                    }
                });
            }

            public Task<int> UpdateAllAsync(IEnumerable<T> items)
            {
                return Task.Run(() =>
                {
                    var conn = provider.Connection;
                    using (conn.Lock())
                    {
                        return UpdateAll(items);
                    }
                });
            }

            public int Delete(T item)
            {
                return provider.Connection.Delete(item);
            }

            public Task<int> DeleteAsync(T item)
            {
                return Task.Run(() =>
                {
                    var conn = provider.Connection;
                    using (conn.Lock())
                    {
                        return Delete(item);
                    }
                });
            }

            public MappingEntity Entity
            {
                get { return entity; }
            }
        }

        public sealed class Executor
        {
            public Executor(EntityProvider provider)
            {
                Provider = provider;
            }

            public EntityProvider Provider { get; private set; }

            public IEnumerable<T> Execute<T>(QueryCommand command, Func<FieldReader, T> fnProjector,
                MappingEntity entity, object[] paramValues)
            {
                return Provider.LinqQuery(command.CommandText, paramValues, fnProjector);
            }
        }

        private IEnumerable<T> LinqQuery<T>(string commandText, object[] paramValues, Func<FieldReader, T> projector)
        {
            return Connection.LinqQuery(commandText, paramValues, projector);
        }
    }
}