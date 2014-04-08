using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace SQLite.WinRT
{
    public partial class AsyncTableQuery<T> where T : new()
    {
        public async Task Delete(Expression<Func<T, bool>> predExpr)
        {
            var list = await Where(predExpr).ToListAsync();

            using (((SQLiteConnectionWithLock)innerQuery.Connection).Lock())
            {
                var conn = innerQuery.Connection;
                foreach (var item in list)
                {
                    conn.Delete(item);
                }
            }
        }

        public Task<TColumn> MaxAsync<TColumn>(Expression<Func<T, TColumn>> column)
        {
            return Task<TColumn>.Factory.StartNew(() =>
            {
                using (((SQLiteConnectionWithLock)innerQuery.Connection).Lock())
                {
                    return innerQuery.Max(column);
                }
            });
        }

        public Task<TColumn> MinAsync<TColumn>(Expression<Func<T, TColumn>> column)
        {
            return Task<TColumn>.Factory.StartNew(() =>
            {
                using (((SQLiteConnectionWithLock)innerQuery.Connection).Lock())
                {
                    return innerQuery.Min(column);
                }
            });
        }
    }
}
