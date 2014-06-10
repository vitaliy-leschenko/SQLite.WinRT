// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections.Generic;
using System.Linq;

namespace SQLite.WinRT.Linq.Base
{
    public struct DeferredValue<T> : IDeferLoadable
    {
        private bool loaded;
        private IEnumerable<T> source;

        private T value;

        public DeferredValue(T value)
        {
            this.value = value;
            source = null;
            loaded = true;
        }

        public DeferredValue(IEnumerable<T> source)
        {
            this.source = source;
            loaded = false;
            value = default(T);
        }

        public bool IsAssigned
        {
            get { return loaded && source == null; }
        }

        public T Value
        {
            get
            {
                Check();
                return value;
            }

            set
            {
                this.value = value;
                loaded = true;
                source = null;
            }
        }

        public void Load()
        {
            if (source != null)
            {
                value = source.SingleOrDefault();
                loaded = true;
            }
        }

        public bool IsLoaded
        {
            get { return loaded; }
        }

        private void Check()
        {
            if (!IsLoaded)
            {
                Load();
            }
        }
    }
}