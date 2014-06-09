// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Reflection;
using SQLite.WinRT.Linq.Base;

namespace SQLite.WinRT.Linq
{
	public static class DbTypeSystem
	{
		public static DbQueryType Parse(string typeDeclaration)
		{
			string[] args = null;
			string typeName = null;
			string remainder = null;
			int openParen = typeDeclaration.IndexOf('(');
			if (openParen >= 0)
			{
				typeName = typeDeclaration.Substring(0, openParen).Trim();

				int closeParen = typeDeclaration.IndexOf(')', openParen);
				if (closeParen < openParen)
				{
					closeParen = typeDeclaration.Length;
				}

				string argstr = typeDeclaration.Substring(openParen + 1, closeParen - (openParen + 1));
				args = argstr.Split(',');
				remainder = typeDeclaration.Substring(closeParen + 1);
			}
			else
			{
				int space = typeDeclaration.IndexOf(' ');
				if (space >= 0)
				{
					typeName = typeDeclaration.Substring(0, space);
					remainder = typeDeclaration.Substring(space + 1).Trim();
				}
				else
				{
					typeName = typeDeclaration;
				}
			}

			bool isNotNull = (remainder != null) ? remainder.ToUpper().Contains("NOT NULL") : false;

			return GetQueryType(typeName, args, isNotNull);
		}

		public static DbQueryType GetQueryType(string typeName, string[] args, bool isNotNull)
		{
			if (String.Compare(typeName, "rowversion", StringComparison.OrdinalIgnoreCase) == 0)
			{
				typeName = "Timestamp";
			}

			if (String.Compare(typeName, "numeric", StringComparison.OrdinalIgnoreCase) == 0)
			{
				typeName = "Decimal";
			}

			if (String.Compare(typeName, "sql_variant", StringComparison.OrdinalIgnoreCase) == 0)
			{
				typeName = "Variant";
			}

			SqlDbType dbType = GetSqlType(typeName);

			int length = 0;
			short precision = 0;
			short scale = 0;

			switch (dbType)
			{
				case SqlDbType.Binary:
				case SqlDbType.Char:
				case SqlDbType.Image:
				case SqlDbType.NChar:
				case SqlDbType.NVarChar:
				case SqlDbType.VarBinary:
				case SqlDbType.VarChar:
					if (args == null || args.Length < 1)
					{
						length = 80;
					}
					else if (string.Compare(args[0], "max", StringComparison.CurrentCultureIgnoreCase) == 0)
					{
						length = Int32.MaxValue;
					}
					else
					{
						length = Int32.Parse(args[0]);
					}
					break;
				case SqlDbType.Money:
					if (args == null || args.Length < 1)
					{
						precision = 29;
					}
					else
					{
						precision = Int16.Parse(args[0]);
					}
					if (args == null || args.Length < 2)
					{
						scale = 4;
					}
					else
					{
						scale = Int16.Parse(args[1]);
					}
					break;
				case SqlDbType.Decimal:
					if (args == null || args.Length < 1)
					{
						precision = 29;
					}
					else
					{
						precision = Int16.Parse(args[0]);
					}
					if (args == null || args.Length < 2)
					{
						scale = 0;
					}
					else
					{
						scale = Int16.Parse(args[1]);
					}
					break;
				case SqlDbType.Float:
				case SqlDbType.Real:
					if (args == null || args.Length < 1)
					{
						precision = 29;
					}
					else
					{
						precision = Int16.Parse(args[0]);
					}
					break;
			}

			return NewType(dbType, isNotNull, length, precision, scale);
		}

		public static DbQueryType NewType(SqlDbType type, bool isNotNull, int length, short precision, short scale)
		{
			return new DbQueryType(type, length, scale);
		}

		public static SqlDbType GetSqlType(string typeName)
		{
            if (string.Compare(typeName, "TEXT", StringComparison.CurrentCultureIgnoreCase) == 0 || string.Compare(typeName, "CHAR", StringComparison.CurrentCultureIgnoreCase) == 0
                || string.Compare(typeName, "CLOB", StringComparison.CurrentCultureIgnoreCase) == 0 || string.Compare(typeName, "VARYINGCHARACTER", StringComparison.CurrentCultureIgnoreCase) == 0
                || string.Compare(typeName, "NATIONALVARYINGCHARACTER", StringComparison.CurrentCultureIgnoreCase) == 0)
			{
				return SqlDbType.VarChar;
			}
            else if (string.Compare(typeName, "INT", StringComparison.CurrentCultureIgnoreCase) == 0 || string.Compare(typeName, "INTEGER", StringComparison.CurrentCultureIgnoreCase) == 0)
			{
				return SqlDbType.BigInt;
			}
            else if (string.Compare(typeName, "BLOB", StringComparison.CurrentCultureIgnoreCase) == 0)
			{
				return SqlDbType.Binary;
			}
            else if (string.Compare(typeName, "BOOLEAN", StringComparison.CurrentCultureIgnoreCase) == 0)
			{
				return SqlDbType.Bit;
			}
			else if (string.Compare(typeName, "NUMERIC", StringComparison.CurrentCultureIgnoreCase) == 0)
			{
				return SqlDbType.Decimal;
			}
			else
			{
				return (SqlDbType)Enum.Parse(typeof(SqlDbType), typeName, true);
			}
		}

		public const int StringDefaultSize = Int32.MaxValue;

		public const int BinaryDefaultSize = Int32.MaxValue;

		public static DbQueryType GetColumnType(Type type)
		{
			bool isNotNull = type.GetTypeInfo().IsValueType && !TypeHelper.IsNullableType(type);
			type = TypeHelper.GetNonNullableType(type);
			switch (TypeHelper.GetTypeCode(type))
			{
				case TypeCode.Boolean:
					return NewType(SqlDbType.Bit, isNotNull, 0, 0, 0);
				case TypeCode.SByte:
				case TypeCode.Byte:
					return NewType(SqlDbType.TinyInt, isNotNull, 0, 0, 0);
				case TypeCode.Int16:
				case TypeCode.UInt16:
					return NewType(SqlDbType.SmallInt, isNotNull, 0, 0, 0);
				case TypeCode.Int32:
				case TypeCode.UInt32:
					return NewType(SqlDbType.Int, isNotNull, 0, 0, 0);
				case TypeCode.Int64:
				case TypeCode.UInt64:
					return NewType(SqlDbType.BigInt, isNotNull, 0, 0, 0);
				case TypeCode.Single:
				case TypeCode.Double:
					return NewType(SqlDbType.Float, isNotNull, 0, 0, 0);
				case TypeCode.String:
					return NewType(SqlDbType.NVarChar, isNotNull, StringDefaultSize, 0, 0);
				case TypeCode.Char:
					return NewType(SqlDbType.NChar, isNotNull, 1, 0, 0);
				case TypeCode.DateTime:
					return NewType(SqlDbType.DateTime, isNotNull, 0, 0, 0);
				case TypeCode.Decimal:
					return NewType(SqlDbType.Decimal, isNotNull, 0, 29, 4);
				default:
					if (type == typeof(byte[]))
					{
						return NewType(SqlDbType.VarBinary, isNotNull, BinaryDefaultSize, 0, 0);
					}
					else if (type == typeof(Guid))
					{
						return NewType(SqlDbType.UniqueIdentifier, isNotNull, 0, 0, 0);
					}
					else if (type == typeof(DateTimeOffset))
					{
						return NewType(SqlDbType.DateTimeOffset, isNotNull, 0, 0, 0);
					}
					else if (type == typeof(TimeSpan))
					{
						return NewType(SqlDbType.Time, isNotNull, 0, 0, 0);
					}
					return null;
			}
		}

		public static DbType GetDbType(SqlDbType dbType)
		{
			switch (dbType)
			{
				case SqlDbType.BigInt:
					return DbType.Int64;
				case SqlDbType.Binary:
					return DbType.Binary;
				case SqlDbType.Bit:
					return DbType.Boolean;
				case SqlDbType.Char:
					return DbType.AnsiStringFixedLength;
				case SqlDbType.Date:
					return DbType.Date;
				case SqlDbType.DateTime:
				case SqlDbType.SmallDateTime:
					return DbType.DateTime;
				case SqlDbType.DateTime2:
					return DbType.DateTime2;
				case SqlDbType.DateTimeOffset:
					return DbType.DateTimeOffset;
				case SqlDbType.Decimal:
					return DbType.Decimal;
				case SqlDbType.Float:
				case SqlDbType.Real:
					return DbType.Double;
				case SqlDbType.Image:
					return DbType.Binary;
				case SqlDbType.Int:
					return DbType.Int32;
				case SqlDbType.Money:
				case SqlDbType.SmallMoney:
					return DbType.Currency;
				case SqlDbType.NChar:
					return DbType.StringFixedLength;
				case SqlDbType.NText:
				case SqlDbType.NVarChar:
					return DbType.String;
				case SqlDbType.SmallInt:
					return DbType.Int16;
				case SqlDbType.Text:
					return DbType.AnsiString;
				case SqlDbType.Time:
					return DbType.Time;
				case SqlDbType.Timestamp:
					return DbType.Binary;
				case SqlDbType.TinyInt:
					return DbType.SByte;
				case SqlDbType.Udt:
					return DbType.Object;
				case SqlDbType.UniqueIdentifier:
					return DbType.Guid;
				case SqlDbType.VarBinary:
					return DbType.Binary;
				case SqlDbType.VarChar:
					return DbType.AnsiString;
				case SqlDbType.Variant:
					return DbType.Object;
				case SqlDbType.Xml:
					return DbType.String;
				default:
					throw new InvalidOperationException(string.Format("Unhandled sql type: {0}", dbType));
			}
		}
	}

	public class DbQueryType
	{
		private readonly SqlDbType sqlDbType;

		public DbQueryType(SqlDbType dbType, int length, short scale)
		{
			this.sqlDbType = dbType;
			this.Length = length;
			this.Scale = scale;
		}

		public DbType DbType
		{
			get
			{
				return DbTypeSystem.GetDbType(this.sqlDbType);
			}
		}

		public int Length { get; private set; }

		public short Scale { get; private set; }
	}
}