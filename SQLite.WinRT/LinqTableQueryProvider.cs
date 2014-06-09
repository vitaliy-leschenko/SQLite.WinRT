using System;
using System.Linq;
using System.Linq.Expressions;

namespace SQLite.WinRT
{
    public class LinqTableQueryProvider: IQueryProvider
    {
        private readonly SQLiteConnection connection;

        public LinqTableQueryProvider(SQLiteConnection connection)
        {
            this.connection = connection;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            Type elementType = expression.Type.GetElementType();
            try
            {
                return (IQueryable)Activator.CreateInstance(typeof(LinqTableQuery<>).MakeGenericType(elementType), new object[] { this, expression });
            }
            catch (System.Reflection.TargetInvocationException tie)
            {
                throw tie.InnerException;
            }
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new LinqTableQuery<TElement>(this, expression);
        }

        public object Execute(Expression expression)
        {
            return LinqQueryContext.Execute(connection, expression, false);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            var isEnumerable = (typeof(TResult).Name == "IEnumerable`1");
            return (TResult)LinqQueryContext.Execute(connection, expression, isEnumerable);
        }
    }
}
