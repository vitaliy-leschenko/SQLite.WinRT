namespace SQLite.WinRT
{
    public interface IDatabaseChangeset
    {
        int Version { get; }
        void Update(SQLiteConnection connection);
    }
}