using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SQLite.WinRT.Tests.Data;

// ReSharper disable CheckNamespace
#if WINDOWS_PHONE_APP
namespace SQLite.WinRT.Tests.WinPhone81
#elif NETFX_CORE
namespace SQLite.WinRT.Tests.Win8
#else
namespace SQLite.WinRT.Tests.WinPhone8
#endif
// ReSharper restore CheckNamespace
{
    [TestClass]
    public class SqlQueryTests
    {
        private const string DbName = "db.sqlite";

        protected SQLiteAsyncConnection connection;

        [TestInitialize]
        public void TestInitialize()
        {
            var folder = ApplicationData.Current.LocalFolder;
            connection = new SQLiteAsyncConnection(Path.Combine(folder.Path, DbName), true);

            Task.WaitAll(DataInitialize());

            connection.GetConnection().Trace = true;
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

                    item.Time = DateTime.Today.AddHours(i).AddMinutes(j).AddSeconds(5);

                    await connection.UpdateAsync(item);
                }
            }
        }

        [TestMethod]
        public void UpdateTest()
        {
            var db = new DbContext(connection);
            var count = db.Categories
                .Update()
                .Set("Name").EqualTo("test name")
                .Where("CategoryID").IsBetweenAnd(3, 4)
                .Execute();

            var cats = db.Categories.Where(t => t.CategoryID >= 3 && t.CategoryID <= 4).ToList();
            Assert.IsTrue(count == cats.Count);

            foreach (var cat in cats)
            {
                Assert.IsTrue(cat.Name == "test name");
            }
        }

        [TestMethod]
        public void DeleteTest()
        {
            var db = new DbContext(connection);
            var count = db.Categories.Delete().Where("CategoryID").IsLessThanOrEqualTo(3).Execute();
            Assert.IsTrue(count == 3);
            Assert.IsFalse(db.Categories.Any(t => t.CategoryID <= 3));
        }
    }
}
