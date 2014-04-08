using System;
using System.Linq;
using System.Linq.Expressions;

namespace SQLite.WinRT
{
    public partial class TableQuery<T>
    {
        public TColumn Min<TColumn>(Expression<Func<T, TColumn>> column)
        {
            var map = Connection.GetMapping(typeof(T));
            var expression = column.Body as MemberExpression;
            if (expression != null)
            {
                var name = expression.Member.Name;
                var c = map.Columns.First(t => t.PropertyName == name);

                return GenerateCommand("min(\"" + c.Name + "\")").ExecuteScalar<TColumn>();
            }
            return default(TColumn);
        }

        public TColumn Max<TColumn>(Expression<Func<T, TColumn>> column)
        {
            var expression = column.Body as MemberExpression;
            if (expression != null)
            {
                var name = expression.Member.Name;
                var map = Connection.GetMapping(typeof(T));
                var c = map.Columns.First(t => t.PropertyName == name);

                return GenerateCommand("max(\"" + c.Name + "\")").ExecuteScalar<TColumn>();
            }
            return default(TColumn);
        }
    }
}
