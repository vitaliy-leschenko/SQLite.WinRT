using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SQLite.WinRT.Linq;
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
    public class LinqTests
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
        public async Task TestSelect()
        {
            var db = new DbContext(connection);
            var categories = await db.Categories.ToListAsync();
            Assert.IsNotNull(categories);
            Assert.IsTrue(categories.Count == 10);
        }

        [TestMethod]
        public async Task TestSelectWithWhere()
        {
            var db = new DbContext(connection);
            var items = await db.Items.Where(t => t.Boolean).ToListAsync();
            Assert.IsNotNull(items);
        }

        [TestMethod]
        public async Task TestJoin()
        {
            var db = new DbContext(connection);
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
            var db = new DbContext(connection);
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
            var db = new DbContext(connection);
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
            var db = new DbContext(connection);
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
            var db = new DbContext(connection);
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
            var db = new DbContext(connection);
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
            var db = new DbContext(connection);
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
            var db = new DbContext(connection);
            var items = await db.Items.Skip(3).Take(5).ToListAsync();
            Assert.IsNotNull(items);
            Assert.IsTrue(items.Count == 5);
        }

        [TestMethod]
        public async Task TestCount()
        {
            var db = new DbContext(connection);
            var count = await db.Categories.CountAsync(t => t.CategoryID % 2 == 0);
            Assert.IsTrue(count == 5);
        }

        [TestMethod]
        public async Task TestOrderBy()
        {
            var db = new DbContext(connection);
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
            var db = new DbContext(connection);
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
            var db = new DbContext(connection);
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
            var db = new DbContext(connection);
            int[] ids = { 1, 2, 5000 };
            var items = await db.Items.Where(t => ids.Contains(t.ItemID)).ToListAsync();
            Assert.IsTrue(items.Count == 2);
        }

        [TestMethod]
        public async Task TestCollectionContains()
        {
            var db = new DbContext(connection);
            ICollection<int> ids = new[] { 1, 2, 5000 };
            var items = await db.Items.Where(t => ids.Contains(t.ItemID)).ToListAsync();
            Assert.IsTrue(items.Count == 2);
        }

        [TestMethod]
        public async Task TestSum()
        {
            var db = new DbContext(connection);
            var sum = await db.Categories.ExecuteAsync(t => t.Sum(q => q.CategoryID));
            Assert.AreEqual(sum, (1 + 10) * 10 / 2);
        }

        [TestMethod]
        public async Task TestGroupByCount()
        {
            var db = new DbContext(connection);
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
            var db = new DbContext(connection);
            var items = await db.Items.GroupBy(t => t.CategoryID).SelectMany(t => t).ToListAsync();
            Assert.IsNotNull(items);
        }

        [TestMethod]
        public async Task TestDistinct()
        {
            var db = new DbContext(connection);
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
