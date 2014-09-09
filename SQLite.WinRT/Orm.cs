using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SQLite.WinRT
{
    public static class Orm
    {
        public const int DefaultMaxStringLength = 140;

        public static string SqlDecl(TableMapping.Column p, bool storeDateTimeAsTicks)
        {
            var decl = "\"" + p.Name + "\" " + SqlType(p, storeDateTimeAsTicks) + " ";
            if (p.IsPK)
            {
                decl += "primary key ";
            }
            if (p.IsAutoInc)
            {
                decl += "autoincrement ";
            }
            if (!p.IsNullable)
            {
                decl += "not null ";
            }
            if (!string.IsNullOrEmpty(p.Collation))
            {
                decl += "collate " + p.Collation + " ";
            }
            return decl;
        }

        public static string SqlType(TableMapping.Column p, bool storeDateTimeAsTicks)
        {
            var clrType = p.ColumnType;
            if (clrType == typeof(Boolean) || clrType == typeof(Byte) || clrType == typeof(UInt16) || clrType == typeof(SByte) || clrType == typeof(Int16) || clrType == typeof(Int32))
            {
                return "integer";
            }
            if (clrType == typeof(UInt32) || clrType == typeof(Int64))
            {
                return "bigint";
            }
            if (clrType == typeof(Single) || clrType == typeof(Double) || clrType == typeof(Decimal))
            {
                return "float";
            }
            if (clrType == typeof(String))
            {
                var len = p.MaxStringLength;
                return "varchar(" + len + ")";
            }
            if (clrType == typeof(DateTime))
            {
                return storeDateTimeAsTicks ? "bigint" : "datetime";
            }
            if (clrType.GetTypeInfo().IsEnum)
            {
                return "integer";
            }
            if (clrType == typeof(byte[]))
            {
                return "blob";
            }
            if (clrType == typeof(Guid))
            {
                return "varchar(36)";
            }
            throw new NotSupportedException("Don't know about " + clrType);
        }

        public static bool IsPK(MemberInfo p)
        {
            var attrs = p.GetCustomAttributes(typeof(PrimaryKeyAttribute), true);
            return attrs.Any();
        }

        public static string Collation(MemberInfo p)
        {
            var attrs = p.GetCustomAttributes(typeof(CollationAttribute), true).ToList();
            if (attrs.Any())
            {
                return ((CollationAttribute)attrs.First()).Value;
            }
            return string.Empty;
        }

        public static bool IsAutoInc(MemberInfo p)
        {
            var attrs = p.GetCustomAttributes(typeof(AutoIncrementAttribute), true);
            return attrs.Any();
        }

        public static IEnumerable<IndexedAttribute> GetIndices(MemberInfo p)
        {
            var attrs = p.GetCustomAttributes(typeof(IndexedAttribute), true);
            return attrs.Cast<IndexedAttribute>();
        }

        public static int MaxStringLength(PropertyInfo p)
        {
            var attr = (MaxLengthAttribute)p.GetCustomAttributes(typeof(MaxLengthAttribute), true).FirstOrDefault();
            if (attr != null)
            {
                return attr.Value;
            }
            return DefaultMaxStringLength;
        }
    }
}