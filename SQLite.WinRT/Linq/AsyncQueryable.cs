using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using SQLite.WinRT.Linq.Base;

namespace SQLite.WinRT.Linq
{
    public static class AsyncQueryable
    {
        public static Task<IEnumerable<TElement>> ExecuteAsync<TElement>(this IQueryable<TElement> query)
        {
            var queryExpression = query.Expression;
            var asyncProvider = query.Provider as IAsyncQueryProvider;
            if (asyncProvider != null)
            {
                return asyncProvider.ExecuteAsync<IEnumerable<TElement>>(queryExpression);
            }
            return Task.Run(() => query.Provider.Execute<IEnumerable<TElement>>(queryExpression));
        }

        public static Task<TResult> ExecuteAsync<TElement, TResult>(
            this IQueryable<TElement> query, Expression<Func<IQueryable<TElement>, TResult>> selector)
        {
            var queryExpression = ExpressionReplacer.Replace(selector.Body, selector.Parameters[0], query.Expression);

            var asyncProvider = query.Provider as IAsyncQueryProvider;
            if (asyncProvider != null)
            {
                return asyncProvider.ExecuteAsync<TResult>(queryExpression);
            }
            return Task.Run(() => query.Provider.Execute<TResult>(queryExpression));
        }

        public static async Task<List<T>> ToListAsync<T>(this IQueryable<T> query)
        {
            var result = await query.ExecuteAsync();
            return new List<T>(result);
        }

        public static async Task<int> CountAsync<T>(this IQueryable<T> query)
        {
            return await query.ExecuteAsync(t => t.Count());
        }

        public static async Task<int> CountAsync<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate)
        {
            return await query.ExecuteAsync(t => t.Count(predicate));
        }

        public static async Task<T> MaxAsync<T>(this IQueryable<T> query)
        {
            return await query.ExecuteAsync(t => t.Max());
        }

        public static async Task<TResult> MaxAsync<T, TResult>(this IQueryable<T> query, Expression<Func<T, TResult>> predicate)
        {
            return await query.ExecuteAsync(t => t.Max(predicate));
        }

        public static async Task<T> MinAsync<T>(this IQueryable<T> query)
        {
            return await query.ExecuteAsync(t => t.Min());
        }

        public static async Task<TResult> MinAsync<T, TResult>(this IQueryable<T> query, Expression<Func<T, TResult>> predicate)
        {
            return await query.ExecuteAsync(t => t.Min(predicate));
        }

        public static async Task<T> FirstAsync<T>(this IQueryable<T> query)
        {
            return await query.ExecuteAsync(t => t.First());
        }

        public static async Task<T> FirstAsync<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate)
        {
            return await query.ExecuteAsync(t => t.First(predicate));
        }

        public static async Task<T> FirstOrDefaultAsync<T>(this IQueryable<T> query)
        {
            return await query.ExecuteAsync(t => t.FirstOrDefault());
        }

        public static async Task<T> FirstOrDefaultAsync<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate)
        {
            return await query.ExecuteAsync(t => t.FirstOrDefault(predicate));
        }
    }
}
