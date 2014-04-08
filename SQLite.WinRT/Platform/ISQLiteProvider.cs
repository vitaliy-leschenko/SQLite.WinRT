using System;

namespace SQLite.WinRT
{
    public interface ISQLiteProvider
    {
        long LastInsertRowid(object handle);
        void BusyTimeout(object handle, int totalMilliseconds);

        SQLiteResult Open(string databasePath, out object handle, int openFlags, object zero);
        SQLiteResult Close(object handle);
        string GetErrorMessage(object handle);
        SQLiteResult Step(object stmt);
        int Changes(object handle);
        int ColumnCount(object stmt);
        string ColumnName16(object stmt, int i);
        ColType ColumnType(object stmt, int i);
        void Finalize(object stmt);
        object Prepare2(object handle, string commandText);
        int BindParameterIndex(object stmt, string name);
        void BindNull(object stmt, int index);
        void BindInt(object stmt, int index, int value);
        void BindText(object stmt, int index, string value, int n, object negativePointer);
        void BindInt64(object stmt, int index, long toInt64);
        void BindDouble(object stmt, int index, double value);
        void BindBlob(object stmt, int index, byte[] value, int length, object negativePointer);
        string ColumnString(object stmt, int index);
        int ColumnInt(object stmt, int index);
        long ColumnInt64(object stmt, int index);
        double ColumnDouble(object stmt, int index);
        byte[] ColumnByteArray(object stmt, int index);
        void Reset(object stmt);
    }
}