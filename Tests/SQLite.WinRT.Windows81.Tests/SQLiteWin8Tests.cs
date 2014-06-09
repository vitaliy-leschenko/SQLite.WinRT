using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

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

    public class Category
    {
        [PrimaryKey, AutoIncrement]
        public int CategoryID { get; set; }
        public string Name { get; set; }
    }

    public class Item
    {
        [PrimaryKey, AutoIncrement]
        public int ItemID { get; set; }
        public int CategoryID { get; set; }
        public string Title { get; set; }
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
            connection = new SQLiteAsyncConnection(Path.Combine(folder.Path, DbName));
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
            await connection.InsertAsync(cat);

            for (int i = 0; i < 10; i++)
            {
                var item = new Item();
                item.CategoryID = cat.CategoryID;
                item.Title = "item" + i;
                await connection.InsertAsync(item);
            }

            var categoryID = cat.CategoryID;

            var query = 
                from c in connection.LinqTable<Category>()
                join i in connection.LinqTable<Item>() on c.CategoryID equals i.CategoryID
                where c.CategoryID == categoryID
                select i;

            var items = query.ToList();
            Assert.IsNotNull(items);
        }
    }
}
