// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SQLite.WinRT.Linq.Base
{
    /// <summary>
    ///     Type related helper methods
    /// </summary>
    public static class TypeHelper
    {
        private static readonly Dictionary<Type, TypeCode> typeMapper = new Dictionary<Type, TypeCode>
        {
            {typeof (bool), TypeCode.Boolean},
            {typeof (char), TypeCode.Char},
            {typeof (sbyte), TypeCode.SByte},
            {typeof (byte), TypeCode.Byte},
            {typeof (short), TypeCode.Int16},
            {typeof (ushort), TypeCode.UInt16},
            {typeof (int), TypeCode.Int32},
            {typeof (uint), TypeCode.UInt32},
            {typeof (long), TypeCode.Int64},
            {typeof (ulong), TypeCode.UInt64},
            {typeof (float), TypeCode.Single},
            {typeof (double), TypeCode.Double},
            {typeof (decimal), TypeCode.Decimal},
            {typeof (DateTime), TypeCode.DateTime},
            {typeof (string), TypeCode.String}
        };

        public static Type FindIEnumerable(Type seqType)
        {
            if (seqType == null || seqType == typeof (string))
            {
                return null;
            }
            if (seqType.IsArray)
            {
                return typeof (IEnumerable<>).MakeGenericType(seqType.GetElementType());
            }
            if (seqType.GetTypeInfo().IsGenericType)
            {
                foreach (Type arg in seqType.GetTypeInfo().GenericTypeArguments)
                {
                    Type ienum = typeof (IEnumerable<>).MakeGenericType(arg);
                    if (ienum.GetTypeInfo().IsAssignableFrom(seqType.GetTypeInfo()))
                    {
                        return ienum;
                    }
                }
            }
            Type[] ifaces = seqType.GetTypeInfo().ImplementedInterfaces.ToArray();
            if (ifaces != null && ifaces.Length > 0)
            {
                foreach (Type iface in ifaces)
                {
                    Type ienum = FindIEnumerable(iface);
                    if (ienum != null)
                    {
                        return ienum;
                    }
                }
            }
            Type baseType = seqType.GetTypeInfo().BaseType;
            if (baseType != null && baseType != typeof (object))
            {
                return FindIEnumerable(baseType);
            }
            return null;
        }

        public static Type GetSequenceType(Type elementType)
        {
            return typeof (IEnumerable<>).MakeGenericType(elementType);
        }

        public static Type GetElementType(Type seqType)
        {
            Type ienum = FindIEnumerable(seqType);
            if (ienum == null)
            {
                return seqType;
            }
            return ienum.GenericTypeArguments[0];
        }

        public static bool IsNullableType(Type type)
        {
            return type != null && type.GetTypeInfo().IsGenericType &&
                   type.GetGenericTypeDefinition() == typeof (Nullable<>);
        }

        public static bool IsNullAssignable(Type type)
        {
            return !type.GetTypeInfo().IsValueType || IsNullableType(type);
        }

        public static Type GetNonNullableType(Type type)
        {
            if (IsNullableType(type))
            {
                return type.GenericTypeArguments[0];
            }
            return type;
        }

        public static Type GetNullAssignableType(Type type)
        {
            if (!IsNullAssignable(type))
            {
                return typeof (Nullable<>).MakeGenericType(type);
            }
            return type;
        }

        public static ConstantExpression GetNullConstant(Type type)
        {
            return Expression.Constant(null, GetNullAssignableType(type));
        }

        public static Type GetMemberType(MemberInfo mi)
        {
            var fi = mi as FieldInfo;
            if (fi != null)
            {
                return fi.FieldType;
            }
            var pi = mi as PropertyInfo;
            if (pi != null)
            {
                return pi.PropertyType;
            }
            var ei = mi as EventInfo;
            if (ei != null)
            {
                return ei.EventHandlerType;
            }
            var meth = mi as MethodInfo; // property getters really
            if (meth != null)
            {
                return meth.ReturnType;
            }
            return null;
        }

        public static object GetDefault(Type type)
        {
            bool isNullable = !type.GetTypeInfo().IsValueType || IsNullableType(type);
            if (!isNullable)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }

        public static bool IsReadOnly(MemberInfo member)
        {
            var pi = member as PropertyInfo;
            if (pi != null) return !pi.CanWrite;

            var f = member as FieldInfo;
            if (f != null)
            {
                return (f.Attributes & FieldAttributes.InitOnly) != 0;
            }

            return true;
        }

        public static bool IsInteger(Type type)
        {
            Type nnType = GetNonNullableType(type);
            switch (GetTypeCode(type))
            {
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
                default:
                    return false;
            }
        }

        public static TypeCode GetTypeCode(Type type)
        {
            if (type == null) return TypeCode.Empty;
            return typeMapper.ContainsKey(type) ? typeMapper[type] : TypeCode.Object;
        }

        public static bool IsAssignableFrom(this Type type, Type test)
        {
            return type.GetTypeInfo().IsAssignableFrom(test.GetTypeInfo());
        }

        public static PropertyInfo GetProperty(this Type type, string name)
        {
            return
                type.GetTypeInfo()
                    .DeclaredProperties.FirstOrDefault(
                        t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        public static ConstructorInfo GetConstructor(this Type type, Type[] parameters)
        {
            foreach (ConstructorInfo constructor in type.GetTypeInfo().DeclaredConstructors)
            {
                ParameterInfo[] @params = constructor.GetParameters();
                if (@params.Length == parameters.Length)
                {
                    bool result = true;
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        if (@params[i].ParameterType != parameters[i])
                        {
                            result = false;
                            break;
                        }
                    }
                    if (result)
                    {
                        return constructor;
                    }
                }
            }
            return null;
        }

        public static ConstructorInfo[] GetConstructors(this Type type)
        {
            return type.GetTypeInfo().DeclaredConstructors.ToArray();
        }

        public static Type[] GetGenericArguments(this Type type)
        {
            return type.GetTypeInfo().GenericTypeArguments;
        }

        public static PropertyInfo[] GetProperties(this Type type)
        {
            return type.GetTypeInfo().DeclaredProperties.ToArray();
        }

        public static FieldInfo[] GetFields(this Type type)
        {
            return type.GetTypeInfo().DeclaredFields.ToArray();
        }

        public static MemberInfo[] GetMembers(this Type type)
        {
            return type.GetTypeInfo().DeclaredMembers.ToArray();
        }

        public static MemberInfo[] GetMember(this Type type, string name)
        {
            return
                type.GetTypeInfo()
                    .DeclaredMembers.Where(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
        }

        public static MethodInfo[] GetMethods(this Type type)
        {
            return type.GetTypeInfo().DeclaredMethods.ToArray();
        }
    }

    public static class EnumerableHelper
    {
        public static ReadOnlyCollection<T> AsReadOnly<T>(this IEnumerable<T> items)
        {
            return new ReadOnlyCollection<T>(items.ToList());
        }
    }
}