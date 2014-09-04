using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using SQLite.WinRT.Linq.Base;

namespace SQLite.WinRT.Query
{
    public class Delete<T>
    {
        private readonly SqlQuery<T> query;

        internal Delete(string tableId, IEntityProvider provider)
        {
            query = new SqlQuery<T>(provider);
            query.QueryCommandType = QueryType.Delete;
            query.FromTables.Add(tableId);
        }

        public StringConstraint<T> Where(Expression<Func<T, string>> propertySelector)
        {
            return query.Where(propertySelector);
        }

        public Constraint<T, TValue> Where<TValue>(Expression<Func<T, TValue>> propertySelector)
        {
            return query.Where(propertySelector);
        }

        public virtual int Execute()
        {
            return query.Execute();
        }

        public Task<int> ExecuteAsync()
        {
            return query.ExecuteAsync();
        }
    }
}