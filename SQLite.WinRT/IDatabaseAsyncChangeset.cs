using System.Threading.Tasks;
using SQLite.WinRT.Linq.Base;

namespace SQLite.WinRT
{
    public interface IDatabaseAsyncChangeset : IBaseDatabaseChangeset
    {
        Task UpdateAsync(IEntityProvider provider);
    }
}