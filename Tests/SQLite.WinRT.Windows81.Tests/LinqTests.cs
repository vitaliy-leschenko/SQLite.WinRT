using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SQLite.WinRT.Linq;
using SQLite.WinRT.Tests.Data;

namespace SQLite.WinRT.Tests
{
    [TestClass]
    public class LinqTests: BaseSQLiteTests
    {
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
    }
}
