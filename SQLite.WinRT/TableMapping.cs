using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SQLite.WinRT
{
    public class TableMapping
    {
        private readonly SQLiteConnection connection;

        public Type MappedType { get; private set; }

        public string TableName { get; private set; }

        public Column[] Columns { get; private set; }

        public Column PK { get; private set; }

        public string GetByPrimaryKeySql { get; private set; }

        private readonly Column autoPk = null;
        private List<string> InsertColumns { get; set; }

        public Func<object, object> PrimaryKeyFunc { get; private set; }
        public Func<object, object[]> ColumnValuesFunc { get; private set; }

        public TableMapping(Type type, SQLiteConnection connection)
        {
            this.connection = connection;
            MappedType = type;

            var tableAttr = (TableAttribute) type.GetTypeInfo().GetCustomAttribute(typeof (TableAttribute), true);

            TableName = tableAttr != null ? tableAttr.Name : MappedType.Name;

            var props = MappedType.GetRuntimeProperties()
                .Where(p => p.GetMethod != null && p.GetMethod.IsPublic &&
                            p.SetMethod != null && p.SetMethod.IsPublic);

            PropertyInfo pkColumn = null;
            var dataColumns = new List<PropertyInfo>();
            var cols = new List<Column>();
            foreach (var p in props)
            {
                var ignore = p.GetCustomAttributes(typeof (IgnoreAttribute), true).Any();
                if (!ignore)
                {
                    var column = new Column(p);
                    if (!column.IsPK)
                    {
                        dataColumns.Add(p);
                    }
                    else
                    {
                        pkColumn = p;
                    }
                    cols.Add(column);
                }
            }
            InsertColumns = dataColumns.Select(t => t.Name).ToList();
            Columns = cols.ToArray();
            foreach (var c in Columns)
            {
                if (c.IsAutoInc && c.IsPK)
                {
                    autoPk = c;
                }
                if (c.IsPK)
                {
                    PK = c;
                }
            }

            HasAutoIncPK = autoPk != null;

            if (PK != null)
            {
                GetByPrimaryKeySql = string.Format("select * from \"{0}\" where \"{1}\" = ?", TableName, PK.Name);
                BuildPrimaryKeyFunc(pkColumn);
                BuildColumnsFunc(dataColumns);
            }
        }

        private void BuildColumnsFunc(IEnumerable<PropertyInfo> cols)
        {
            var t = Expression.Parameter(typeof(object), "t");
            var s = Expression.Convert(t, MappedType);

            var initializers = new List<Expression>();
            var names = new List<string>();
            foreach (var property in cols)
            {
                var item = Expression.Convert(Expression.Property(s, property), typeof(object));
                names.Add(property.Name);
                initializers.Add(item);
            }
            var lambda = Expression.Lambda<Func<object, object[]>>(
                Expression.NewArrayInit(typeof(object), initializers),
                t);
            ColumnValuesFunc = lambda.Compile();
        }

        private void BuildPrimaryKeyFunc(PropertyInfo pk)
        {
            var t = Expression.Parameter(typeof(object), "t");
            var prop = Expression.Property(Expression.Convert(t, MappedType), pk);

            var lambda = Expression.Lambda<Func<object, object>>(Expression.Convert(prop, typeof(object)), t);
            PrimaryKeyFunc = lambda.Compile();
        }

        public bool HasAutoIncPK { get; private set; }

        public void SetAutoIncPK(object obj, long id)
        {
            if (autoPk != null)
            {
                autoPk.SetValue(obj, Convert.ChangeType(id, autoPk.ColumnType, null));
            }
        }

        public Column FindColumnWithPropertyName(string propertyName)
        {
            return Columns.FirstOrDefault(c => c.PropertyName == propertyName);
        }

        public Column FindColumn(string columnName)
        {
            return Columns.FirstOrDefault(c => c.Name == columnName);
        }

        private PreparedSqlLiteInsertCommand insertCommand;

        public SQLiteCommand GetUpdateCommand(object[] vals, object pk)
        {
            var cols = InsertColumns;

            var sql = string.Format ("update \"{0}\" set {1} where {2} = ? ", 
                TableName,
                string.Join(", ", cols.Select(c => "\"" + c + "\" = ?")), PK.Name);

            var cmd = new SQLiteCommand(connection);
            cmd.CommandText = sql;
            foreach (var val in vals)
            {
                cmd.Bind(val);
            }
            cmd.Bind(pk);

            return cmd;
        }

        public PreparedSqlLiteInsertCommand GetInsertCommand()
        {
            return insertCommand ?? (insertCommand = CreateInsertCommand());
        }

        private PreparedSqlLiteInsertCommand CreateInsertCommand()
        {
            var cols = InsertColumns;

            var sql = string.Format("insert into \"{0}\"({1}) values ({2})",
                TableName,
                string.Join(", ", cols.Select(c => "\"" + c + "\"")),
                string.Join(", ", cols.Select(c => "?")));

            var cmd = new PreparedSqlLiteInsertCommand(connection);
            cmd.CommandText = sql;
            return cmd;
        }

        protected internal void Dispose()
        {
            if (insertCommand != null)
            {
                insertCommand.Dispose();
                insertCommand = null;
            }
        }

        public class Column
        {
            private readonly PropertyInfo prop;

            public string Name { get; private set; }

            public string PropertyName { get { return Property.Name; } }

            public Type ColumnType { get; private set; }

            public string Collation { get; private set; }

            public bool IsAutoInc { get; private set; }

            public bool IsPK { get; private set; }

            public IEnumerable<IndexedAttribute> Indices { get; set; }

            public bool IsNullable { get; private set; }

            public int MaxStringLength { get; private set; }

            public PropertyInfo Property
            {
                get { return prop; }
            }

            public Column(PropertyInfo prop)
            {
                var colAttr = (ColumnAttribute)prop.GetCustomAttributes(typeof(ColumnAttribute), true).FirstOrDefault();

                this.prop = prop;
                Name = colAttr == null ? prop.Name : colAttr.Name;
                ColumnType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                Collation = Orm.Collation(prop);
                IsAutoInc = Orm.IsAutoInc(prop);
                IsPK = Orm.IsPK(prop);
                Indices = Orm.GetIndices(prop);
                IsNullable = !IsPK;
                MaxStringLength = Orm.MaxStringLength(prop);
            }

            public void SetValue(object obj, object val)
            {
                Property.SetValue(obj, val, null);
            }

            public object GetValue(object obj)
            {
                return Property.GetValue(obj, null);
            }
        }
    }
}