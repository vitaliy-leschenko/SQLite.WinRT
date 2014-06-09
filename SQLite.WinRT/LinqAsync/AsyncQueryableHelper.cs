using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using SQLite.WinRT.Linq;

namespace SQLite.WinRT.LinqAsync
{
    public static class AsyncQueryableHelper
    {
        public static Task<List<T>> ToListAsync<T>(this IQueryable<T> query)
        {
            return Task.Factory.StartNew(() =>
            {
                var provider = (EntityProvider)query.Provider;
                using (((SQLiteConnectionWithLock)provider.Connection).Lock())
                {
                    return query.ToList();
                }
            });
        }

        public static Task<int> CountAsync<T>(this IQueryable<T> query)
        {
            return Task.Factory.StartNew(() =>
            {
                var provider = (EntityProvider)query.Provider;
                using (((SQLiteConnectionWithLock)provider.Connection).Lock())
                {
                    return query.Count();
                }
            });
        }

        public static Task<int> CountAsync<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate)
        {
            return Task.Factory.StartNew(() =>
            {
                var provider = (EntityProvider)query.Provider;
                using (((SQLiteConnectionWithLock)provider.Connection).Lock())
                {
                    return query.Count(predicate);
                }
            });
        }

        public static Task<T> MaxAsync<T>(this IQueryable<T> query)
        {
            return Task.Factory.StartNew(() =>
            {
                var provider = (EntityProvider)query.Provider;
                using (((SQLiteConnectionWithLock)provider.Connection).Lock())
                {
                    return query.Max();
                }
            });
        }

        public static Task<TResult> MaxAsync<T, TResult>(this IQueryable<T> query, Expression<Func<T, TResult>> predicate)
        {
            return Task.Factory.StartNew(() =>
            {
                var provider = (EntityProvider)query.Provider;
                using (((SQLiteConnectionWithLock)provider.Connection).Lock())
                {
                    return query.Max(predicate);
                }
            });
        }

        public static Task<T> MinAsync<T>(this IQueryable<T> query)
        {
            return Task.Factory.StartNew(() =>
            {
                var provider = (EntityProvider)query.Provider;
                using (((SQLiteConnectionWithLock)provider.Connection).Lock())
                {
                    return query.Min();
                }
            });
        }

        public static Task<TResult> MinAsync<T, TResult>(this IQueryable<T> query, Expression<Func<T, TResult>> predicate)
        {
            return Task.Factory.StartNew(() =>
            {
                var provider = (EntityProvider)query.Provider;
                using (((SQLiteConnectionWithLock)provider.Connection).Lock())
                {
                    return query.Min(predicate);
                }
            });
        }

        public static Task<T> FirstAsync<T>(this IQueryable<T> query)
        {
            return Task.Factory.StartNew(() =>
            {
                var provider = (EntityProvider) query.Provider;
                using (((SQLiteConnectionWithLock) provider.Connection).Lock())
                {
                    return query.First();
                }
            });
        }

        public static Task<T> FirstAsync<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate)
        {
            return Task.Factory.StartNew(() =>
            {
                var provider = (EntityProvider)query.Provider;
                using (((SQLiteConnectionWithLock)provider.Connection).Lock())
                {
                    return query.First(predicate);
                }
            });
        }

        public static Task<T> FirstOrDefaultAsync<T>(this IQueryable<T> query)
        {
            return Task.Factory.StartNew(() =>
            {
                var provider = (EntityProvider)query.Provider;
                using (((SQLiteConnectionWithLock)provider.Connection).Lock())
                {
                    return query.FirstOrDefault();
                }
            });
        }

        public static Task<T> FirstOrDefaultAsync<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate)
        {
            return Task.Factory.StartNew(() =>
            {
                var provider = (EntityProvider)query.Provider;
                using (((SQLiteConnectionWithLock)provider.Connection).Lock())
                {
                    return query.FirstOrDefault(predicate);
                }
            });
        }
        public static Task<TResult> DoAsync<T, TResult>(this IQueryable<T> query, Func<IQueryable<T>, TResult> action)
        {
            return Task.Factory.StartNew(() =>
            {
                var provider = (EntityProvider)query.Provider;
                using (((SQLiteConnectionWithLock)provider.Connection).Lock())
                {
                    return action(query);
                }
            });
        }
    }
}
