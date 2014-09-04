// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Linq;
using System.Linq.Expressions;
using SQLite.WinRT.Query;

namespace SQLite.WinRT.Linq.Base
{
    public interface IEntityProvider : IQueryProvider
    {
        IEntityTable<T> GetTable<T>(string tableId);

        IEntityTable GetTable(Type type, string tableId);

        bool CanBeEvaluatedLocally(Expression expression);

        bool CanBeParameter(Expression expression);
        SQLiteConnection Connection { get; }
    }

    public interface IEntityTable : IQueryable
    {
        new IEntityProvider Provider { get; }

        string TableId { get; }

        object GetById(object id);
    }

    public interface IEntityTable<T> : IQueryable<T>, IEntityTable
    {
        new T GetById(object id);
        Update Update();
    }
}