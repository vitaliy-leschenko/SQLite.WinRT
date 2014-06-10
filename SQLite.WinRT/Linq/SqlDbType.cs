namespace SQLite.WinRT.Linq
{
    /// <summary>
    ///     Specifies SQL Server-specific data type of a field, property, for use in a
    ///     <see cref="T:System.Data.SqlClient.SqlParameter" />.
    /// </summary>
    /// <filterpriority>2</filterpriority>
    public enum SqlDbType
    {
        BigInt = 0,
        Binary = 1,
        Bit = 2,
        Char = 3,
        DateTime = 4,
        Decimal = 5,
        Float = 6,
        Image = 7,
        Int = 8,
        Money = 9,
        NChar = 10,
        NText = 11,
        NVarChar = 12,
        Real = 13,
        UniqueIdentifier = 14,
        SmallDateTime = 15,
        SmallInt = 16,
        SmallMoney = 17,
        Text = 18,
        Timestamp = 19,
        TinyInt = 20,
        VarBinary = 21,
        VarChar = 22,
        Variant = 23,
        Xml = 25,
        Udt = 29,
        Structured = 30,
        Date = 31,
        Time = 32,
        DateTime2 = 33,
        DateTimeOffset = 34,
    }

    public enum DbType
    {
        AnsiString = 0,
        Binary = 1,
        Byte = 2,
        Boolean = 3,
        Currency = 4,
        Date = 5,
        DateTime = 6,
        Decimal = 7,
        Double = 8,
        Guid = 9,
        Int16 = 10,
        Int32 = 11,
        Int64 = 12,
        Object = 13,
        SByte = 14,
        Single = 15,
        String = 16,
        Time = 17,
        UInt16 = 18,
        UInt32 = 19,
        UInt64 = 20,
        VarNumeric = 21,
        AnsiStringFixedLength = 22,
        StringFixedLength = 23,
        Xml = 25,
        DateTime2 = 26,
        DateTimeOffset = 27,
    }
}