using System.Collections.Generic;
using SQLite.WinRT.Linq.Base;

namespace SQLite.WinRT.Query
{
    public class Setting
    {
        public string ColumnName { get; internal set; }
        public object Value { get; internal set; }
        public bool IsExpression { get; internal set; }
        public Update Query { get; internal set; }

        public Update EqualTo(object value)
        {
            Value = value;
            Query.SetStatements.Add(this);
            return Query;
        }

        public Update EqualToExpression(string value)
        {
            Value = value;
            IsExpression = true;
            Query.SetStatements.Add(this);
            return Query;
        }
    }

    public class Update
    {
        private readonly SqlQuery query;

        internal List<Setting> SetStatements
        {
            get { return query.SetStatements; }
        }

        internal Update(string tableId, IEntityProvider provider)
        {
            query = new SqlQuery(provider);
            query.QueryCommandType = QueryType.Update;
            query.FromTables.Add(tableId);
        }

        public Setting Set(string columnName)
        {
            return new Setting
            {
                Query = this,
                ColumnName = columnName,
                IsExpression = false
            };
        }

        public Constraint Where(string columnName)
        {
            return query.Where(columnName);
        }
    }
}