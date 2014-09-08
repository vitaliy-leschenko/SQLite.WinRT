using System.IO;

namespace SQLite.WinRT
{
    public class PlatformStorage : IPlatformStorage
    {
        private static IPlatformStorage instance;

        protected PlatformStorage()
        {
        }

        public static IPlatformStorage Instance
        {
            get { return instance ?? (instance = new PlatformStorage()); }
        }

        public void SetTempDirectory()
        {
            SQLite3.SetDirectory(/*temp directory type*/2, Path.GetTempPath());
        }

        public string GetDatabasePath(string databasePath)
        {
            return databasePath;
        }
    }
}