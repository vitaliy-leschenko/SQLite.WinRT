using System;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Windows.Globalization.DateTimeFormatting;
using Windows.Storage;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SQLite.WinRT.Linq;
using SQLite.WinRT.Linq.Base;

namespace SQLite.WinRT.Tests
{
    [Table("TestTable")]
    public class TestTable
    {
        [PrimaryKey, AutoIncrement]
        public int ID { get; set; }
        public int IntValue { get; set; }
    }

    [Table("TestTable")]
    public class TestTable2
    {
        [PrimaryKey, AutoIncrement]
        public int ID { get; set; }
        public int IntValue { get; set; }
        public double DoubleValue { get; set; }
    }

    [Table("Categories")]
    public class Category
    {
        [PrimaryKey, AutoIncrement]
        public int CategoryID { get; set; }
        public string Name { get; set; }

        public byte[] Text { get; set; }
    }

    [Table("Items")]
    public class Item
    {
        [PrimaryKey, AutoIncrement]
        public int ItemID { get; set; }
        public int CategoryID { get; set; }
        public string Title { get; set; }

        public int? Data { get; set; }

        public DateTime? Time { get; set; }

        public bool Boolean { get; set; }
    }

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

    [TestClass]
    public class SQLiteWin8Tests
    {
        private readonly Random rnd = new Random();

        private const string DbName = "db.sqlite";

        private SQLiteAsyncConnection connection;

        [TestInitialize]
        public void TestInitialize()
        {
            var folder = ApplicationData.Current.LocalFolder;
            connection = new SQLiteAsyncConnection(Path.Combine(folder.Path, DbName), true);
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

        [TestMethod]
        public async Task CreateDatabaseTest()
        {
            await connection.CreateTableAsync<TestTable>();
        }


        [TestMethod]
        public async Task UpdateDatabaseTest()
        {
            await connection.CreateTableAsync<TestTable>();
            await connection.CreateTableAsync<TestTable2>();
        }

        [TestMethod]
        public async Task DropTableTest()
        {
            await connection.CreateTableAsync<TestTable2>();
            await connection.DropTableAsync<TestTable2>();
        }

        [TestMethod]
        public async Task InsertTest()
        {
            await connection.CreateTableAsync<TestTable>();

            var item = new TestTable();
            item.IntValue = rnd.Next();
            await connection.InsertAsync(item);

            Assert.IsTrue(item.ID != 0);
        }


        [TestMethod]
        public async Task SelectTest()
        {
            await connection.CreateTableAsync<TestTable>();

            var count = rnd.Next(10) + 5;

            for (int i = 0; i < count; i++)
            {
                var item = new TestTable();
                item.IntValue = rnd.Next();
                await connection.InsertAsync(item);
            }

            var items = await connection.Table<TestTable>().OrderBy(t => t.IntValue).ToListAsync();
            Assert.IsTrue(items.Count == count);

            for (int i = 0; i < items.Count - 2; i++)
            {
                Assert.IsTrue(items[i].IntValue <= items[i + 1].IntValue);
            }
        }

        [TestMethod]
        public async Task UpdateTest()
        {
            await connection.CreateTableAsync<TestTable>();

            var item = new TestTable();
            item.IntValue = 100;
            await connection.InsertAsync(item);

            item.IntValue = 200;
            await connection.UpdateAsync(item);

            var table = connection.Table<TestTable>();
            var result = await table.Where(t => t.IntValue == 200).FirstAsync();
            Assert.IsTrue(result.ID == item.ID);
        }

        [TestMethod]
        public async Task DeleteTest()
        {
            await connection.CreateTableAsync<TestTable>();

            var item = new TestTable();
            item.IntValue = 200;
            await connection.InsertAsync(item);

            var table = connection.Table<TestTable>();
            var result = await table.Where(t => t.IntValue == 200).FirstOrDefaultAsync();
            Assert.IsNotNull(result);
            Assert.IsTrue(result.ID == item.ID);

            await connection.DeleteAsync(item);
            result = await table.Where(t => t.IntValue == 200).FirstOrDefaultAsync();
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task LinqTest()
        {
            await connection.CreateTableAsync<Category>();
            await connection.CreateTableAsync<Item>();

            var cat = new Category();
            cat.Name = "test category";
            cat.Text = Encoding.UTF8.GetBytes("message");
            await connection.InsertAsync(cat);

            var test = false;
            for (int i = 0; i < 10; i++)
            {
                var item = new Item();
                item.CategoryID = cat.CategoryID;
                item.Title = "item" + i;
                if (i % 2 == 0)
                {
                    item.Data = i;
                    item.Time = DateTime.UtcNow;
                }
                item.Boolean = test;
                test = !test;

                await connection.InsertAsync(item);
            }

            var categoryID = cat.CategoryID;

            var db = new DbContext(connection);

            var query =
                from c in db.Categories
                join i in db.Items on c.CategoryID equals i.CategoryID
                where c.CategoryID == categoryID
                select i;

            var items = await query.Skip(2).Take(5).ToListAsync();
            Assert.IsNotNull(items);

            var count = await db.Items.CountAsync(t => t.ItemID > 6);
            Assert.IsTrue(count == 4);

            var ct = await db.Categories.FirstOrDefaultAsync(t => t.CategoryID == cat.CategoryID);
            Assert.IsNotNull(ct);
            Assert.AreEqual(ct.CategoryID, cat.CategoryID);
            Assert.AreEqual(ct.Name, cat.Name);

            var sm = await db.Items.DoAsync(t => t.Sum(q => q.ItemID));
            Assert.AreEqual(sm, (1 + 10) * 10 / 2);
        }
    }
}
