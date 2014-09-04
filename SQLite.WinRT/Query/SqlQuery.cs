using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SQLite.WinRT.Linq.Base;

namespace SQLite.WinRT.Query
{
    public class SqlQuery : ISqlQuery
    {
        private readonly IEntityProvider provider;
        private readonly List<string> fromTables = new List<string>();
        private readonly List<Setting> setStatements = new List<Setting>();
        private readonly List<Constraint> constraints = new List<Constraint>();
        private QueryType queryCommandType = QueryType.Unknown;

        public SqlQuery(IEntityProvider provider)
        {
            this.provider = provider;
        }

        internal List<string> FromTables
        {
            get { return fromTables; }
        }

        internal List<Setting> SetStatements
        {
            get { return setStatements; }
        }

        public List<Constraint> Constraints
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

        public Constraint Where(string columnName)
        {
            return new Constraint(ConstraintType.Where, columnName, this);
        }

        public Constraint And(string columnName)
        {
            return new Constraint(ConstraintType.And, columnName, this);
        }

        public Constraint Or(string columnName)
        {
            return new Constraint(ConstraintType.Or, columnName, this);
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
            return Task.Run(() => Execute());
        }

        internal ISqlGenerator GetGenerator()
        {
            return new SqlGenerator(this);
        }
    }
}