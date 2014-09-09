// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using SQLite.WinRT.Query;

namespace SQLite.WinRT.Linq.Base
{
    public interface IEntityProvider : IQueryProvider
    {
        IEntityTable<T> GetTable<T>(string tableId = null);

        SQLiteConnection Connection { get; }

        int CreateTable(Type type);
        int CreateTable<T>();
        void DropTable(string tableName);
        Task<int> CreateTableAsync<T>();
        Task DropTableAsync(string tableName);
    }

    public interface IEntityTable : IQueryable
    {
        new IEntityProvider Provider { get; }

        string TableId { get; }
    }

    public interface IEntityTable<T> : IQueryable<T>, IEntityTable
    {
        Update<T> Update();
        Delete<T> Delete();

        int Delete(T item);
        Task<int> DeleteAsync(T item);

        int Insert(T item);
        int InsertAll(IEnumerable<T> items);
        Task<int> InsertAsync(T item);
        Task<int> InsertAllAsync(IEnumerable<T> items);

        int Update(T item);
        Task<int> UpdateAsync(T item);
        int UpdateAll(IEnumerable<T> items);
        Task<int> UpdateAllAsync(IEnumerable<T> items);
    }
}