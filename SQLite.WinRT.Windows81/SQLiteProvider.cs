using SQLite.Core;

namespace SQLite.WinRT
{
    public class SQLiteProvider : ISQLiteProvider
    {
        private static ISQLiteProvider instance;

        protected SQLiteProvider()
        {
        }

        public static ISQLiteProvider Instance => instance ?? (instance = new SQLiteProvider());

        public void BusyTimeout(object handle, int totalMilliseconds)
        {
            Sqlite3.sqlite3_busy_timeout((Database)handle, totalMilliseconds);
        }

        public long LastInsertRowid(object handle)
        {
            return Sqlite3.sqlite3_last_insert_rowid((Database)handle);
        }

        public SQLiteResult Open(string databasePath, out object handle, int openFlags, object zero)
        {
            Database db;
            var result = (SQLiteResult) Sqlite3.sqlite3_open_v2(databasePath, out db, openFlags, "");
            handle = db;
            return result;
        }

        public SQLiteResult Close(object handle)
        {
            return (SQLiteResult)Sqlite3.sqlite3_close((Database)handle);
        }

        public string GetErrorMessage(object handle)
        {
            return Sqlite3.sqlite3_errmsg((Database)handle);
        }

        public SQLiteResult Step(object stmt)
        {
            return (SQLiteResult)Sqlite3.sqlite3_step((Statement)stmt);
        }

        public int Changes(object handle)
        {
            return Sqlite3.sqlite3_changes((Database)handle);
        }

        public int ColumnCount(object stmt)
        {
            return Sqlite3.sqlite3_column_count((Statement)stmt);
        }

        public string ColumnName16(object stmt, int i)
        {
            return Sqlite3.sqlite3_column_name((Statement)stmt, i);
        }

        public ColType ColumnType(object stmt, int i)
        {
            return (ColType)Sqlite3.sqlite3_column_type((Statement)stmt, i);
        }

        public void Finalize(object stmt)
        {
            Sqlite3.sqlite3_finalize((Statement)stmt);
        }

        public object Prepare2(object handle, string commandText)
        {
            var db = (Database) handle;

            Statement stmt;
            var r = Sqlite3.sqlite3_prepare_v2(db, commandText, out stmt);
            if (r == 0) return stmt;

            var error = Sqlite3.sqlite3_errmsg(db);
            throw new SQLiteException((SQLiteResult)r, error);
        }

        public int BindParameterIndex(object stmt, string name)
        {
            return Sqlite3.sqlite3_bind_parameter_index((Statement)stmt, name);
        }

        public void BindNull(object stmt, int index)
        {
            Sqlite3.sqlite3_bind_null((Statement)stmt, index);
        }

        public void BindInt(object stmt, int index, int value)
        {
            Sqlite3.sqlite3_bind_int((Statement)stmt, index, value);
        }

        public void BindText(object stmt, int index, string value, int n, object negativePointer)
        {
            Sqlite3.sqlite3_bind_text((Statement)stmt, index, value, n);
        }

        public void BindInt64(object stmt, int index, long value)
        {
            Sqlite3.sqlite3_bind_int64((Statement)stmt, index, value);
        }

        public void BindDouble(object stmt, int index, double value)
        {
            Sqlite3.sqlite3_bind_double((Statement)stmt, index, value);
        }

        public void BindBlob(object stmt, int index, byte[] value, int length, object negativePointer)
        {
            Sqlite3.sqlite3_bind_blob((Statement)stmt, index, value, length);
        }

        public string ColumnString(object stmt, int index)
        {
            return Sqlite3.sqlite3_column_text((Statement)stmt, index);
        }

        public int ColumnInt(object stmt, int index)
        {
            return Sqlite3.sqlite3_column_int((Statement)stmt, index);
        }

        public long ColumnInt64(object stmt, int index)
        {
            return Sqlite3.sqlite3_column_int64((Statement)stmt, index);
        }

        public double ColumnDouble(object stmt, int index)
        {
            return Sqlite3.sqlite3_column_double((Statement)stmt, index);
        }

        public byte[] ColumnByteArray(object stmt, int index)
        {
            return Sqlite3.sqlite3_column_blob((Statement)stmt, index);
        }

        public void Reset(object stmt)
        {
            Sqlite3.sqlite3_reset((Statement)stmt);
        }
    }
}
