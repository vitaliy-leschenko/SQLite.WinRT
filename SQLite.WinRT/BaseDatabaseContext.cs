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
        protected readonly SQLiteConnection connection;
        protected readonly IEntityProvider provider;

        protected BaseDatabaseContext(SQLiteConnection connection)
        {
            this.connection = connection;
            provider = connection.GetEntityProvider();
        }

        public Task CreateSchemeAsync()
        {
            using (connection.Lock())
            {
                var contextType = GetType();

                var properties = contextType.GetRuntimeProperties().ToList();
                var propertyType = typeof(IEntityTable<>).Name;

                foreach (var types in properties.Where(t => t.PropertyType.Name == propertyType)
                    .Select(t => t.PropertyType.GenericTypeArguments)
                    .Where(t => t != null && t.Length == 1))
                {
                    provider.CreateTable(types[0]);
                }
            }
            return Task.FromResult(0);
        }

        public async Task UpdateSchemeAsync()
        {
            using (connection.Lock())
            {
                var schemeVersion = GetSchemeVersion();
                var dbVersion = GetDatabaseVersion();

                if (schemeVersion > dbVersion)
                {
                    var changesets = GetDatabaseChangesets().Where(t => t.Version > dbVersion);
                    foreach (var changeset in changesets)
                    {
                        var aset = changeset as IDatabaseAsyncChangeset;
                        if (aset != null)
                        {
                            await aset.UpdateAsync(provider);
                        }
                        else
                        {
                            var sset = changeset as IDatabaseChangeset;
                            if (sset != null)
                            {
                                sset.Update(provider);
                            }
                        }
                        UpdateDatabaseVersion(changeset.Version);
                    }
                }
            }
        }

        private void UpdateDatabaseVersion(int versionNumber)
        {
            provider.CreateTable(typeof(DataVersion));

            var table = provider.GetTable<DataVersion>();

            var version = table.FirstOrDefault();
            if (version == null)
            {
                version = new DataVersion {Value = versionNumber};
                provider.GetTable<DataVersion>().Insert(version);
            }
            else
            {
                version.Value = versionNumber;
                table.Update(version);
            }
        }

        protected int GetSchemeVersion()
        {
            var typeInfo = GetType().GetTypeInfo();

            var items = typeInfo.GetCustomAttributes<DatabaseUpdateAttribute>(true);
            var changesets = items.Select(t => Activator.CreateInstance(t.UpdateType)).OfType<IDatabaseChangeset>();
            return changesets.Aggregate(0, (max, c) => Math.Max(max, c.Version));
        }

        protected int GetDatabaseVersion()
        {
            provider.CreateTable(typeof(DataVersion));
            var version = provider.GetTable<DataVersion>().FirstOrDefault();
            return version != null ? version.Value : 0;
        }

        private IEnumerable<IBaseDatabaseChangeset> GetDatabaseChangesets()
        {
            var typeInfo = GetType().GetTypeInfo();
            var items = typeInfo.GetCustomAttributes<DatabaseUpdateAttribute>(true);
            return items.Select(t => Activator.CreateInstance(t.UpdateType)).OfType<IBaseDatabaseChangeset>().OrderBy(t => t.Version);
        }
    }
}
