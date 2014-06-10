﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SQLite.WinRT.Linq.Base
{
    /// <summary>
    ///     Simple implementation of the IGrouping<TKey, TElement> interface
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TElement"></typeparam>
    public class Grouping<TKey, TElement> : IGrouping<TKey, TElement>
    {
        private readonly TKey key;

        private IEnumerable<TElement> group;

        public Grouping(TKey key, IEnumerable<TElement> group)
        {
            this.key = key;
            this.group = group;
        }

        public TKey Key
        {
            get { return key; }
        }

        public IEnumerator<TElement> GetEnumerator()
        {
            if (!(group is List<TElement>))
            {
                group = group.ToList();
            }
            return @group.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return @group.GetEnumerator();
        }
    }
}