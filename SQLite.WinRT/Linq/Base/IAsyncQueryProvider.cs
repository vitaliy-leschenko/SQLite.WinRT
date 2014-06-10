using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace SQLite.WinRT.Linq.Base
{
    public interface IAsyncQueryProvider : IQueryProvider
    {
        Task<object> ExecuteAsync(Expression query);

        Task<TResult> ExecuteAsync<TResult>(Expression query);
    }
}