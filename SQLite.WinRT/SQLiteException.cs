using System;

namespace SQLite.WinRT
{
    public class SQLiteException : Exception
    {
        public SQLiteResult Result { get; private set; }

        public SQLiteException(SQLiteResult r, string message): base(message)
        {
            Result = r;
        }
    }
}