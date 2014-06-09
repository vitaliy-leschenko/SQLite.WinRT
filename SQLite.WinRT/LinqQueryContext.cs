using System;
using System.Linq.Expressions;

namespace SQLite.WinRT
{
    class LinqQueryContext
    {
        internal static object Execute(SQLiteConnection connection, Expression expression, bool isEnumerable)
        {
            throw new NotImplementedException();
        }
    }
}
