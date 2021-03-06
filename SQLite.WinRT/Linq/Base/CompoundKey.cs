﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;

namespace SQLite.WinRT.Linq.Base
{
    public class CompoundKey : IEquatable<CompoundKey>, IEnumerable<object>, IEnumerable
    {
        private readonly int hc;
        private readonly object[] values;

        public CompoundKey(params object[] values)
        {
            this.values = values;
            for (int i = 0, n = values.Length; i < n; i++)
            {
                object value = values[i];
                if (value != null)
                {
                    hc ^= (value.GetHashCode() + i);
                }
            }
        }

        public IEnumerator<object> GetEnumerator()
        {
            return ((IEnumerable<object>) values).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Equals(CompoundKey other)
        {
            if (other == null || other.values.Length != values.Length)
            {
                return false;
            }
            for (int i = 0, n = other.values.Length; i < n; i++)
            {
                if (!Equals(values[i], other.values[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public override int GetHashCode()
        {
            return hc;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
    }
}