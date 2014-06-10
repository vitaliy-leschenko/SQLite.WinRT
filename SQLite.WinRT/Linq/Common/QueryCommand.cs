// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SQLite.WinRT.Linq.Base;

namespace SQLite.WinRT.Linq.Common
{
    public class QueryCommand
    {
        public QueryCommand(string commandText, IEnumerable<QueryParameter> parameters)
        {
            CommandText = commandText;
            Parameters = parameters.ToReadOnly();
        }

        public string CommandText { get; private set; }

        public ReadOnlyCollection<QueryParameter> Parameters { get; private set; }
    }

    public class QueryParameter
    {
        public QueryParameter(string name, Type type, DbQueryType queryType)
        {
            Name = name;
            Type = type;
            QueryType = queryType;
        }

        public string Name { get; private set; }

        public Type Type { get; private set; }

        public DbQueryType QueryType { get; private set; }
    }
}