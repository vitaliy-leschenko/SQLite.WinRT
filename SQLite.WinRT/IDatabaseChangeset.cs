using SQLite.WinRT.Linq.Base;

namespace SQLite.WinRT
{
    public interface IDatabaseChangeset : IBaseDatabaseChangeset
    {
        void Update(IEntityProvider provider);
    }
}