using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SQLite.WinRT.Tests.Data;
#if WINDOWS_PHONE_APP || NETFX_CORE || WINDOWS_PHONE
using Windows.Storage;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

// ReSharper disable CheckNamespace
#if WINDOWS_PHONE_APP
namespace SQLite.WinRT.Tests.WinPhone81
#elif NETFX_CORE
namespace SQLite.WinRT.Tests.Win8
#elif WINDOWS_PHONE
namespace SQLite.WinRT.Tests.WinPhone8
#else
namespace SQLite.WinRT.Tests.net45
#endif
// ReSharper restore CheckNamespace
{
    [TestClass]
    public class SqlQueryTests
    {
        private const string DbName = "db.sqlite";

        protected SQLiteAsyncConnection connection;

#if WINDOWS_PHONE_APP || NETFX_CORE || WINDOWS_PHONE
        [TestInitialize]
        public void TestInitialize()
        {
            var folder = ApplicationData.Current.LocalFolder;
            connection = new SQLiteAsyncConnection(Path.Combine(folder.Path, DbName), true);

            DataInitialize();

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
#else
        [TestInitialize]
        public void TestInitialize()
        {
            var folder = Path.GetTempPath();
            connection = new SQLiteAsyncConnection(Path.Combine(folder, DbName), true);

            DataInitialize();

            connection.GetConnection().Trace = true;
        }

        [TestCleanup]
        public void TestCleanup()
        {
            connection.GetConnection().Close();
            connection = null;
            SQLiteConnectionPool.Shared.Reset();

            var folder = Path.GetTempPath();
            File.Delete(Path.Combine(folder, DbName));
        }
#endif

        private void DataInitialize()
        {
            var conn = connection.GetConnection();
            conn.CreateTable<Category>();
            conn.CreateTable<Item>();

            for (var i = 0; i < 10; i++)
            {
                var category = new Category();
                category.Name = "category " + (i + 1);
                conn.Insert(category);

                for (int j = 0; j < 2 + i * 2; j++)
                {
                    var item = new Item();
                    item.CategoryID = category.CategoryID;
                    item.Boolean = (i + j) % 3 == 0;
                    item.Data = (i - j) % 7 == 3 ? (int?)null : j - i;
                    item.Time = DateTime.Today.AddHours(i).AddMinutes(j).AddSeconds(5);
                    item.Title = "item" + j;

                    conn.Insert(item);
                }
            }
        }

        [TestMethod]
        public void UpdateTest()
        {
            var db = new DbContext(connection);
            var count = db.Categories
                .Update()
                .Set(t => t.Name).EqualTo("test name")
                .Where(t => t.CategoryID).IsBetweenAnd(3, 4)
                .Execute();

            var cats = db.Categories.Where(t => t.CategoryID >= 3 && t.CategoryID <= 4).ToList();
            Assert.IsTrue(count == cats.Count);

            foreach (var cat in cats)
            {
                Assert.IsTrue(cat.Name == "test name");
            }

            count = db.Categories.Delete().Where(t => t.Name).Like("%test%").Execute();
            Assert.IsTrue(count == cats.Count);
        }

        [TestMethod]
        public void DeleteTest()
        {
            var db = new DbContext(connection);
            var count = db.Categories.Delete().Where(t => t.CategoryID).IsLessThanOrEqualTo(3).Execute();
            Assert.IsTrue(count == 3);
            Assert.IsFalse(db.Categories.Any(t => t.CategoryID <= 3));
        }

        [TestMethod]
        public void DeleteAllTest()
        {
            var db = new DbContext(connection);
            db.Items.Delete().Execute();

            Assert.IsFalse(db.Items.Any());
        }
    }
}
