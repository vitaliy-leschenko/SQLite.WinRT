using System;
using Sqlite3 = Sqlite.WP81.Sqlite3;
using Sqlite3DatabaseHandle = Sqlite.WP81.Database;
using Sqlite3Statement = Sqlite.WP81.Statement;

namespace SQLite.WinRT
{
    public static class SQLite3
    {
        public static SQLiteResult Open(string filename, out Sqlite3DatabaseHandle db)
        {
            return (SQLiteResult)Sqlite3.sqlite3_open(filename, out db);
        }

        public static SQLiteResult Open(string filename, out Sqlite3DatabaseHandle db, int flags, IntPtr zVfs)
        {
            return (SQLiteResult)Sqlite3.sqlite3_open_v2(filename, out db, flags, "");
        }

        public static SQLiteResult Close(Sqlite3DatabaseHandle db)
        {
            return (SQLiteResult)Sqlite3.sqlite3_close(db);
        }

        public static SQLiteResult BusyTimeout(Sqlite3DatabaseHandle db, int milliseconds)
        {
            return (SQLiteResult)Sqlite3.sqlite3_busy_timeout(db, milliseconds);
        }

        public static int Changes(Sqlite3DatabaseHandle db)
        {
            return Sqlite3.sqlite3_changes(db);
        }

        public static Sqlite3Statement Prepare2(Sqlite3DatabaseHandle db, string query)
        {
            Sqlite3Statement stmt;
            var r = Sqlite3.sqlite3_prepare_v2(db, query, out stmt);
            if (r != 0)
            {
                throw new SQLiteException((SQLiteResult)r, GetErrmsg(db));
            }
            return stmt;
        }

        public static SQLiteResult Step(Sqlite3Statement stmt)
        {
            return (SQLiteResult)Sqlite3.sqlite3_step(stmt);
        }

        public static SQLiteResult Reset(Sqlite3Statement stmt)
        {
            return (SQLiteResult)Sqlite3.sqlite3_reset(stmt);
        }

        public static SQLiteResult Finalize(Sqlite3Statement stmt)
        {
            return (SQLiteResult)Sqlite3.sqlite3_finalize(stmt);
        }

        public static long LastInsertRowid(Sqlite3DatabaseHandle db)
        {
            return Sqlite3.sqlite3_last_insert_rowid(db);
        }

        public static string GetErrmsg(Sqlite3DatabaseHandle db)
        {
            return Sqlite3.sqlite3_errmsg(db);
        }

        public static int BindParameterIndex(Sqlite3Statement stmt, string name)
        {
            return Sqlite3.sqlite3_bind_parameter_index(stmt, name);
        }

        public static int BindNull(Sqlite3Statement stmt, int index)
        {
            return Sqlite3.sqlite3_bind_null(stmt, index);
        }

        public static int BindInt(Sqlite3Statement stmt, int index, int val)
        {
            return Sqlite3.sqlite3_bind_int(stmt, index, val);
        }

        public static int BindInt64(Sqlite3Statement stmt, int index, long val)
        {
            return Sqlite3.sqlite3_bind_int64(stmt, index, val);
        }

        public static int BindDouble(Sqlite3Statement stmt, int index, double val)
        {
            return Sqlite3.sqlite3_bind_double(stmt, index, val);
        }

        public static int BindText(Sqlite3Statement stmt, int index, string val, int n, IntPtr free)
        {
            return Sqlite3.sqlite3_bind_text(stmt, index, val, n);
        }

        public static int BindBlob(Sqlite3Statement stmt, int index, byte[] val, int n, IntPtr free)
        {
            return Sqlite3.sqlite3_bind_blob(stmt, index, val, n);
        }

        public static int ColumnCount(Sqlite3Statement stmt)
        {
            return Sqlite3.sqlite3_column_count(stmt);
        }

        public static string ColumnName(Sqlite3Statement stmt, int index)
        {
            return Sqlite3.sqlite3_column_name(stmt, index);
        }

        public static string ColumnName16(Sqlite3Statement stmt, int index)
        {
            return Sqlite3.sqlite3_column_name(stmt, index);
        }

        public static ColType ColumnType(Sqlite3Statement stmt, int index)
        {
            return (ColType)Sqlite3.sqlite3_column_type(stmt, index);
        }

        public static int ColumnInt(Sqlite3Statement stmt, int index)
        {
            return Sqlite3.sqlite3_column_int(stmt, index);
        }

        public static long ColumnInt64(Sqlite3Statement stmt, int index)
        {
            return Sqlite3.sqlite3_column_int64(stmt, index);
        }

        public static double ColumnDouble(Sqlite3Statement stmt, int index)
        {
            return Sqlite3.sqlite3_column_double(stmt, index);
        }

        public static string ColumnText(Sqlite3Statement stmt, int index)
        {
            return Sqlite3.sqlite3_column_text(stmt, index);
        }

        public static string ColumnText16(Sqlite3Statement stmt, int index)
        {
            return Sqlite3.sqlite3_column_text(stmt, index);
        }

        public static byte[] ColumnBlob(Sqlite3Statement stmt, int index)
        {
            return Sqlite3.sqlite3_column_blob(stmt, index);
        }

        public static int ColumnBytes(Sqlite3Statement stmt, int index)
        {
            return Sqlite3.sqlite3_column_bytes(stmt, index);
        }

        public static string ColumnString(Sqlite3Statement stmt, int index)
        {
            return Sqlite3.sqlite3_column_text(stmt, index);
        }

        public static byte[] ColumnByteArray(Sqlite3Statement stmt, int index)
        {
            return ColumnBlob(stmt, index);
        }
    }
}