using SQLite.WinRT.Linq.Base;

namespace SQLite.WinRT.Tests.Data
{
    public class DbContext
    {
        private readonly SQLiteAsyncConnection connection;

        public DbContext(SQLiteAsyncConnection connection)
        {
            this.connection = connection;
        }

        public IEntityTable<Category> Categories
        {
            get { return connection.Table<Category>(); }
        }

        public IEntityTable<Item> Items
        {
            get { return connection.Table<Item>(); }
        }
    }
}