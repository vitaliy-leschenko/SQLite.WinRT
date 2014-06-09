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
	    private readonly object stmt;
	    //private TypeCode[] typeCodes;

        //private readonly EntityProvider.Executor executor;

        //public FieldReader(EntityProvider.Executor executor, object reader)
        //{
        //    this.executor = executor;
        //    this.reader = reader;
        //    this.Init();
        //}

        //protected void Init()
        //{
        //    this.typeCodes = new TypeCode[this.FieldCount];
        //}

        //protected int FieldCount
        //{
        //    get
        //    {
        //        return this.reader.FieldCount;
        //    }
        //}

        //protected Type GetFieldType(int ordinal)
        //{
        //    return this.reader.GetFieldType(ordinal);
        //}

        protected bool IsDBNull(int ordinal)
        {
            throw new NotImplementedException();
            //return this.reader.IsDBNull(ordinal);
        }

        //protected T GetValue<T>(int ordinal)
        //{
        //    return (T)this.executor.Convert(this.reader.GetValue(ordinal), typeof(T));
        //}

        //protected Byte GetByte(int ordinal)
        //{
        //    return this.reader.GetByte(ordinal);
        //}

        //protected Char GetChar(int ordinal)
        //{
        //    return this.reader.GetChar(ordinal);
        //}

        protected DateTime GetDateTime(int ordinal)
        {
            throw new NotImplementedException();
            //return this.reader.GetDateTime(ordinal);
        }

        //protected Decimal GetDecimal(int ordinal)
        //{
        //    return this.reader.GetDecimal(ordinal);
        //}

        //protected Double GetDouble(int ordinal)
        //{
        //    return this.reader.GetDouble(ordinal);
        //}

        //protected Single GetSingle(int ordinal)
        //{
        //    return this.reader.GetFloat(ordinal);
        //}

        //protected Guid GetGuid(int ordinal)
        //{
        //    return this.reader.GetGuid(ordinal);
        //}

        //protected Int16 GetInt16(int ordinal)
        //{
        //    return this.reader.GetInt16(ordinal);
        //}

        //protected Int32 GetInt32(int ordinal)
        //{
        //    return this.reader.GetInt32(ordinal);
        //}

        //protected Int64 GetInt64(int ordinal)
        //{
        //    return this.reader.GetInt64(ordinal);
        //}

        //protected String GetString(int ordinal)
        //{
        //    return this.reader.GetString(ordinal);
        //}

        public T ReadValue<T>(int ordinal)
        {
            throw new NotImplementedException();
            //if (this.IsDBNull(ordinal))
            //{
            //    return default(T);
            //}
            //return this.GetValue<T>(ordinal);
        }

        public T? ReadNullableValue<T>(int ordinal) where T : struct
        {
            throw new NotImplementedException();
            //if (this.IsDBNull(ordinal))
            //{
            //    return default(T?);
            //}
            //return this.GetValue<T>(ordinal);
        }

        //public Byte ReadByte(int ordinal)
        //{
        //    if (this.IsDBNull(ordinal))
        //    {
        //        return default(Byte);
        //    }
        //    while (true)
        //    {
        //        switch (typeCodes[ordinal])
        //        {
        //            case TypeCode.Empty:
        //                typeCodes[ordinal] = GetTypeCode(ordinal);
        //                continue;
        //            case TypeCode.Byte:
        //                return this.GetByte(ordinal);
        //            case TypeCode.Int16:
        //                return (Byte)this.GetInt16(ordinal);
        //            case TypeCode.Int32:
        //                return (Byte)this.GetInt32(ordinal);
        //            case TypeCode.Int64:
        //                return (Byte)this.GetInt64(ordinal);
        //            case TypeCode.Double:
        //                return (Byte)this.GetDouble(ordinal);
        //            case TypeCode.Single:
        //                return (Byte)this.GetSingle(ordinal);
        //            case TypeCode.Decimal:
        //                return (Byte)this.GetDecimal(ordinal);
        //            default:
        //                return this.GetValue<Byte>(ordinal);
        //        }
        //    }
        //}

        //public Byte? ReadNullableByte(int ordinal)
        //{
        //    if (this.IsDBNull(ordinal))
        //    {
        //        return default(Byte?);
        //    }
        //    while (true)
        //    {
        //        switch (typeCodes[ordinal])
        //        {
        //            case TypeCode.Empty:
        //                typeCodes[ordinal] = GetTypeCode(ordinal);
        //                continue;
        //            case TypeCode.Byte:
        //                return this.GetByte(ordinal);
        //            case TypeCode.Int16:
        //                return (Byte)this.GetInt16(ordinal);
        //            case TypeCode.Int32:
        //                return (Byte)this.GetInt32(ordinal);
        //            case TypeCode.Int64:
        //                return (Byte)this.GetInt64(ordinal);
        //            case TypeCode.Double:
        //                return (Byte)this.GetDouble(ordinal);
        //            case TypeCode.Single:
        //                return (Byte)this.GetSingle(ordinal);
        //            case TypeCode.Decimal:
        //                return (Byte)this.GetDecimal(ordinal);
        //            default:
        //                return (Byte)this.GetValue<Byte>(ordinal);
        //        }
        //    }
        //}

        //public Char ReadChar(int ordinal)
        //{
        //    if (this.IsDBNull(ordinal))
        //    {
        //        return default(Char);
        //    }
        //    while (true)
        //    {
        //        switch (typeCodes[ordinal])
        //        {
        //            case TypeCode.Empty:
        //                typeCodes[ordinal] = GetTypeCode(ordinal);
        //                continue;
        //            case TypeCode.Byte:
        //                return (Char)this.GetByte(ordinal);
        //            case TypeCode.Int16:
        //                return (Char)this.GetInt16(ordinal);
        //            case TypeCode.Int32:
        //                return (Char)this.GetInt32(ordinal);
        //            case TypeCode.Int64:
        //                return (Char)this.GetInt64(ordinal);
        //            case TypeCode.Double:
        //                return (Char)this.GetDouble(ordinal);
        //            case TypeCode.Single:
        //                return (Char)this.GetSingle(ordinal);
        //            case TypeCode.Decimal:
        //                return (Char)this.GetDecimal(ordinal);
        //            default:
        //                return this.GetValue<Char>(ordinal);
        //        }
        //    }
        //}

        //public Char? ReadNullableChar(int ordinal)
        //{
        //    if (this.IsDBNull(ordinal))
        //    {
        //        return default(Char?);
        //    }
        //    while (true)
        //    {
        //        switch (typeCodes[ordinal])
        //        {
        //            case TypeCode.Empty:
        //                typeCodes[ordinal] = GetTypeCode(ordinal);
        //                continue;
        //            case TypeCode.Byte:
        //                return (Char)this.GetByte(ordinal);
        //            case TypeCode.Int16:
        //                return (Char)this.GetInt16(ordinal);
        //            case TypeCode.Int32:
        //                return (Char)this.GetInt32(ordinal);
        //            case TypeCode.Int64:
        //                return (Char)this.GetInt64(ordinal);
        //            case TypeCode.Double:
        //                return (Char)this.GetDouble(ordinal);
        //            case TypeCode.Single:
        //                return (Char)this.GetSingle(ordinal);
        //            case TypeCode.Decimal:
        //                return (Char)this.GetDecimal(ordinal);
        //            default:
        //                return this.GetValue<Char>(ordinal);
        //        }
        //    }
        //}

        public DateTime ReadDateTime(int ordinal)
        {
            throw new NotImplementedException();
            //if (this.IsDBNull(ordinal))
            //{
            //    return default(DateTime);
            //}
            //while (true)
            //{
            //    switch (typeCodes[ordinal])
            //    {
            //        case TypeCode.Empty:
            //            typeCodes[ordinal] = GetTypeCode(ordinal);
            //            continue;
            //        case TypeCode.DateTime:
            //            return this.GetDateTime(ordinal);
            //        default:
            //            return this.GetValue<DateTime>(ordinal);
            //    }
            //}
        }

        //public DateTime? ReadNullableDateTime(int ordinal)
        //{
        //    if (this.IsDBNull(ordinal))
        //    {
        //        return default(DateTime?);
        //    }
        //    while (true)
        //    {
        //        switch (typeCodes[ordinal])
        //        {
        //            case TypeCode.Empty:
        //                typeCodes[ordinal] = GetTypeCode(ordinal);
        //                continue;
        //            case TypeCode.DateTime:
        //                return this.GetDateTime(ordinal);
        //            default:
        //                return this.GetValue<DateTime>(ordinal);
        //        }
        //    }
        //}

        //public Decimal ReadDecimal(int ordinal)
        //{
        //    if (this.IsDBNull(ordinal))
        //    {
        //        return default(Decimal);
        //    }
        //    while (true)
        //    {
        //        switch (typeCodes[ordinal])
        //        {
        //            case TypeCode.Empty:
        //                typeCodes[ordinal] = GetTypeCode(ordinal);
        //                continue;
        //            case TypeCode.Byte:
        //                return (Decimal)this.GetByte(ordinal);
        //            case TypeCode.Int16:
        //                return (Decimal)this.GetInt16(ordinal);
        //            case TypeCode.Int32:
        //                return (Decimal)this.GetInt32(ordinal);
        //            case TypeCode.Int64:
        //                return (Decimal)this.GetInt64(ordinal);
        //            case TypeCode.Double:
        //                return (Decimal)this.GetDouble(ordinal);
        //            case TypeCode.Single:
        //                return (Decimal)this.GetSingle(ordinal);
        //            case TypeCode.Decimal:
        //                return this.GetDecimal(ordinal);
        //            default:
        //                return this.GetValue<Decimal>(ordinal);
        //        }
        //    }
        //}

        //public Decimal? ReadNullableDecimal(int ordinal)
        //{
        //    if (this.IsDBNull(ordinal))
        //    {
        //        return default(Decimal?);
        //    }
        //    while (true)
        //    {
        //        switch (typeCodes[ordinal])
        //        {
        //            case TypeCode.Empty:
        //                typeCodes[ordinal] = GetTypeCode(ordinal);
        //                continue;
        //            case TypeCode.Byte:
        //                return (Decimal)this.GetByte(ordinal);
        //            case TypeCode.Int16:
        //                return (Decimal)this.GetInt16(ordinal);
        //            case TypeCode.Int32:
        //                return (Decimal)this.GetInt32(ordinal);
        //            case TypeCode.Int64:
        //                return (Decimal)this.GetInt64(ordinal);
        //            case TypeCode.Double:
        //                return (Decimal)this.GetDouble(ordinal);
        //            case TypeCode.Single:
        //                return (Decimal)this.GetSingle(ordinal);
        //            case TypeCode.Decimal:
        //                return this.GetDecimal(ordinal);
        //            default:
        //                return this.GetValue<Decimal>(ordinal);
        //        }
        //    }
        //}

        //public Double ReadDouble(int ordinal)
        //{
        //    if (this.IsDBNull(ordinal))
        //    {
        //        return default(Double);
        //    }
        //    while (true)
        //    {
        //        switch (typeCodes[ordinal])
        //        {
        //            case TypeCode.Empty:
        //                typeCodes[ordinal] = GetTypeCode(ordinal);
        //                continue;
        //            case TypeCode.Byte:
        //                return (Double)this.GetByte(ordinal);
        //            case TypeCode.Int16:
        //                return (Double)this.GetInt16(ordinal);
        //            case TypeCode.Int32:
        //                return (Double)this.GetInt32(ordinal);
        //            case TypeCode.Int64:
        //                return (Double)this.GetInt64(ordinal);
        //            case TypeCode.Double:
        //                return this.GetDouble(ordinal);
        //            case TypeCode.Single:
        //                return (Double)this.GetSingle(ordinal);
        //            case TypeCode.Decimal:
        //                return (Double)this.GetDecimal(ordinal);
        //            default:
        //                return this.GetValue<Double>(ordinal);
        //        }
        //    }
        //}

        //public Double? ReadNullableDouble(int ordinal)
        //{
        //    if (this.IsDBNull(ordinal))
        //    {
        //        return default(Double?);
        //    }
        //    while (true)
        //    {
        //        switch (typeCodes[ordinal])
        //        {
        //            case TypeCode.Empty:
        //                typeCodes[ordinal] = GetTypeCode(ordinal);
        //                continue;
        //            case TypeCode.Byte:
        //                return (Double)this.GetByte(ordinal);
        //            case TypeCode.Int16:
        //                return (Double)this.GetInt16(ordinal);
        //            case TypeCode.Int32:
        //                return (Double)this.GetInt32(ordinal);
        //            case TypeCode.Int64:
        //                return (Double)this.GetInt64(ordinal);
        //            case TypeCode.Double:
        //                return this.GetDouble(ordinal);
        //            case TypeCode.Single:
        //                return (Double)this.GetSingle(ordinal);
        //            case TypeCode.Decimal:
        //                return (Double)this.GetDecimal(ordinal);
        //            default:
        //                return this.GetValue<Double>(ordinal);
        //        }
        //    }
        //}

        //public Single ReadSingle(int ordinal)
        //{
        //    if (this.IsDBNull(ordinal))
        //    {
        //        return default(Single);
        //    }
        //    while (true)
        //    {
        //        switch (typeCodes[ordinal])
        //        {
        //            case TypeCode.Empty:
        //                typeCodes[ordinal] = GetTypeCode(ordinal);
        //                continue;
        //            case TypeCode.Byte:
        //                return (Single)this.GetByte(ordinal);
        //            case TypeCode.Int16:
        //                return (Single)this.GetInt16(ordinal);
        //            case TypeCode.Int32:
        //                return (Single)this.GetInt32(ordinal);
        //            case TypeCode.Int64:
        //                return (Single)this.GetInt64(ordinal);
        //            case TypeCode.Double:
        //                return (Single)this.GetDouble(ordinal);
        //            case TypeCode.Single:
        //                return this.GetSingle(ordinal);
        //            case TypeCode.Decimal:
        //                return (Single)this.GetDecimal(ordinal);
        //            default:
        //                return this.GetValue<Single>(ordinal);
        //        }
        //    }
        //}

        //public Single? ReadNullableSingle(int ordinal)
        //{
        //    if (this.IsDBNull(ordinal))
        //    {
        //        return default(Single?);
        //    }
        //    while (true)
        //    {
        //        switch (typeCodes[ordinal])
        //        {
        //            case TypeCode.Empty:
        //                typeCodes[ordinal] = GetTypeCode(ordinal);
        //                continue;
        //            case TypeCode.Byte:
        //                return (Single)this.GetByte(ordinal);
        //            case TypeCode.Int16:
        //                return (Single)this.GetInt16(ordinal);
        //            case TypeCode.Int32:
        //                return (Single)this.GetInt32(ordinal);
        //            case TypeCode.Int64:
        //                return (Single)this.GetInt64(ordinal);
        //            case TypeCode.Double:
        //                return (Single)this.GetDouble(ordinal);
        //            case TypeCode.Single:
        //                return this.GetSingle(ordinal);
        //            case TypeCode.Decimal:
        //                return (Single)this.GetDecimal(ordinal);
        //            default:
        //                return this.GetValue<Single>(ordinal);
        //        }
        //    }
        //}

        //public Guid ReadGuid(int ordinal)
        //{
        //    if (this.IsDBNull(ordinal))
        //    {
        //        return default(Guid);
        //    }
        //    while (true)
        //    {
        //        switch (typeCodes[ordinal])
        //        {
        //            case TypeCode.Empty:
        //                typeCodes[ordinal] = GetTypeCode(ordinal);
        //                continue;
        //            case tcGuid:
        //                return this.GetGuid(ordinal);
        //            default:
        //                return this.GetValue<Guid>(ordinal);
        //        }
        //    }
        //}

        //public Guid? ReadNullableGuid(int ordinal)
        //{
        //    if (this.IsDBNull(ordinal))
        //    {
        //        return default(Guid?);
        //    }
        //    while (true)
        //    {
        //        switch (typeCodes[ordinal])
        //        {
        //            case TypeCode.Empty:
        //                typeCodes[ordinal] = GetTypeCode(ordinal);
        //                continue;
        //            case tcGuid:
        //                return this.GetGuid(ordinal);
        //            default:
        //                return this.GetValue<Guid>(ordinal);
        //        }
        //    }
        //}

        //public Int16 ReadInt16(int ordinal)
        //{
        //    if (this.IsDBNull(ordinal))
        //    {
        //        return default(Int16);
        //    }
        //    while (true)
        //    {
        //        switch (typeCodes[ordinal])
        //        {
        //            case TypeCode.Empty:
        //                typeCodes[ordinal] = GetTypeCode(ordinal);
        //                continue;
        //            case TypeCode.Byte:
        //                return (Int16)this.GetByte(ordinal);
        //            case TypeCode.Int16:
        //                return (Int16)this.GetInt16(ordinal);
        //            case TypeCode.Int32:
        //                return (Int16)this.GetInt32(ordinal);
        //            case TypeCode.Int64:
        //                return (Int16)this.GetInt64(ordinal);
        //            case TypeCode.Double:
        //                return (Int16)this.GetDouble(ordinal);
        //            case TypeCode.Single:
        //                return (Int16)this.GetSingle(ordinal);
        //            case TypeCode.Decimal:
        //                return (Int16)this.GetDecimal(ordinal);
        //            default:
        //                return this.GetValue<Int16>(ordinal);
        //        }
        //    }
        //}

        //public Int16? ReadNullableInt16(int ordinal)
        //{
        //    if (this.IsDBNull(ordinal))
        //    {
        //        return default(Int16?);
        //    }
        //    while (true)
        //    {
        //        switch (typeCodes[ordinal])
        //        {
        //            case TypeCode.Empty:
        //                typeCodes[ordinal] = GetTypeCode(ordinal);
        //                continue;
        //            case TypeCode.Byte:
        //                return (Int16)this.GetByte(ordinal);
        //            case TypeCode.Int16:
        //                return (Int16)this.GetInt16(ordinal);
        //            case TypeCode.Int32:
        //                return (Int16)this.GetInt32(ordinal);
        //            case TypeCode.Int64:
        //                return (Int16)this.GetInt64(ordinal);
        //            case TypeCode.Double:
        //                return (Int16)this.GetDouble(ordinal);
        //            case TypeCode.Single:
        //                return (Int16)this.GetSingle(ordinal);
        //            case TypeCode.Decimal:
        //                return (Int16)this.GetDecimal(ordinal);
        //            default:
        //                return this.GetValue<Int16>(ordinal);
        //        }
        //    }
        //}

        public Int32 ReadInt32(int ordinal)
        {
            return Platform.Current.SQLiteProvider.ColumnInt(stmt, ordinal);
        }

        //public Int32? ReadNullableInt32(int ordinal)
        //{
        //    if (this.IsDBNull(ordinal))
        //    {
        //        return default(Int32?);
        //    }
        //    while (true)
        //    {
        //        switch (typeCodes[ordinal])
        //        {
        //            case TypeCode.Empty:
        //                typeCodes[ordinal] = GetTypeCode(ordinal);
        //                continue;
        //            case TypeCode.Byte:
        //                return (Int32)this.GetByte(ordinal);
        //            case TypeCode.Int16:
        //                return (Int32)this.GetInt16(ordinal);
        //            case TypeCode.Int32:
        //                return (Int32)this.GetInt32(ordinal);
        //            case TypeCode.Int64:
        //                return (Int32)this.GetInt64(ordinal);
        //            case TypeCode.Double:
        //                return (Int32)this.GetDouble(ordinal);
        //            case TypeCode.Single:
        //                return (Int32)this.GetSingle(ordinal);
        //            case TypeCode.Decimal:
        //                return (Int32)this.GetDecimal(ordinal);
        //            default:
        //                return this.GetValue<Int32>(ordinal);
        //        }
        //    }
        //}

        public Int64 ReadInt64(int ordinal)
        {
            return Platform.Current.SQLiteProvider.ColumnInt64(stmt, ordinal);
        }

        //public Int64? ReadNullableInt64(int ordinal)
        //{
        //    if (this.IsDBNull(ordinal))
        //    {
        //        return default(Int64?);
        //    }
        //    while (true)
        //    {
        //        switch (typeCodes[ordinal])
        //        {
        //            case TypeCode.Empty:
        //                typeCodes[ordinal] = GetTypeCode(ordinal);
        //                continue;
        //            case TypeCode.Byte:
        //                return (Int64)this.GetByte(ordinal);
        //            case TypeCode.Int16:
        //                return (Int64)this.GetInt16(ordinal);
        //            case TypeCode.Int32:
        //                return (Int64)this.GetInt32(ordinal);
        //            case TypeCode.Int64:
        //                return (Int64)this.GetInt64(ordinal);
        //            case TypeCode.Double:
        //                return (Int64)this.GetDouble(ordinal);
        //            case TypeCode.Single:
        //                return (Int64)this.GetSingle(ordinal);
        //            case TypeCode.Decimal:
        //                return (Int64)this.GetDecimal(ordinal);
        //            default:
        //                return this.GetValue<Int64>(ordinal);
        //        }
        //    }
        //}

        public String ReadString(int ordinal)
        {
            return Platform.Current.SQLiteProvider.ColumnString(stmt, ordinal);
        }

        //public Byte[] ReadByteArray(int ordinal)
        //{
        //    if (this.IsDBNull(ordinal))
        //    {
        //        return default(Byte[]);
        //    }
        //    while (true)
        //    {
        //        switch (typeCodes[ordinal])
        //        {
        //            case TypeCode.Empty:
        //                typeCodes[ordinal] = GetTypeCode(ordinal);
        //                continue;
        //            case TypeCode.Byte:
        //                return new Byte[] { this.GetByte(ordinal) };
        //            default:
        //                return this.GetValue<Byte[]>(ordinal);
        //        }
        //    }
        //}

        //public Char[] ReadCharArray(int ordinal)
        //{
        //    if (this.IsDBNull(ordinal))
        //    {
        //        return default(Char[]);
        //    }
        //    while (true)
        //    {
        //        switch (typeCodes[ordinal])
        //        {
        //            case TypeCode.Empty:
        //                typeCodes[ordinal] = GetTypeCode(ordinal);
        //                continue;
        //            case TypeCode.Char:
        //                return new Char[] { this.GetChar(ordinal) };
        //            default:
        //                return this.GetValue<Char[]>(ordinal);
        //        }
        //    }
        //}

        //private const TypeCode tcGuid = (TypeCode)20;

        //private const TypeCode tcByteArray = (TypeCode)21;

        //private const TypeCode tcCharArray = (TypeCode)22;

        //private TypeCode GetTypeCode(int ordinal)
        //{
        //    Type type = this.GetFieldType(ordinal);
        //    TypeCode tc = Type.GetTypeCode(type);
        //    if (tc == TypeCode.Object)
        //    {
        //        if (type == typeof(Guid))
        //        {
        //            tc = tcGuid;
        //        }
        //        else if (type == typeof(Byte[]))
        //        {
        //            tc = tcByteArray;
        //        }
        //        else if (type == typeof(Char[]))
        //        {
        //            tc = tcCharArray;
        //        }
        //    }
        //    return tc;
        //}

        public static MethodInfo GetReaderMethod(Type type)
        {
            if (readerMethods == null)
            {
                var meths = typeof(FieldReader).GetMethods().Where(m => m.Name.StartsWith("Read")).ToList();
                readerMethods = meths.ToDictionary(m => m.ReturnType);
                miReadValue = meths.Single(m => m.Name == "ReadValue");
                miReadNullableValue = meths.Single(m => m.Name == "ReadNullableValue");
            }

            MethodInfo mi;
            readerMethods.TryGetValue(type, out mi);
            if (mi == null)
            {
                if (TypeHelper.IsNullableType(type))
                {
                    mi = miReadNullableValue.MakeGenericMethod(TypeHelper.GetNonNullableType(type));
                }
                else
                {
                    mi = miReadValue.MakeGenericMethod(type);
                }
            }
            return mi;
        }

        private static Dictionary<Type, MethodInfo> readerMethods;

        private static MethodInfo miReadValue;

        private static MethodInfo miReadNullableValue;

	    public FieldReader(object stmt)
	    {
	        this.stmt = stmt;
	    }
	}
}