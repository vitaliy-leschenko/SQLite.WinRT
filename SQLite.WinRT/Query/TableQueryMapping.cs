using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SQLite.WinRT.Query
{
    public class TableQueryMapping<T>
    {
        public Type MappedType { get; private set; }
        public string TableName { get; private set; }
        public bool HasAutoIncPK { get; set; }
        public string PrimaryKeyColumnName { get; private set; }
        public string[] ColumnNames { get; private set; }
        public Func<T, object> PrimaryKeyFunc { get; private set; }
        public Type PrimaryKeyType { get; private set; }
        public MethodInfo PrimaryKeySetter { get; private set; }
        public Func<T, object[]> ColumnValuesFunc { get; private set; }

        public TableQueryMapping()
        {
            MappedType = typeof(T);

            var tableAttr = (TableAttribute)MappedType.GetTypeInfo().GetCustomAttribute(typeof(TableAttribute), true);
            TableName = tableAttr != null ? tableAttr.Name : MappedType.Name;

            var props = from p in MappedType.GetRuntimeProperties()
                        where p.GetMethod != null && p.GetMethod.IsPublic && p.SetMethod != null && p.SetMethod.IsPublic
                        select p;
            PropertyInfo pk = null;
            HasAutoIncPK = false;
            var cols = new List<PropertyInfo>();
            foreach (var p in props)
            {
                if (Orm.IsPK(p))
                {
                    pk = p;
                    if (Orm.IsAutoInc(p))
                    {
                        HasAutoIncPK = true;
                    }
                }
                else
                {
                    cols.Add(p);
                }
            }
            if (pk == null) throw new InvalidOperationException("PK not found.");

            BuildPrimaryKeyFunc(pk);
            BuildColumnsFunc(cols);
        }

        private void BuildColumnsFunc(IEnumerable<PropertyInfo> cols)
        {
            var t = Expression.Parameter(MappedType, "t");
            var initializers = new List<Expression>();
            var names = new List<string>();
            foreach (var property in cols)
            {
                var item = Expression.Convert(Expression.Property(t, property), typeof (object));
                names.Add(property.Name);
                initializers.Add(item);
            }
            var lambda = Expression.Lambda<Func<T, object[]>>(
                Expression.NewArrayInit(typeof (object), initializers),
                t);
            ColumnNames = names.ToArray();
            ColumnValuesFunc = lambda.Compile();
        }

        private void BuildPrimaryKeyFunc(PropertyInfo pk)
        {
            PrimaryKeyColumnName = pk.Name;

            var t = Expression.Parameter(MappedType, "t");
            var lambda = Expression.Lambda<Func<T, object>>(Expression.Convert(Expression.Property(t, pk), typeof (object)), t);
            PrimaryKeyFunc = lambda.Compile();

            PrimaryKeyType = pk.PropertyType;
            PrimaryKeySetter = pk.SetMethod;
        }
    }
}
