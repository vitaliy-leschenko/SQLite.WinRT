namespace SQLite.WinRT
{
    /// <summary>
    /// Represents a parsed connection string.
    /// </summary>
    public class SQLiteConnectionString
    {
        public string ConnectionString { get; private set; }
        public string DatabasePath { get; private set; }
        public bool StoreDateTimeAsTicks { get; private set; }

        public SQLiteConnectionString (string databasePath, bool storeDateTimeAsTicks)
        {
            ConnectionString = databasePath;
            StoreDateTimeAsTicks = storeDateTimeAsTicks;

            DatabasePath = Platform.Current.PlatformStorage.GetDatabasePath(databasePath);
        }
    }
}