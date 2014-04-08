using System;

namespace SQLite.WinRT
{
    public interface IPlatform
    {
        ISQLiteProvider SQLiteProvider { get; }
        IPlatformStorage PlatformStorage { get; }
    }
}