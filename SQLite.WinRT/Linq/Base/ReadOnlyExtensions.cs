// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SQLite.WinRT.Linq.Base
{
	public static class ReadOnlyExtensions
	{
		public static ReadOnlyCollection<T> ToReadOnly<T>(this IEnumerable<T> collection)
		{
			ReadOnlyCollection<T> roc = collection as ReadOnlyCollection<T>;
			if (roc == null)
			{
				if (collection == null)
				{
					roc = EmptyReadOnlyCollection<T>.Empty;
				}
				else
				{
                    roc = new ReadOnlyCollection<T>(collection.ToList());
				}
			}
			return roc;
		}

		private class EmptyReadOnlyCollection<T>
		{
            internal static readonly ReadOnlyCollection<T> Empty = new ReadOnlyCollection<T>(new T[0]);
		}
	}
}