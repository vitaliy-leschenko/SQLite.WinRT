// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SQLite.WinRT.Linq.Base;

namespace SQLite.WinRT.Linq.Common
{
    public class FieldReader
    {
        private static Dictionary<Type, MethodInfo> readerMethods;
        private readonly ISQLiteProvider provider;
        private readonly object stmt;

        public FieldReader(object stmt)
        {
            this.stmt = stmt;
            provider = Platform.Current.SQLiteProvider;
        }

        public Byte ReadByte(int ordinal)
        {
            return (byte) provider.ColumnInt(stmt, ordinal);
        }

        public Byte? ReadNullableByte(int ordinal)
        {
            ColType type = provider.ColumnType(stmt, ordinal);
            if (type == ColType.Null) return null;

            return (byte) provider.ColumnInt(stmt, ordinal);
        }

        public bool ReadBoolean(int ordinal)
        {
            return provider.ColumnInt(stmt, ordinal) != 0;
        }

        public bool? ReadNullableBoolean(int ordinal)
        {
            var type = provider.ColumnType(stmt, ordinal);
            if (type == ColType.Null) return null;

            return provider.ColumnInt(stmt, ordinal) != 0;
        }

        public Char ReadChar(int ordinal)
        {
            return provider.ColumnString(stmt, ordinal)[0];
        }

        public Char? ReadNullableChar(int ordinal)
        {
            ColType type = provider.ColumnType(stmt, ordinal);
            if (type == ColType.Null) return null;

            return provider.ColumnString(stmt, ordinal)[0];
        }

        public DateTime ReadDateTime(int ordinal)
        {
            ColType type = provider.ColumnType(stmt, ordinal);
            switch (type)
            {
                case ColType.Integer:
                    long ticks = provider.ColumnInt64(stmt, ordinal);
                    return new DateTime(ticks);
                case ColType.Text:
                    string text = provider.ColumnString(stmt, ordinal);
                    return DateTime.Parse(text);
                default:
                    return DateTime.MinValue;
            }
        }

        public DateTime? ReadNullableDateTime(int ordinal)
        {
            ColType type = provider.ColumnType(stmt, ordinal);
            switch (type)
            {
                case ColType.Integer:
                    long ticks = provider.ColumnInt64(stmt, ordinal);
                    return new DateTime(ticks);
                case ColType.Text:
                    string text = provider.ColumnString(stmt, ordinal);
                    return DateTime.Parse(text);
                default:
                    return null;
            }
        }

        public Decimal ReadDecimal(int ordinal)
        {
            return (decimal) provider.ColumnDouble(stmt, ordinal);
        }

        public Decimal? ReadNullableDecimal(int ordinal)
        {
            ColType type = provider.ColumnType(stmt, ordinal);
            if (type == ColType.Null) return null;

            return (decimal) provider.ColumnDouble(stmt, ordinal);
        }

        public Double ReadDouble(int ordinal)
        {
            return provider.ColumnDouble(stmt, ordinal);
        }

        public Double? ReadNullableDouble(int ordinal)
        {
            ColType type = provider.ColumnType(stmt, ordinal);
            if (type == ColType.Null) return null;

            return provider.ColumnDouble(stmt, ordinal);
        }

        public Single ReadSingle(int ordinal)
        {
            return (float) provider.ColumnDouble(stmt, ordinal);
        }

        public Single? ReadNullableSingle(int ordinal)
        {
            ColType type = provider.ColumnType(stmt, ordinal);
            if (type == ColType.Null) return null;

            return (float) provider.ColumnDouble(stmt, ordinal);
        }

        public Guid ReadGuid(int ordinal)
        {
            return Guid.Parse(provider.ColumnString(stmt, ordinal));
        }

        public Guid? ReadNullableGuid(int ordinal)
        {
            ColType type = provider.ColumnType(stmt, ordinal);
            if (type == ColType.Null) return null;

            return Guid.Parse(provider.ColumnString(stmt, ordinal));
        }

        public Int16 ReadInt16(int ordinal)
        {
            return (short) provider.ColumnInt(stmt, ordinal);
        }

        public Int16? ReadNullableInt16(int ordinal)
        {
            ColType type = provider.ColumnType(stmt, ordinal);
            if (type == ColType.Null) return null;

            return (short) provider.ColumnInt(stmt, ordinal);
        }

        public Int32 ReadInt32(int ordinal)
        {
            return provider.ColumnInt(stmt, ordinal);
        }

        public Int32? ReadNullableInt32(int ordinal)
        {
            ColType type = provider.ColumnType(stmt, ordinal);
            if (type == ColType.Null) return null;

            return provider.ColumnInt(stmt, ordinal);
        }

        public Int64 ReadInt64(int ordinal)
        {
            return provider.ColumnInt64(stmt, ordinal);
        }

        public Int64? ReadNullableInt64(int ordinal)
        {
            ColType type = provider.ColumnType(stmt, ordinal);
            if (type == ColType.Null) return null;
            return provider.ColumnInt64(stmt, ordinal);
        }

        public String ReadString(int ordinal)
        {
            return provider.ColumnString(stmt, ordinal);
        }

        public Byte[] ReadByteArray(int ordinal)
        {
            return provider.ColumnByteArray(stmt, ordinal);
        }

        public static MethodInfo GetReaderMethod(Type type)
        {
            if (readerMethods == null)
            {
                List<MethodInfo> meths =
                    typeof (FieldReader).GetMethods().Where(m => m.Name.StartsWith("Read")).ToList();
                readerMethods = meths.ToDictionary(m => m.ReturnType);
            }

            MethodInfo mi;
            if (!readerMethods.TryGetValue(type, out mi))
            {
                throw new NotSupportedException();
            }
            return mi;
        }
    }
}