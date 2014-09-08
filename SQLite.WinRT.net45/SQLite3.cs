using System;
using System.Runtime.InteropServices;

namespace SQLite.WinRT
{
    public static class SQLite3
    {
        [DllImport("sqlite3.dll", EntryPoint = "sqlite3_open", CallingConvention=CallingConvention.Cdecl)]
        public static extern SQLiteResult Open ([MarshalAs(UnmanagedType.LPStr)] string filename, out IntPtr db);

        [DllImport("sqlite3.dll", EntryPoint = "sqlite3_open_v2", CallingConvention = CallingConvention.Cdecl)]
        public static extern SQLiteResult Open(byte[] filename, out IntPtr db, int flags, IntPtr zvfs);

        [DllImport("sqlite3.dll", EntryPoint = "sqlite3_open16", CallingConvention = CallingConvention.Cdecl)]
        public static extern SQLiteResult Open16([MarshalAs(UnmanagedType.LPWStr)] string filename, out IntPtr db);

        [DllImport("sqlite3.dll", EntryPoint = "sqlite3_close", CallingConvention=CallingConvention.Cdecl)]
        public static extern SQLiteResult Close (IntPtr db);

        [DllImport("sqlite3.dll", EntryPoint = "sqlite3_config", CallingConvention=CallingConvention.Cdecl)]
        public static extern SQLiteResult Config (ConfigOption option);

        [DllImport("sqlite3.dll", EntryPoint = "sqlite3_win32_set_directory", CallingConvention=CallingConvention.Cdecl, CharSet=CharSet.Unicode)]
        public static extern int SetDirectory (uint directoryType, string directoryPath);

        [DllImport("sqlite3.dll", EntryPoint = "sqlite3_busy_timeout", CallingConvention=CallingConvention.Cdecl)]
        public static extern SQLiteResult BusyTimeout (IntPtr db, int milliseconds);

        [DllImport("sqlite3.dll", EntryPoint = "sqlite3_changes", CallingConvention=CallingConvention.Cdecl)]
        public static extern int Changes (IntPtr db);

        [DllImport("sqlite3.dll", EntryPoint = "sqlite3_prepare_v2", CallingConvention=CallingConvention.Cdecl)]
        public static extern SQLiteResult Prepare2 (IntPtr db, [MarshalAs(UnmanagedType.LPStr)] string sql, int numBytes, out IntPtr stmt, IntPtr pzTail);

        public static IntPtr Prepare2 (IntPtr db, string query)
        {
            IntPtr stmt;
            var r = Prepare2 (db, query, query.Length, out stmt, IntPtr.Zero);
            if (r != SQLiteResult.OK) {
                throw SQLiteException.New (r, GetErrmsg (db));
            }
            return stmt;
        }

        [DllImport("sqlite3.dll", EntryPoint = "sqlite3_step", CallingConvention=CallingConvention.Cdecl)]
        public static extern SQLiteResult Step (IntPtr stmt);

        [DllImport("sqlite3.dll", EntryPoint = "sqlite3_reset", CallingConvention=CallingConvention.Cdecl)]
        public static extern SQLiteResult Reset (IntPtr stmt);

        [DllImport("sqlite3.dll", EntryPoint = "sqlite3_finalize", CallingConvention=CallingConvention.Cdecl)]
        public static extern SQLiteResult Finalize (IntPtr stmt);

        [DllImport("sqlite3.dll", EntryPoint = "sqlite3_last_insert_rowid", CallingConvention=CallingConvention.Cdecl)]
        public static extern long LastInsertRowid (IntPtr db);

        [DllImport("sqlite3.dll", EntryPoint = "sqlite3_errmsg16", CallingConvention=CallingConvention.Cdecl)]
        public static extern IntPtr Errmsg (IntPtr db);

        public static string GetErrmsg (IntPtr db)
        {
            return Marshal.PtrToStringUni (Errmsg (db));
        }

        [DllImport("sqlite3.dll", EntryPoint = "sqlite3_bind_parameter_index", CallingConvention=CallingConvention.Cdecl)]
        public static extern int BindParameterIndex (IntPtr stmt, [MarshalAs(UnmanagedType.LPStr)] string name);

        [DllImport("sqlite3.dll", EntryPoint = "sqlite3_bind_null", CallingConvention=CallingConvention.Cdecl)]
        public static extern int BindNull (IntPtr stmt, int index);

        [DllImport("sqlite3.dll", EntryPoint = "sqlite3_bind_int", CallingConvention=CallingConvention.Cdecl)]
        public static extern int BindInt (IntPtr stmt, int index, int val);

        [DllImport("sqlite3.dll", EntryPoint = "sqlite3_bind_int64", CallingConvention=CallingConvention.Cdecl)]
        public static extern int BindInt64 (IntPtr stmt, int index, long val);

        [DllImport("sqlite3.dll", EntryPoint = "sqlite3_bind_double", CallingConvention=CallingConvention.Cdecl)]
        public static extern int BindDouble (IntPtr stmt, int index, double val);

        [DllImport("sqlite3.dll", EntryPoint = "sqlite3_bind_text16", CallingConvention=CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        public static extern int BindText (IntPtr stmt, int index, [MarshalAs(UnmanagedType.LPWStr)] string val, int n, IntPtr free);

        [DllImport("sqlite3.dll", EntryPoint = "sqlite3_bind_blob", CallingConvention=CallingConvention.Cdecl)]
        public static extern int BindBlob (IntPtr stmt, int index, byte[] val, int n, IntPtr free);

        [DllImport("sqlite3.dll", EntryPoint = "sqlite3_column_count", CallingConvention=CallingConvention.Cdecl)]
        public static extern int ColumnCount (IntPtr stmt);

        [DllImport("sqlite3.dll", EntryPoint = "sqlite3_column_name", CallingConvention=CallingConvention.Cdecl)]
        public static extern IntPtr ColumnName (IntPtr stmt, int index);

        [DllImport("sqlite3.dll", EntryPoint = "sqlite3_column_name16", CallingConvention=CallingConvention.Cdecl)]
        private static extern IntPtr ColumnName16Internal (IntPtr stmt, int index);
        public static string ColumnName16(IntPtr stmt, int index)
        {
            return Marshal.PtrToStringUni(ColumnName16Internal(stmt, index));
        }

        [DllImport("sqlite3.dll", EntryPoint = "sqlite3_column_type", CallingConvention=CallingConvention.Cdecl)]
        public static extern ColType ColumnType (IntPtr stmt, int index);

        [DllImport("sqlite3.dll", EntryPoint = "sqlite3_column_int", CallingConvention=CallingConvention.Cdecl)]
        public static extern int ColumnInt (IntPtr stmt, int index);

        [DllImport("sqlite3.dll", EntryPoint = "sqlite3_column_int64", CallingConvention=CallingConvention.Cdecl)]
        public static extern long ColumnInt64 (IntPtr stmt, int index);

        [DllImport("sqlite3.dll", EntryPoint = "sqlite3_column_double", CallingConvention=CallingConvention.Cdecl)]
        public static extern double ColumnDouble (IntPtr stmt, int index);

        [DllImport("sqlite3.dll", EntryPoint = "sqlite3_column_text", CallingConvention=CallingConvention.Cdecl)]
        public static extern IntPtr ColumnText (IntPtr stmt, int index);

        [DllImport("sqlite3.dll", EntryPoint = "sqlite3_column_text16", CallingConvention=CallingConvention.Cdecl)]
        public static extern IntPtr ColumnText16 (IntPtr stmt, int index);

        [DllImport("sqlite3.dll", EntryPoint = "sqlite3_column_blob", CallingConvention=CallingConvention.Cdecl)]
        public static extern IntPtr ColumnBlob (IntPtr stmt, int index);

        [DllImport("sqlite3.dll", EntryPoint = "sqlite3_column_bytes", CallingConvention=CallingConvention.Cdecl)]
        public static extern int ColumnBytes (IntPtr stmt, int index);

        public static string ColumnString (IntPtr stmt, int index)
        {
            return Marshal.PtrToStringUni (SQLite3.ColumnText16 (stmt, index));
        }

        public static byte[] ColumnByteArray (IntPtr stmt, int index)
        {
            int length = ColumnBytes (stmt, index);
            byte[] result = new byte[length];
            if (length > 0)
                Marshal.Copy (ColumnBlob (stmt, index), result, 0, length);
            return result;
        }
    }
}