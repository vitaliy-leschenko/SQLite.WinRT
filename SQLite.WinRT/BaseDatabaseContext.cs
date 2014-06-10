using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SQLite.WinRT.Linq.Base;

namespace SQLite.WinRT
{
    public abstract class BaseDatabaseContext
    {
        protected readonly SQLiteAsyncConnection connection;

        protected BaseDatabaseContext(SQLiteAsyncConnection connection)
        {
            this.connection = connection;
        }

        public async Task CreateSchemeAsync()
        {
            await connection.RunInTransactionAsync(CreateScheme);
        }

        public async Task UpdateSchemeAsync()
        {
            await connection.RunAsync(UpdateScheme);
        }

        private void UpdateScheme(SQLiteConnection conn)
        {
            var schemeVersion = GetSchemeVersion();
            var dbVersion = GetDatabaseVersion(conn);

            if (schemeVersion > dbVersion)
            {
                var changesets = GetDatabaseChangesets().Where(t => t.Version > dbVersion);
                foreach (var changeset in changesets)
                {
                    changeset.Update(conn);
                    UpdateDatabaseVersion(conn, changeset.Version);
                }
            }
        }

        private void CreateScheme(SQLiteConnection conn)
        {
            var contextType = GetType();

            var properties = contextType.GetRuntimeProperties().ToList();
            var propertyType = typeof (IEntityTable<>).Name;

            foreach (var types in properties.Where(t=> t.PropertyType.Name == propertyType)
                .Select(t => t.PropertyType.GenericTypeArguments)
                .Where(t => t != null && t.Length == 1))
            {
                conn.CreateTable(types[0]);
            }
        }

        private static void UpdateDatabaseVersion(SQLiteConnection conn, int versionNumber)
        {
            conn.CreateTable<DataVersion>();

            var version = conn.Table<DataVersion>().FirstOrDefault();
            if (version == null)
            {
                version = new DataVersion {Value = versionNumber};
                conn.Insert(version);
            }
            else
            {
                version.Value = versionNumber;
                conn.Update(version);
            }
        }

        protected int GetSchemeVersion()
        {
            var typeInfo = GetType().GetTypeInfo();

            var items = typeInfo.GetCustomAttributes<DatabaseUpdateAttribute>(true);
            var changesets = items.Select(t => Activator.CreateInstance(t.UpdateType)).OfType<IDatabaseChangeset>();
            return changesets.Aggregate(0, (max, c) => Math.Max(max, c.Version));
        }

        protected int GetDatabaseVersion(SQLiteConnection conn)
        {
            conn.CreateTable<DataVersion>();
            var version = conn.Table<DataVersion>().FirstOrDefault();
            return version != null ? version.Value : 0;
        }

        private IEnumerable<IDatabaseChangeset> GetDatabaseChangesets()
        {
            var typeInfo = GetType().GetTypeInfo();
            var items = typeInfo.GetCustomAttributes<DatabaseUpdateAttribute>(true);
            return items.Select(t => Activator.CreateInstance(t.UpdateType)).OfType<IDatabaseChangeset>().OrderBy(t => t.Version);
        }
    }
}
