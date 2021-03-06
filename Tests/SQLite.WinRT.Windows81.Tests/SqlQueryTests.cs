﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SQLite.WinRT.Linq.Base;
using SQLite.WinRT.Tests.Data;
#if WINDOWS_PHONE_APP || NETFX_CORE || WINDOWS_PHONE
using Windows.Storage;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

namespace SQLite.WinRT.Tests
{
    [TestClass]
    public class SqlQueryTests
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
            connection.Trace = false;
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
            connection.Trace = false;
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
        public void UpdateTest()
        {
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
            var count = db.Categories.Delete().Where(t => t.CategoryID).IsLessThanOrEqualTo(3).Execute();
            Assert.IsTrue(count == 3);
            Assert.IsFalse(db.Categories.Any(t => t.CategoryID <= 3));
        }

        [TestMethod]
        public void Delete2Test()
        {
            var count = db.Items.Delete()
                .Where(t => t.CategoryID).IsLessThanOrEqualTo(3)
                .And(t => t.Title).IsEqualTo("item0")
                .Execute();
            Assert.IsTrue(count == 3);
        }

        [TestMethod]
        public void DeleteAllTest()
        {
            db.Items.Delete().Execute();

            Assert.IsFalse(db.Items.Any());
        }

        [TestMethod]
        public void QueryableAnyTest()
        {
            var t = new[] {true};
            var q = System.Linq.Enumerable.SingleOrDefault(t);

            Assert.IsTrue(db.Items.Any());
        }

        [TestMethod]
        public void InsertTest()
        {
            provider.CreateTable<Country>();

            var count = db.Countries.InsertAll(new[]
            {
                new Country {CountryCode = "BY", Text = "Belarus"},
                new Country {CountryCode = "RU", Text = "Russia"}
            });
            Assert.IsTrue(count == 2);
        }
    }
}
