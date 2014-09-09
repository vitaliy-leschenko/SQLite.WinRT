using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SQLite.WinRT.Linq;
using SQLite.WinRT.Linq.Base;
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

        private IEntityProvider provider;

#if WINDOWS_PHONE_APP || NETFX_CORE || WINDOWS_PHONE
        [TestInitialize]
        public void TestInitialize()
        {
            var folder = ApplicationData.Current.LocalFolder;
            var connectionString = new SQLiteConnectionString(Path.Combine(folder.Path, DbName), true);
            var connection = SQLiteConnectionPool.Shared.GetConnection(connectionString);
            connection.Trace = true;
            connection.TimeExecution = true;
            provider = connection.GetEntityProvider();
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
            connection.Trace = true;
            connection.TimeExecution = true;
            provider = connection.GetEntityProvider();
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

        [TestMethod]
        public async Task TestCreateDatabase()
        {
            await provider.CreateTableAsync<TestTable>();
        }


        [TestMethod]
        public async Task TestUpdateDatabase()
        {
            await provider.CreateTableAsync<TestTable>();
            await provider.CreateTableAsync<TestTable2>();
        }

        [TestMethod]
        public async Task TestDropTable()
        {
            await provider.CreateTableAsync<TestTable2>();
            await provider.DropTableAsync("TestTable2");
        }

        [TestMethod]
        public async Task TestInsert()
        {
            await provider.CreateTableAsync<TestTable>();

            var item = new TestTable();
            item.IntValue = rnd.Next();
            await provider.GetTable<TestTable>().InsertAsync(item);

            Assert.IsTrue(item.ID != 0);
        }

        [TestMethod]
        public async Task TestUpdate()
        {
            await provider.CreateTableAsync<TestTable>();
            var table = provider.GetTable<TestTable>();

            var item = new TestTable();
            item.IntValue = 100;
            await table.InsertAsync(item);

            item.IntValue = 200;
            await table.UpdateAsync(item);

            var result = await table.Where(t => t.IntValue == 200).FirstAsync();
            Assert.IsTrue(result.ID == item.ID);
        }

        [TestMethod]
        public async Task TestDelete()
        {
            await provider.CreateTableAsync<TestTable>();
            var table = provider.GetTable<TestTable>();

            var item = new TestTable();
            item.IntValue = 200;
            await table.InsertAsync(item);

            var result = await table.Where(t => t.IntValue == 200).FirstOrDefaultAsync();
            Assert.IsNotNull(result);
            Assert.IsTrue(result.ID == item.ID);

            await table.DeleteAsync(item);
            result = await table.Where(t => t.IntValue == 200).FirstOrDefaultAsync();
            Assert.IsNull(result);
        }
    }
}
