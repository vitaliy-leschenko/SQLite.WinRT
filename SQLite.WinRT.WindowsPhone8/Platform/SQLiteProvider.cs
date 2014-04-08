using System;
using Sqlite;

namespace SQLite.WinRT
{
    public class SQLiteProvider : ISQLiteProvider
    {
        private static ISQLiteProvider instance;

        protected SQLiteProvider()
        {
        }

        public static ISQLiteProvider Instance
        {
            get { return instance ?? (instance = new SQLiteProvider()); }
        }

        public void BusyTimeout(object handle, int totalMilliseconds)
        {
            SQLite3.BusyTimeout((Database)handle, totalMilliseconds);
        }

        public long LastInsertRowid(object handle)
        {
            return SQLite3.LastInsertRowid((Database)handle);
        }

        public SQLiteResult Open(string databasePath, out object handle, int openFlags, object zero)
        {
            Database db;
            var result = SQLite3.Open(databasePath, out db, openFlags, IntPtr.Zero);
            handle = db;
            return result;
        }

        public SQLiteResult Close(object handle)
        {
            return SQLite3.Close((Database)handle);
        }

        public string GetErrorMessage(object handle)
        {
            return SQLite3.GetErrmsg((Database)handle);
        }

        public SQLiteResult Step(object stmt)
        {
            return SQLite3.Step((Statement)stmt);
        }

        public int Changes(object handle)
        {
            return SQLite3.Changes((Database)handle);
        }

        public int ColumnCount(object stmt)
        {
            return SQLite3.ColumnCount((Statement)stmt);
        }

        public string ColumnName16(object stmt, int i)
        {
            return SQLite3.ColumnName16((Statement)stmt, i);
        }

        public ColType ColumnType(object stmt, int i)
        {
            return SQLite3.ColumnType((Statement)stmt, i);
        }

        public void Finalize(object stmt)
        {
            SQLite3.Finalize((Statement)stmt);
        }

        public object Prepare2(object handle, string commandText)
        {
            return SQLite3.Prepare2((Database)handle, commandText);
        }

        public int BindParameterIndex(object stmt, string name)
        {
            return SQLite3.BindParameterIndex((Statement)stmt, name);
        }

        public void BindNull(object stmt, int index)
        {
            SQLite3.BindNull((Statement)stmt, index);
        }

        public void BindInt(object stmt, int index, int value)
        {
            SQLite3.BindInt((Statement)stmt, index, value);
        }

        public void BindText(object stmt, int index, string value, int n, object negativePointer)
        {
            SQLite3.BindText((Statement)stmt, index, value, n, (IntPtr)negativePointer);
        }

        public void BindInt64(object stmt, int index, long value)
        {
            SQLite3.BindInt64((Statement)stmt, index, value);
        }

        public void BindDouble(object stmt, int index, double value)
        {
            SQLite3.BindDouble((Statement)stmt, index, value);
        }

        public void BindBlob(object stmt, int index, byte[] value, int length, object negativePointer)
        {
            SQLite3.BindBlob((Statement)stmt, index, value, length, (IntPtr)negativePointer);
        }

        public string ColumnString(object stmt, int index)
        {
            return SQLite3.ColumnString((Statement)stmt, index);
        }

        public int ColumnInt(object stmt, int index)
        {
            return SQLite3.ColumnInt((Statement)stmt, index);
        }

        public long ColumnInt64(object stmt, int index)
        {
            return SQLite3.ColumnInt64((Statement)stmt, index);
        }

        public double ColumnDouble(object stmt, int index)
        {
            return SQLite3.ColumnDouble((Statement)stmt, index);
        }

        public byte[] ColumnByteArray(object stmt, int index)
        {
            return SQLite3.ColumnByteArray((Statement)stmt, index);
        }

        public void Reset(object stmt)
        {
            SQLite3.Reset((Statement)stmt);
        }
    }
}
