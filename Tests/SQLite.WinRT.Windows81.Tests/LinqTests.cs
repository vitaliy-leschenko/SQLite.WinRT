using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SQLite.WinRT.Linq;
using SQLite.WinRT.Linq.Base;
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
    public class LinqTests
    {
        private const string DbName = "db.sqlite";

        private IEntityProvider provider;
        private DbContext db;

#if WINDOWS_PHONE_APP || NETFX_CORE || WINDOWS_PHONE
        [TestInitialize]
        public void TestInitialize()
        {
            var folder = ApplicationData.Current.LocalFolder;
            var connectionString = new SQLiteConnectionString(Path.Combine(folder.Path, DbName), true);
            var connection = SQLiteConnectionPool.Shared.GetConnection(connectionString);
            provider = connection.GetEntityProvider();

            DataInitialize();

            connection.Trace = true;
            connection.TimeExecution = true;
            db = new DbContext(connection);
        }

        [TestCleanup]
        public async Task TestCleanup()
        {
            provider.Connection.Close();
            provider = null;
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
            var connectionString = new SQLiteConnectionString(Path.Combine(folder, DbName), true);
            var connection = SQLiteConnectionPool.Shared.GetConnection(connectionString);
            provider = connection.GetEntityProvider();

            DataInitialize();

            connection.Trace = true;
            connection.TimeExecution = true;
            db = new DbContext(connection);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            provider.Connection.Close();
            provider = null;
            SQLiteConnectionPool.Shared.Reset();

            var folder = Path.GetTempPath();
            File.Delete(Path.Combine(folder, DbName));
        }
#endif


        private void DataInitialize()
        {
            provider.CreateTable<Category>();
            provider.CreateTable<Item>();

            var categories = provider.GetTable<Category>();
            var items = new List<Item>();

            for (var i = 0; i < 10; i++)
            {
                var category = new Category();
                category.Name = "category " + (i + 1);
                categories.Insert(category);

                for (int j = 0; j < 2 + i * 2; j++)
                {
                    var item = new Item();
                    item.CategoryID = category.CategoryID;
                    item.Boolean = (i + j) % 3 == 0;
                    item.Data = (i - j) % 7 == 3 ? (int?)null : j - i;
                    item.Time = DateTime.Today.AddHours(i).AddMinutes(j).AddSeconds(5);
                    item.Title = "item" + j;

                    items.Add(item);
                }
            }
            provider.GetTable<Item>().InsertAll(items);
        }

        [TestMethod]
        public async Task TestSelect()
        {
            var categories = await db.Categories.ToListAsync();
            Assert.IsNotNull(categories);
            Assert.IsTrue(categories.Count == 10);
        }

        [TestMethod]
        public async Task TestSelectWithWhere()
        {
            var items = await db.Items.Where(t => t.Boolean).ToListAsync();
            Assert.IsNotNull(items);
        }

        [TestMethod]
        public async Task TestJoin()
        {
            var query =
                from c in db.Categories
                join i in db.Items on c.CategoryID equals i.CategoryID
                select i;

            var items = await query.ToListAsync();
            Assert.IsNotNull(items);
        }

        [TestMethod]
        public async Task TestJoinMultiKey()
        {
            var query =
                from c in db.Categories
                join i in db.Items on new { a = c.CategoryID, b = c.CategoryID } equals new { a = i.CategoryID, b = i.CategoryID }
                select i;

            var items = await query.ToListAsync();
            Assert.IsNotNull(items);
        }

        [TestMethod]
        public async Task TestJoinInto()
        {
            var query =
                from c in db.Categories
                join i in db.Items on c.CategoryID equals i.CategoryID into ords
                select new { cust = c, ords = ords.ToList() };

            var items = await query.ToListAsync();
            Assert.IsNotNull(items);
        }

        [TestMethod]
        public async Task TestJoinIntoCount()
        {
            var query =
                from c in db.Categories
                join i in db.Items on c.CategoryID equals i.CategoryID into ords
                select new { cust = c, ords = ords.Count() };

            var items = await query.ToListAsync();
            Assert.IsNotNull(items);
        }

        [TestMethod]
        public async Task TestJoinIntoDefaultIfEmpty()
        {
            var query =
                from c in db.Categories
                join i in db.Items on c.CategoryID equals i.CategoryID into ords
                from i in ords.DefaultIfEmpty()
                select new { c, i };

            var items = await query.ToListAsync();
            Assert.IsNotNull(items);
        }

        [TestMethod]
        public async Task TestJoinWithWhere()
        {
            var query =
                from c in db.Categories
                join i in db.Items on c.CategoryID equals i.CategoryID
                where i.CategoryID == 3
                select i;

            var items = await query.ToListAsync();
            Assert.IsNotNull(items);
            Assert.IsTrue(items.Count == 6);
            Assert.IsTrue(items.All(t => t.CategoryID == 3));
        }

        [TestMethod]
        public async Task TestJoinWithMissingJoinCondition()
        {
            var query =
                from c in db.Categories
                from i in db.Items
                where c.CategoryID == i.CategoryID && c.Name == "category 1"
                select i;
            var items = await query.ToListAsync();
            Assert.IsNotNull(items);
            Assert.IsTrue(items.All(t => t.CategoryID == 1));
            Assert.IsTrue(items.Count == 2);
        }

        [TestMethod]
        public async Task TestSelectWithLimits()
        {
            var items = await db.Items.Skip(3).Take(5).ToListAsync();
            Assert.IsNotNull(items);
            Assert.IsTrue(items.Count == 5);
        }

        [TestMethod]
        public async Task TestCount()
        {
            var count = await db.Categories.CountAsync(t => t.CategoryID % 2 == 0);
            Assert.IsTrue(count == 5);
        }

        [TestMethod]
        public async Task TestOrderBy()
        {
            var items = await db.Items.OrderBy(t => t.Data).ToListAsync();
            Assert.IsNotNull(items);

            for (var i = 1; i < items.Count; i++)
            {
                if (items[i].Data < items[i - 1].Data)
                {
                    Assert.Fail();
                }
            }
        }

        [TestMethod]
        public async Task TestSubQueryCount()
        {
            var query = 
                from c in db.Categories
                where c.CategoryID % 2 == 0
                select new { Count = db.Items.Count(t => t.CategoryID == c.CategoryID), Category = c };
            var result = await query.ToListAsync();
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task TestSubQuerySelect()
        {
            var query =
                from c in db.Categories
                where c.CategoryID % 2 == 0
                select new { Items = db.Items.Where(t => t.CategoryID == c.CategoryID).ToList(), Category = c };
            var result = await query.ToListAsync();
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task TestArrayContains()
        {
            int[] ids = { 1, 2, 5000 };
            var items = await db.Items.Where(t => ids.Contains(t.ItemID)).ToListAsync();
            Assert.IsTrue(items.Count == 2);
        }

        [TestMethod]
        public async Task TestCollectionContains()
        {
            ICollection<int> ids = new[] { 1, 2, 5000 };
            var items = await db.Items.Where(t => ids.Contains(t.ItemID)).ToListAsync();
            Assert.IsTrue(items.Count == 2);
        }

        [TestMethod]
        public async Task TestSum()
        {
            var sum = await db.Categories.ExecuteAsync(t => t.Sum(q => q.CategoryID));
            Assert.AreEqual(sum, (1 + 10) * 10 / 2);
        }

        [TestMethod]
        public async Task TestGroupByCount()
        {
            var query = 
                from i in db.Items
                group i by i.CategoryID into g
                select new {CategoryID = g.Key, Count = g.Count()};

            var items = await query.ToListAsync();
            Assert.IsNotNull(items);
        }

        [TestMethod]
        public async Task TestGroupBySelectMany()
        {
            var items = await db.Items.GroupBy(t => t.CategoryID).SelectMany(t => t).ToListAsync();
            Assert.IsNotNull(items);
        }

        [TestMethod]
        public async Task TestDistinct()
        {
            var query =
                from i in db.Items
                select i.Boolean;
            var values = await query.Distinct().ToListAsync();
            Assert.IsNotNull(values);
            Assert.IsTrue(values.Count == 2);
            Assert.AreNotEqual(values[0], values[1]);
        }
    }
}
