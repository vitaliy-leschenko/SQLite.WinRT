using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using SQLite.WinRT.Linq.Base;

namespace SQLite.WinRT.Query
{
    public class Update<T>
    {
        private readonly SqlQuery<T> query;

        internal List<ISetting<T>> SetStatements
        {
            get { return query.SetStatements; }
        }

        internal Update(string tableId, IEntityProvider provider)
        {
            query = new SqlQuery<T>(provider);
            query.QueryCommandType = QueryType.Update;
            query.FromTables.Add(tableId);
        }

        public Setting<T, TValue> Set<TValue>(Expression<Func<T, TValue>> columnName)
        {
            var memberExpression = columnName.Body as MemberExpression;
            if (memberExpression == null) throw new InvalidOperationException();

            return new Setting<T, TValue>
            {
                Query = this,
                ColumnName = memberExpression.Member.Name,
                IsExpression = false
            };
        }

        public StringConstraint<T> Where(Expression<Func<T, string>> propertySelector)
        {
            return query.Where(propertySelector);
        }

        public Constraint<T, TValue> Where<TValue>(Expression<Func<T, TValue>> propertySelector)
        {
            return query.Where(propertySelector);
        }
    }
}