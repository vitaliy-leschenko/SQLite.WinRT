namespace SQLite.WinRT
{
    public interface IPlatformStorage
    {
        void SetTempDirectory();
        string GetDatabasePath(string databasePath);
    }
}