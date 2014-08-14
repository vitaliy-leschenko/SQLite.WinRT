using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SQLite.WinRT.Linq;

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

    [TestClass]
    public class CommonTests
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
        public async Task TestCreateDatabase()
        {
            await connection.CreateTableAsync<TestTable>();
        }


        [TestMethod]
        public async Task TestUpdateDatabase()
        {
            await connection.CreateTableAsync<TestTable>();
            await connection.CreateTableAsync<TestTable2>();
        }

        [TestMethod]
        public async Task TestDropTable()
        {
            await connection.CreateTableAsync<TestTable2>();
            await connection.DropTableAsync<TestTable2>();
        }

        [TestMethod]
        public async Task TestInsert()
        {
            await connection.CreateTableAsync<TestTable>();

            var item = new TestTable();
            item.IntValue = rnd.Next();
            await connection.InsertAsync(item);

            Assert.IsTrue(item.ID != 0);
        }

        [TestMethod]
        public async Task TestUpdate()
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
        public async Task TestDelete()
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
    }
}
