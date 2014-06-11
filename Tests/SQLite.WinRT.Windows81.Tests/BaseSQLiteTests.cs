using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SQLite.WinRT.Tests.Data;

namespace SQLite.WinRT.Tests
{
    public class BaseSQLiteTests
    {
        private const string DbName = "db.sqlite";

        protected SQLiteAsyncConnection connection;

        [TestInitialize]
        public void TestInitialize()
        {
            var folder = ApplicationData.Current.LocalFolder;
            connection = new SQLiteAsyncConnection(Path.Combine(folder.Path, DbName), true);
            connection.GetConnection().Trace = true;

            Task.WaitAll(DataInitialize());
        }

        [TestCleanup]
        public async Task TestCleanup()
        {
            connection.GetConnection().Close();
            connection = null;
            SQLiteConnectionPool.Shared.Reset();

            var folder = ApplicationData.Current.LocalFolder;
            var file = await folder.GetFileAsync(DbName);
            await file.DeleteAsync();
        }
        private async Task DataInitialize()
        {
            await connection.CreateTableAsync<Category>();
            await connection.CreateTableAsync<Item>();

            for (var i = 0; i < 10; i++)
            {
                var category = new Category();
                category.Name = "category " + (i + 1);
                await connection.InsertAsync(category);

                for (int j = 0; j < 2 + i * 2; j++)
                {
                    var item = new Item();
                    item.CategoryID = category.CategoryID;
                    item.Boolean = (i + j) % 3 == 0;
                    item.Data = (i - j) % 7 == 3 ? (int?)null : j - i;
                    item.Time = DateTime.Today.AddHours(i).AddMinutes(j);
                    item.Title = "item" + j;

                    await connection.InsertAsync(item);
                }
            }
        }
    }
}
