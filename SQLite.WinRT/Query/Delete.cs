using System.Threading.Tasks;
using SQLite.WinRT.Linq.Base;

namespace SQLite.WinRT.Query
{
    public class Delete
    {
        private readonly SqlQuery query;

        internal Delete(string tableId, IEntityProvider provider)
        {
            query = new SqlQuery(provider);
            query.QueryCommandType = QueryType.Delete;
            query.FromTables.Add(tableId);
        }

        public Constraint Where(string columnName)
        {
            return query.Where(columnName);
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