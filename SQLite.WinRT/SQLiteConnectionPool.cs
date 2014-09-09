using System.Collections.Generic;

namespace SQLite.WinRT
{
    public class SQLiteConnectionPool
    {
        class Entry
        {
            public SQLiteConnectionString ConnectionString { get; private set; }
            public SQLiteConnection Connection { get; private set; }

            public Entry(SQLiteConnectionString connectionString)
            {
                ConnectionString = connectionString;
                Connection = new SQLiteConnection(connectionString);
            }

            public void OnApplicationSuspended()
            {
                Connection.Dispose();
                Connection = null;
            }
        }

        readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>();
        readonly object entriesLock = new object();

        static readonly SQLiteConnectionPool shared = new SQLiteConnectionPool();

        /// <summary>
        /// Gets the singleton instance of the connection tool.
        /// </summary>
        public static SQLiteConnectionPool Shared
        {
            get
            {
                return shared;
            }
        }

        public SQLiteConnection GetConnection(SQLiteConnectionString connectionString)
        {
            lock (entriesLock)
            {
                Entry entry;
                var key = connectionString.ConnectionString;

                if (!entries.TryGetValue(key, out entry))
                {
                    entry = new Entry(connectionString);
                    entries[key] = entry;
                }

                return entry.Connection;
            }
        }

        /// <summary>
        /// Closes all connections managed by this pool.
        /// </summary>
        public void Reset()
        {
            lock (entriesLock)
            {
                foreach (var entry in entries.Values)
                {
                    entry.OnApplicationSuspended();
                }
                entries.Clear();
            }
        }

        /// <summary>
        /// Call this method when the application is suspended.
        /// </summary>
        /// <remarks>Behaviour here is to close any open connections.</remarks>
        public void ApplicationSuspended()
        {
            Reset();
        }
    }
}