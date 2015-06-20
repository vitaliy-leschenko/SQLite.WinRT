using System;
using System.Text;

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
            SQLite3.BusyTimeout((IntPtr)handle, totalMilliseconds);
        }

        public long LastInsertRowid(object handle)
        {
            return SQLite3.LastInsertRowid((IntPtr)handle);
        }

        public SQLiteResult Open(string databasePath, out object handle, int openFlags, object zero)
        {
            byte[] databasePathAsBytes = GetNullTerminatedUtf8(databasePath);

            IntPtr db;
            var result = SQLite3.Open(databasePathAsBytes, out db, openFlags, IntPtr.Zero);
            handle = db;
            return result;
        }

        public SQLiteResult Close(object handle)
        {
            return SQLite3.Close((IntPtr)handle);
        }

        public string GetErrorMessage(object handle)
        {
            return SQLite3.GetErrmsg((IntPtr)handle);
        }

        public SQLiteResult Step(object stmt)
        {
            return SQLite3.Step((IntPtr)stmt);
        }

        public int Changes(object handle)
        {
            return SQLite3.Changes((IntPtr)handle);
        }

        public int ColumnCount(object stmt)
        {
            return SQLite3.ColumnCount((IntPtr)stmt);
        }

        public string ColumnName16(object stmt, int i)
        {
            return SQLite3.ColumnName16((IntPtr)stmt, i);
        }

        public ColType ColumnType(object stmt, int i)
        {
            return SQLite3.ColumnType((IntPtr)stmt, i);
        }

        public void Finalize(object stmt)
        {
            SQLite3.Finalize((IntPtr)stmt);
        }

        public object Prepare2(object handle, string commandText)
        {
            return SQLite3.Prepare2((IntPtr)handle, commandText);
        }

        public int BindParameterIndex(object stmt, string name)
        {
            return SQLite3.BindParameterIndex((IntPtr)stmt, name);
        }

        public void BindNull(object stmt, int index)
        {
            SQLite3.BindNull((IntPtr)stmt, index);
        }

        public void BindInt(object stmt, int index, int value)
        {
            SQLite3.BindInt((IntPtr)stmt, index, value);
        }

        public void BindText(object stmt, int index, string value, int n, object negativePointer)
        {
            SQLite3.BindText((IntPtr)stmt, index, value, n, (IntPtr)negativePointer);
        }

        public void BindInt64(object stmt, int index, long value)
        {
            SQLite3.BindInt64((IntPtr)stmt, index, value);
        }

        public void BindDouble(object stmt, int index, double value)
        {
            SQLite3.BindDouble((IntPtr)stmt, index, value);
        }

        public void BindBlob(object stmt, int index, byte[] value, int length, object negativePointer)
        {
            SQLite3.BindBlob((IntPtr)stmt, index, value, length, (IntPtr)negativePointer);
        }

        public string ColumnString(object stmt, int index)
        {
            return SQLite3.ColumnString((IntPtr)stmt, index);
        }

        public int ColumnInt(object stmt, int index)
        {
            return SQLite3.ColumnInt((IntPtr)stmt, index);
        }

        public long ColumnInt64(object stmt, int index)
        {
            return SQLite3.ColumnInt64((IntPtr)stmt, index);
        }

        public double ColumnDouble(object stmt, int index)
        {
            return SQLite3.ColumnDouble((IntPtr)stmt, index);
        }

        public byte[] ColumnByteArray(object stmt, int index)
        {
            return SQLite3.ColumnByteArray((IntPtr)stmt, index);
        }

        public void Reset(object stmt)
        {
            SQLite3.Reset((IntPtr)stmt);
        }

        static byte[] GetNullTerminatedUtf8(string s)
        {
            return Encoding.UTF8.GetBytes(s);
        }

        public int SetLimits(object handle, int id, int val)
        {
            return SQLite3.SetLimits((IntPtr)handle, id, val);
        }
    }
}
