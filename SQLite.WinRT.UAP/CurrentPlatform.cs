namespace SQLite.WinRT
{
    public class CurrentPlatform: IPlatform
    {
        public ISQLiteProvider SQLiteProvider => WinRT.SQLiteProvider.Instance;

        public IPlatformStorage PlatformStorage => WinRT.PlatformStorage.Instance;
    }
}
