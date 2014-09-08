namespace SQLite.WinRT
{
    public class CurrentPlatform: IPlatform
    {
        public ISQLiteProvider SQLiteProvider
        {
            get { return WinRT.SQLiteProvider.Instance; }
        }

        public IPlatformStorage PlatformStorage
        {
            get { return WinRT.PlatformStorage.Instance; }
        }
    }
}
