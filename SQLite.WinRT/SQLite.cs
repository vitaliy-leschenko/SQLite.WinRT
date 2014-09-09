//
// Copyright (c) 2009-2012 Krueger Systems, Inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SQLite.WinRT.Linq;
using SQLite.WinRT.Linq.Base;
using SQLite.WinRT.Linq.Common;
using Sqlite3DatabaseHandle = System.Object;

namespace SQLite.WinRT
{
    [Flags]
    public enum SQLiteOpenFlags
    {
        ReadOnly = 1, ReadWrite = 2, Create = 4,
        NoMutex = 0x8000, FullMutex = 0x10000,
        SharedCache = 0x20000, PrivateCache = 0x40000,
        ProtectionComplete = 0x00100000,
        ProtectionCompleteUnlessOpen = 0x00200000,
        ProtectionCompleteUntilFirstUserAuthentication = 0x00300000,
        ProtectionNone = 0x00400000
    }

    /// <summary>
    /// Represents an open connection to a SQLite database.
    /// </summary>
    public class SQLiteConnection : IDisposable
    {
        readonly object lockPoint = new object();


        public IDisposable Lock()
        {
            return new LockWrapper(lockPoint);
        }

        private class LockWrapper : IDisposable
        {
            readonly object lockPoint;

            public LockWrapper(object lockPoint)
            {
                this.lockPoint = lockPoint;
                Monitor.Enter(this.lockPoint);
            }

            public void Dispose()
            {
                Monitor.Exit(lockPoint);
            }
        }

        private bool open;
        private TimeSpan busyTimeout;
        private Dictionary<string, TableMapping> mappings;
        private Dictionary<string, TableMapping> tables;
        private Stopwatch watch;
        private long elapsedMilliseconds;

        private int trasactionDepth;
        private readonly Random rand = new Random();

        public Sqlite3DatabaseHandle Handle { get; private set; }
        internal static readonly Sqlite3DatabaseHandle NullHandle = default(Sqlite3DatabaseHandle);

        public string DatabasePath { get; private set; }

        public bool TimeExecution { get; set; }

        public bool Trace { get; set; }

        public bool StoreDateTimeAsTicks { get; private set; }

        public SQLiteConnection(SQLiteConnectionString connectionString)
            : this(connectionString.DatabasePath, connectionString.StoreDateTimeAsTicks)
        {
        }

        /// <summary>
        /// Constructs a new SQLiteConnection and opens a SQLite database specified by databasePath.
        /// </summary>
        /// <param name="databasePath">
        /// Specifies the path to the database file.
        /// </param>
        /// <param name="storeDateTimeAsTicks">
        /// Specifies whether to store DateTime properties as ticks (true) or strings (false). You
        /// absolutely do want to store them as Ticks in all new projects. The default of false is
        /// only here for backwards compatibility. There is a *significant* speed advantage, with no
        /// down sides, when setting storeDateTimeAsTicks = true.
        /// </param>
        public SQLiteConnection(string databasePath, bool storeDateTimeAsTicks = false)
            : this(databasePath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create, storeDateTimeAsTicks)
        {
        }

        public SQLiteConnection(string databasePath, SQLiteOpenFlags flags, bool storeDateTimeAsTicks = false)
        {
            DatabasePath = databasePath;

            Platform.Current.PlatformStorage.SetTempDirectory();

            Sqlite3DatabaseHandle handle;
            var r = Platform.Current.SQLiteProvider.Open(DatabasePath, out handle, (int)flags, IntPtr.Zero);

            Handle = handle;
            if (r != SQLiteResult.OK)
            {
                throw new SQLiteException(r, String.Format("Could not open database file: {0} ({1})", DatabasePath, r));
            }
            open = true;

            StoreDateTimeAsTicks = storeDateTimeAsTicks;
            BusyTimeout = TimeSpan.FromSeconds(0.1);
        }

        /// <summary>
        /// Sets a busy handler to sleep the specified amount of time when a table is locked.
        /// The handler will sleep multiple times until a total time of <see cref="BusyTimeout"/> has accumulated.
        /// </summary>
        public TimeSpan BusyTimeout
        {
            get { return busyTimeout; }
            set
            {
                busyTimeout = value;
                if (Handle != NullHandle)
                {
                    Platform.Current.SQLiteProvider.BusyTimeout(Handle, (int)busyTimeout.TotalMilliseconds);
                }
            }
        }

        /// <summary>
        /// Returns the mappings from types to tables that the connection
        /// currently understands.
        /// </summary>
        public IEnumerable<TableMapping> TableMappings
        {
            get
            {
                return tables == null ? Enumerable.Empty<TableMapping>() : tables.Values;
            }
        }

        /// <summary>
        /// Retrieves the mapping that is automatically generated for the given type.
        /// </summary>
        /// <param name="type">
        ///     The type whose mapping to the database is returned.
        /// </param>
        /// <returns>
        /// The mapping represents the schema of the columns of the database and contains 
        /// methods to set and get properties of objects.
        /// </returns>
        public TableMapping GetMapping(Type type)
        {
            if (mappings == null)
            {
                mappings = new Dictionary<string, TableMapping>();
            }
            TableMapping map;
            if (!mappings.TryGetValue(type.FullName, out map))
            {
                map = new TableMapping(type, this);
                mappings[type.FullName] = map;
            }
            return map;
        }

        /// <summary>
        /// Retrieves the mapping that is automatically generated for the given type.
        /// </summary>
        /// <returns>
        /// The mapping represents the schema of the columns of the database and contains 
        /// methods to set and get properties of objects.
        /// </returns>
        public TableMapping GetMapping<T>()
        {
            return GetMapping(typeof(T));
        }

        private struct IndexedColumn
        {
            public int Order { get; set; }
            public string ColumnName { get; set; }
        }

        private struct IndexInfo
        {
            public string IndexName { get; set; }
            public string TableName { get; set; }
            public bool Unique { get; set; }
            public List<IndexedColumn> Columns { get; set; }
        }

        /// <summary>
        /// Executes a "create table if not exists" on the database. It also
        /// creates any specified indexes on the columns of the table. It uses
        /// a schema automatically generated from the specified type. You can
        /// later access this schema by calling GetMapping.
        /// </summary>
        /// <param name="ty">Type to reflect to a database table.</param>
        /// <returns>
        /// The number of entries added to the database schema.
        /// </returns>
        public int CreateTable(Type ty)
        {
            if (tables == null)
            {
                tables = new Dictionary<string, TableMapping>();
            }
            TableMapping map;
            if (!tables.TryGetValue(ty.FullName, out map))
            {
                map = GetMapping(ty);
                tables.Add(ty.FullName, map);
            }
            var query = "create table if not exists \"" + map.TableName + "\"(\n";

            var decls = map.Columns.Select(p => Orm.SqlDecl(p, StoreDateTimeAsTicks));
            var decl = string.Join(",\n", decls.ToArray());
            query += decl;
            query += ")";

            var count = Execute(query);
            MigrateTable(map);

            var indexes = new Dictionary<string, IndexInfo>();
            foreach (var c in map.Columns)
            {
                foreach (var i in c.Indices)
                {
                    var iname = i.Name ?? map.TableName + "_" + c.Name;
                    IndexInfo iinfo;
                    if (!indexes.TryGetValue(iname, out iinfo))
                    {
                        iinfo = new IndexInfo
                        {
                            IndexName = iname,
                            TableName = map.TableName,
                            Unique = i.Unique,
                            Columns = new List<IndexedColumn>()
                        };
                        indexes.Add(iname, iinfo);
                    }

                    if (i.Unique != iinfo.Unique)
                        throw new Exception("All the columns in an index must have the same value for their Unique property");

                    iinfo.Columns.Add(new IndexedColumn
                    {
                        Order = i.Order,
                        ColumnName = c.Name
                    });
                }
            }

            foreach (var indexName in indexes.Keys)
            {
                var index = indexes[indexName];
                const string sqlFormat = "create {3} index if not exists \"{0}\" on \"{1}\"(\"{2}\")";
                var columns = String.Join("\",\"", index.Columns.OrderBy(i => i.Order).Select(i => i.ColumnName).ToArray());
                var sql = String.Format(sqlFormat, indexName, index.TableName, columns, index.Unique ? "unique" : "");
                count += Execute(sql);
            }

            return count;
        }

        public class ColumnInfo
        {
            [Column("cid")]
            public int ColumnID { get; set; }
            [Column("name")]
            public string Name { get; set; }
            [Column("type")]
            public string Type { get; set; }
            [Column("notnull")]
            public bool NotNull { get; set; }
            [Column("dflt_value")]
            public string DefaultValue { get; set; }
            [Column("pk")]
            public bool IsPrimaryKey { get; set; }

            public override string ToString()
            {
                return Name;
            }
        }

        public IEnumerable<ColumnInfo> GetTableInfo(string tableName)
        {
            var query = "pragma table_info(\"" + tableName + "\")";
            return Query<ColumnInfo>(query);
        }

        private void MigrateTable(TableMapping map)
        {
            var existingCols = GetTableInfo(map.TableName).ToList();

            var toBeAdded = new List<TableMapping.Column>();

            foreach (var p in map.Columns)
            {
                var found = false;
                foreach (var c in existingCols)
                {
                    found = (string.Compare(p.Name, c.Name, StringComparison.OrdinalIgnoreCase) == 0);
                    if (found) break;
                }
                if (!found)
                {
                    toBeAdded.Add(p);
                }
            }

            foreach (var p in toBeAdded)
            {
                var addCol = "alter table \"" + map.TableName + "\" add column " + Orm.SqlDecl(p, StoreDateTimeAsTicks);
                Execute(addCol);
            }
        }

        /// <summary>
        /// Creates a new SQLiteCommand given the command text with arguments. Place a '?'
        /// in the command text for each of the arguments.
        /// </summary>
        /// <param name="cmdText">
        /// The fully escaped SQL.
        /// </param>
        /// <param name="args">
        /// Arguments to substitute for the occurences of '?' in the command text.
        /// </param>
        /// <returns>
        /// A <see cref="SQLiteCommand"/>
        /// </returns>
        public SQLiteCommand CreateCommand(string cmdText, params object[] args)
        {
            if (!open) throw new SQLiteException(SQLiteResult.Error, "Cannot create commands from unopened database");

            var cmd = new SQLiteCommand(this);
            cmd.CommandText = cmdText;
            foreach (var o in args)
            {
                cmd.Bind(o);
            }
            return cmd;
        }

        /// <summary>
        /// Creates a SQLiteCommand given the command text (SQL) with arguments. Place a '?'
        /// in the command text for each of the arguments and then executes that command.
        /// Use this method instead of Query when you don't expect rows back. Such cases include
        /// INSERTs, UPDATEs, and DELETEs.
        /// You can set the Trace or TimeExecution properties of the connection
        /// to profile execution.
        /// </summary>
        /// <param name="query">
        /// The fully escaped SQL.
        /// </param>
        /// <param name="args">
        /// Arguments to substitute for the occurences of '?' in the query.
        /// </param>
        /// <returns>
        /// The number of rows modified in the database as a result of this execution.
        /// </returns>
        public int Execute(string query, params object[] args)
        {
            var cmd = CreateCommand(query, args);

            if (TimeExecution)
            {
                if (watch == null)
                {
                    watch = new Stopwatch();
                }
                watch.Reset();
                watch.Start();
            }

            var r = cmd.ExecuteNonQuery();

            if (TimeExecution)
            {
                watch.Stop();
                elapsedMilliseconds += watch.ElapsedMilliseconds;
                Debug.WriteLine("Finished in {0} ms ({1:0.0} s total)", watch.ElapsedMilliseconds, elapsedMilliseconds / 1000.0);
            }

            return r;
        }

        public T ExecuteScalar<T>(string query, params object[] args)
        {
            if (Trace)
            {
                Debug.WriteLine("Executing: " + query);
            }

            var cmd = CreateCommand(query, args);

            if (TimeExecution)
            {
                if (watch == null)
                {
                    watch = new Stopwatch();
                }
                watch.Reset();
                watch.Start();
            }

            var r = cmd.ExecuteScalar<T>();

            if (TimeExecution)
            {
                watch.Stop();
                elapsedMilliseconds += watch.ElapsedMilliseconds;
                Debug.WriteLine("Finished in {0} ms ({1:0.0} s total)", watch.ElapsedMilliseconds, elapsedMilliseconds / 1000.0);
            }

            return r;
        }

        /// <summary>
        /// Creates a SQLiteCommand given the command text (SQL) with arguments. Place a '?'
        /// in the command text for each of the arguments and then executes that command.
        /// It returns each row of the result using the mapping automatically generated for
        /// the given type.
        /// </summary>
        /// <param name="query">
        /// The fully escaped SQL.
        /// </param>
        /// <param name="args">
        /// Arguments to substitute for the occurences of '?' in the query.
        /// </param>
        /// <returns>
        /// An enumerable with one result for each row returned by the query.
        /// </returns>
        public List<T> Query<T>(string query, params object[] args) where T : new()
        {
            var cmd = CreateCommand(query, args);
            return cmd.ExecuteQuery<T>();
        }

        /// <summary>
        /// Whether <see cref="SaveTransactionPoint"/> has been called and the database is waiting for a <see cref="Release"/>.
        /// </summary>
        public bool IsInTransaction
        {
            get { return trasactionDepth > 0; }
        }

        /// <summary>
        /// Creates a savepoint in the database at the current point in the transaction timeline.
        /// Begins a new transaction if one is not in progress.
        /// 
        /// Call <see cref="RollbackTo"/> to undo transactions since the returned savepoint.
        /// Call <see cref="Release"/> to commit transactions after the savepoint returned here.
        /// </summary>
        /// <returns>A string naming the savepoint.</returns>
        public string SaveTransactionPoint()
        {
            var depth = Interlocked.Increment(ref trasactionDepth) - 1;
            var retVal = "S" + (short)rand.Next(short.MaxValue) + "D" + depth;

            try
            {
                Execute("savepoint " + retVal);
            }
            catch (Exception ex)
            {
                var sqlExp = ex as SQLiteException;
                if (sqlExp != null)
                {
                    // It is recommended that applications respond to the errors listed below 
                    //    by explicitly issuing a ROLLBACK command.
                    // TODO: This rollback failsafe should be localized to all throw sites.
                    switch (sqlExp.Result)
                    {
                        case SQLiteResult.IOError:
                        case SQLiteResult.Full:
                        case SQLiteResult.Busy:
                        case SQLiteResult.NoMem:
                        case SQLiteResult.Interrupt:
                            RollbackTo(null, true);
                            break;
                    }
                }
                else
                {
                    Interlocked.Decrement(ref trasactionDepth);
                }

                throw;
            }

            return retVal;
        }

        /// <summary>
        /// Rolls back the transaction that was begun by <see cref="SaveTransactionPoint"/> or <see cref="SaveTransactionPoint"/>.
        /// </summary>
        public void Rollback()
        {
            RollbackTo(null, false);
        }

        /// <summary>
        /// Rolls back the savepoint created by <see cref="SaveTransactionPoint"/> or SaveTransactionPoint.
        /// </summary>
        /// <param name="savepoint">The name of the savepoint to roll back to, as returned by <see cref="SaveTransactionPoint"/>.  If savepoint is null or empty, this method is equivalent to a call to <see cref="Rollback"/></param>
        public void RollbackTo(string savepoint)
        {
            RollbackTo(savepoint, false);
        }

        /// <summary>
        /// Rolls back the transaction that was begun by <see cref="SaveTransactionPoint"/>.
        /// </summary>
        /// <param name="savepoint"></param>
        /// <param name="noThrow">true to avoid throwing exceptions, false otherwise</param>
        private void RollbackTo(string savepoint, bool noThrow)
        {
            // Rolling back without a TO clause rolls backs all transactions 
            //    and leaves the transaction stack empty.   
            try
            {
                if (String.IsNullOrEmpty(savepoint))
                {
                    if (Interlocked.Exchange(ref trasactionDepth, 0) > 0)
                    {
                        Execute("rollback");
                    }
                }
                else
                {
                    DoSavePointExecute(savepoint, "rollback to ");
                }
            }
            catch (SQLiteException)
            {
                if (!noThrow)
                {
                    throw;
                }
            }
            // No need to rollback if there are no transactions open.
        }

        public void Release(string savepoint)
        {
            DoSavePointExecute(savepoint, "release ");
        }

        private void DoSavePointExecute(string savepoint, string cmd)
        {
            // Validate the savepoint
            int firstLen = savepoint.IndexOf('D');
            if (firstLen >= 2 && savepoint.Length > firstLen + 1)
            {
                int depth;
                if (Int32.TryParse(savepoint.Substring(firstLen + 1), out depth))
                {
                    // TODO: Mild race here, but inescapable without locking almost everywhere.
                    if (0 <= depth && depth < trasactionDepth)
                    {
                        Volatile.Write(ref trasactionDepth, depth);
                        Execute(cmd + savepoint);
                        return;
                    }
                }
            }

            throw new ArgumentException("SavePoint is not valid, and should be the result of a call to SaveTransactionPoint.", "savepoint");
        }

        /// <summary>
        /// Executes <param name="action" /> within a (possibly nested) transaction by wrapping it in a SAVEPOINT. If an
        /// exception occurs the whole transaction is rolled back, not just the current savepoint. The exception
        /// is rethrown.
        /// </summary>
        /// <param name="action">
        /// The <see cref="Action"/> to perform within a transaction.
        /// </param>
        public void RunInTransaction(Action action)
        {
            try
            {
                var savePoint = SaveTransactionPoint();
                action();
                Release(savePoint);
            }
            catch (Exception)
            {
                Rollback();
                throw;
            }
        }

        /// <summary>
        /// Deletes the given object from the database using its primary key.
        /// </summary>
        /// <param name="objectToDelete">
        /// The object to delete. It must have a primary key designated using the PrimaryKeyAttribute.
        /// </param>
        /// <returns>
        /// The number of rows deleted.
        /// </returns>
        public int Delete(object objectToDelete)
        {
            var map = GetMapping(objectToDelete.GetType());
            var pk = map.PK;
            if (pk == null)
            {
                throw new NotSupportedException("Cannot delete " + map.TableName + ": it has no PK");
            }
            var q = string.Format("delete from \"{0}\" where \"{1}\" = ?", map.TableName, pk.Name);
            return Execute(q, pk.GetValue(objectToDelete));
        }

        ~SQLiteConnection()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            Close();
        }

        public void Close()
        {
            if (open && Handle != NullHandle)
            {
                try
                {
                    if (mappings != null)
                    {
                        foreach (var sqlInsertCommand in mappings.Values)
                        {
                            sqlInsertCommand.Dispose();
                        }
                    }
                    var r = Platform.Current.SQLiteProvider.Close(Handle);
                    //SQLite3.Close (Handle);
                    if (r != SQLiteResult.OK)
                    {
                        string msg = Platform.Current.SQLiteProvider.GetErrorMessage(Handle);
                        throw new SQLiteException(r, msg);
                    }
                }
                finally
                {
                    Handle = NullHandle;
                    open = false;
                }
            }
        }

        internal IEnumerable<T> LinqQuery<T>(string commandText, object[] paramValues, Func<FieldReader, T> projector)
        {
            var cmd = CreateCommand(commandText, paramValues);
            return cmd.ExecuteQueryProjector(projector);
        }

        private IEntityProvider provider;
        internal IEntityProvider GetEntityProvider()
        {
            if (provider != null) return provider;
            return provider = new EntityProvider(this);
        }
    }
}
