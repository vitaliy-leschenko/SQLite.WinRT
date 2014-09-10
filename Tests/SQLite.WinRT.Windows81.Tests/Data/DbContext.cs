using SQLite.WinRT.Linq.Base;

namespace SQLite.WinRT.Tests.Data
{
    public class DbContext: BaseDatabaseContext
    {
        public DbContext(SQLiteConnection connection) : base(connection)
        {
        }

        public IEntityTable<Category> Categories
        {
            get { return provider.GetTable<Category>(); }
        }

        public IEntityTable<Item> Items
        {
            get { return provider.GetTable<Item>(); }
        }

        public IEntityTable<Country> Countries
        {
            get { return provider.GetTable<Country>(); }
        }
    }
}