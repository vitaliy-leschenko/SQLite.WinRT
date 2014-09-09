using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using SQLite.WinRT.Linq.Base;

namespace SQLite.WinRT.Query
{
    public class SqlQuery<T> : ISqlQuery
    {
        private readonly IEntityProvider provider;
        private readonly List<string> fromTables = new List<string>();
        private readonly List<ISetting<T>> setStatements = new List<ISetting<T>>();
        private readonly List<IConstraint> constraints = new List<IConstraint>();
        private QueryType queryCommandType = QueryType.Unknown;

        public SqlQuery(IEntityProvider provider)
        {
            this.provider = provider;
        }

        internal List<string> FromTables
        {
            get { return fromTables; }
        }

        internal List<ISetting<T>> SetStatements
        {
            get { return setStatements; }
        }

        public List<IConstraint> Constraints
        {
            get { return constraints; }
        }

        internal QueryType QueryCommandType
        {
            get { return queryCommandType; }
            set { queryCommandType = value; }
        }

        public virtual string BuildSqlStatement()
        {
            var generator = GetGenerator();

            switch (QueryCommandType)
            {
                case QueryType.Update:
                    return generator.BuildUpdateStatement();
                case QueryType.Delete:
                    return generator.BuildDeleteStatement();
                default:
                    throw new NotSupportedException();
            }
        }

        public StringConstraint<T> Where(Expression<Func<T, string>> propertySelector)
        {
            var memberExpression = propertySelector.Body as MemberExpression;
            if (memberExpression == null) throw new InvalidOperationException();

            return new StringConstraint<T>(ConstraintType.Where, memberExpression.Member.Name, this);
        }
        public StringConstraint<T> And(Expression<Func<T, string>> propertySelector)
        {
            var memberExpression = propertySelector.Body as MemberExpression;
            if (memberExpression == null) throw new InvalidOperationException();

            return new StringConstraint<T>(ConstraintType.And, memberExpression.Member.Name, this);
        }
        public StringConstraint<T> Or(Expression<Func<T, string>> propertySelector)
        {
            var memberExpression = propertySelector.Body as MemberExpression;
            if (memberExpression == null) throw new InvalidOperationException();

            return new StringConstraint<T>(ConstraintType.Or, memberExpression.Member.Name, this);
        }

        public Constraint<T, TValue> Where<TValue>(Expression<Func<T, TValue>> propertySelector)
        {
            var memberExpression = propertySelector.Body as MemberExpression;
            if (memberExpression == null) throw new InvalidOperationException();

            return new Constraint<T, TValue>(ConstraintType.Where, memberExpression.Member.Name, this);
        }
        public Constraint<T, TValue> And<TValue>(Expression<Func<T, TValue>> propertySelector)
        {
            var memberExpression = propertySelector.Body as MemberExpression;
            if (memberExpression == null) throw new InvalidOperationException();

            return new Constraint<T, TValue>(ConstraintType.And, memberExpression.Member.Name, this);
        }
        public Constraint<T, TValue> Or<TValue>(Expression<Func<T, TValue>> propertySelector)
        {
            var memberExpression = propertySelector.Body as MemberExpression;
            if (memberExpression == null) throw new InvalidOperationException();

            return new Constraint<T, TValue>(ConstraintType.Or, memberExpression.Member.Name, this);
        }

        public virtual int Execute()
        {
            var generator = GetGenerator();

            string sql;
            switch (QueryCommandType)
            {
                case QueryType.Update:
                    sql = generator.BuildUpdateStatement();
                    break;
                case QueryType.Delete:
                    sql = generator.BuildDeleteStatement();
                    break;
                default:
                    throw new NotSupportedException();
            }

            return provider.Connection.Execute(sql, generator.Parameters.ToArray());
        }

        public Task<int> ExecuteAsync()
        {
            return Task.Run(() =>
            {
                using (provider.Connection.Lock())
                {
                    return Execute();
                }
            });
        }

        internal ISqlGenerator GetGenerator()
        {
            return new SqlGenerator<T>(this);
        }
    }
}