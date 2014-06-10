// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Reflection;

namespace SQLite.WinRT.Linq.Base
{
    public static class ReflectionExtensions
    {
        public static object GetValue(this MemberInfo member, object instance)
        {
            var property = member as PropertyInfo;
            if (property != null)
            {
                return property.GetValue(instance, null);
            }

            var field = member as FieldInfo;
            if (field != null)
            {
                return field.GetValue(instance);
            }

            throw new InvalidOperationException();
        }

        public static void SetValue(this MemberInfo member, object instance, object value)
        {
            var property = member as PropertyInfo;
            if (property != null)
            {
                property.SetValue(instance, value, null);
                return;
            }

            var field = member as FieldInfo;
            if (field != null)
            {
                field.SetValue(instance, value);
            }

            throw new InvalidOperationException();
        }
    }
}