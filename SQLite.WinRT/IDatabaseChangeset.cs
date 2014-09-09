using SQLite.WinRT.Linq.Base;

namespace SQLite.WinRT
{
    public interface IDatabaseChangeset
    {
        int Version { get; }
        void Update(IEntityProvider provider);
    }
}