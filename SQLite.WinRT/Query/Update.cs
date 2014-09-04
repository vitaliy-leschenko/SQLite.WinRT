using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SQLite.WinRT.Linq;
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

    public class Update : ISqlQuery
    {
        private readonly SqlQuery query;

        internal List<string> FromTables
        {
            get { return query.FromTables; }
        }

        internal List<Setting> SetStatements
        {
            get { return query.SetStatements; }
        }

        internal List<Constraint> Constraints
        {
            get { return query.Constraints; }
        }

        internal Update(string tableId, IEntityProvider provider)
        {
            query = new SqlQuery(provider);
            query.QueryCommandType = QueryType.Update;
            query.FromTables.Add(tableId);
        }

        string ISqlQuery.BuildSqlStatement()
        {
            return query.BuildSqlStatement();
        }

        int ISqlQuery.Execute()
        {
            return query.Execute();
        }

        Task<int> ISqlQuery.ExecuteAsync()
        {
            return query.ExecuteAsync();
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