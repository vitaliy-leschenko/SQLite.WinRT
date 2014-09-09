using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using SQLite.WinRT.Linq.Common;

namespace SQLite.WinRT
{
    public class SQLiteCommand
    {
        readonly SQLiteConnection conn;
        private readonly List<Binding> bindings;

        public string CommandText { get; set; }

        internal SQLiteCommand(SQLiteConnection conn)
        {
            this.conn = conn;
            bindings = new List<Binding>();
            CommandText = "";
        }

        public int ExecuteNonQuery()
        {
            if (conn.Trace)
            {
                Debug.WriteLine("Executing: " + this);
            }

            var r = SQLiteResult.OK;
            var stmt = Prepare();
            r = Platform.Current.SQLiteProvider.Step(stmt);// SQLite3.Step (stmt);
            Finalize(stmt);
            if (r == SQLiteResult.Done)
            {
                int rowsAffected = Platform.Current.SQLiteProvider.Changes(conn.Handle);
                return rowsAffected;
            }
            else if (r == SQLiteResult.Error)
            {
                string msg = Platform.Current.SQLiteProvider.GetErrorMessage(conn.Handle);
                throw SQLiteException.New(r, msg);
            }
            else
            {
                throw SQLiteException.New(r, r.ToString());
            }
        }

        public IEnumerable<T> ExecuteDeferredQuery<T>()
        {
            return ExecuteDeferredQuery<T>(conn.GetMapping(typeof(T)));
        }

        public List<T> ExecuteQuery<T>()
        {
            return ExecuteDeferredQuery<T>(conn.GetMapping(typeof(T))).ToList();
        }

        public IEnumerable<T> ExecuteQueryProjector<T>(Func<FieldReader, T> projector)
        {
            if (conn.Trace)
            {
                Debug.WriteLine("Executing: " + this);
            }
            var stmt = Prepare();
            try
            {
                var reader = new FieldReader(stmt);
                while (Platform.Current.SQLiteProvider.Step(stmt) == SQLiteResult.Row)
                {
                    var item = projector(reader);
                    yield return item;
                }
            }
            finally
            {
                Platform.Current.SQLiteProvider.Finalize(stmt);
            }
        }

        public List<T> ExecuteQuery<T>(TableMapping map)
        {
            return ExecuteDeferredQuery<T>(map).ToList();
        }

        /// <summary>
        /// Invoked every time an instance is loaded from the database.
        /// </summary>
        /// <param name='obj'>
        /// The newly created object.
        /// </param>
        /// <remarks>
        /// This can be overridden in combination with the <see cref="SQLiteConnection.NewCommand"/>
        /// method to hook into the life-cycle of objects.
        ///
        /// Type safety is not possible because MonoTouch does not support virtual generic methods.
        /// </remarks>
        protected virtual void OnInstanceCreated(object obj)
        {
            // Can be overridden.
        }

        public IEnumerable<T> ExecuteDeferredQuery<T>(TableMapping map)
        {
            if (conn.Trace)
            {
                Debug.WriteLine("Executing: " + this);
            }

            var stmt = Prepare();
            try
            {
                var cols = new TableMapping.Column[Platform.Current.SQLiteProvider.ColumnCount(stmt)];

                for (int i = 0; i < cols.Length; i++)
                {
                    var name = Platform.Current.SQLiteProvider.ColumnName16(stmt, i);
                    cols[i] = map.FindColumn(name);
                }

                while (Platform.Current.SQLiteProvider.Step(stmt) == SQLiteResult.Row)
                {
                    var obj = Activator.CreateInstance(map.MappedType);
                    for (int i = 0; i < cols.Length; i++)
                    {
                        if (cols[i] == null)
                            continue;
                        var colType = Platform.Current.SQLiteProvider.ColumnType(stmt, i);
                        var val = ReadCol(stmt, i, colType, cols[i].ColumnType);
                        cols[i].SetValue(obj, val);
                    }
                    OnInstanceCreated(obj);
                    yield return (T)obj;
                }
            }
            finally
            {
                Platform.Current.SQLiteProvider.Finalize(stmt);
            }
        }

        public T ExecuteScalar<T>()
        {
            if (conn.Trace)
            {
                Debug.WriteLine("Executing Query: " + this);
            }

            T val = default(T);

            var stmt = Prepare();
            if (Platform.Current.SQLiteProvider.Step(stmt) == SQLiteResult.Row)
            {
                var colType = Platform.Current.SQLiteProvider.ColumnType(stmt, 0);
                val = (T)ReadCol(stmt, 0, colType, typeof(T));
            }
            Finalize(stmt);

            return val;
        }

        public void Bind(string name, object val)
        {
            bindings.Add(new Binding
            {
                Name = name,
                Value = val
            });
        }

        public void Bind(object val)
        {
            Bind(null, val);
        }

        public override string ToString()
        {
            var parts = new string[1 + bindings.Count];
            parts[0] = CommandText;
            var i = 1;
            foreach (var b in bindings)
            {
                parts[i] = string.Format("  {0}: {1}", i - 1, b.Value);
                i++;
            }
            return string.Join(Environment.NewLine, parts);
        }

        Object Prepare()
        {
            var stmt = Platform.Current.SQLiteProvider.Prepare2(conn.Handle, CommandText);
            BindAll(stmt);
            return stmt;
        }

        void Finalize(Object stmt)
        {
            Platform.Current.SQLiteProvider.Finalize(stmt);
        }

        void BindAll(Object stmt)
        {
            int nextIdx = 1;
            foreach (var b in bindings)
            {
                if (b.Name != null)
                {
                    b.Index = Platform.Current.SQLiteProvider.BindParameterIndex(stmt, b.Name);
                }
                else
                {
                    b.Index = nextIdx++;
                }

                BindParameter(stmt, b.Index, b.Value, conn.StoreDateTimeAsTicks);
            }
        }

        internal static IntPtr NegativePointer = new IntPtr(-1);

        internal static void BindParameter(Object stmt, int index, object value, bool storeDateTimeAsTicks)
        {
            if (value == null)
            {
                Platform.Current.SQLiteProvider.BindNull(stmt, index);
            }
            else
            {
                if (value is Int32)
                {
                    Platform.Current.SQLiteProvider.BindInt(stmt, index, (int)value);
                }
                else if (value is String)
                {
                    Platform.Current.SQLiteProvider.BindText(stmt, index, (string)value, -1, NegativePointer);
                }
                else if (value is Byte || value is UInt16 || value is SByte || value is Int16)
                {
                    Platform.Current.SQLiteProvider.BindInt(stmt, index, Convert.ToInt32(value));
                }
                else if (value is Boolean)
                {
                    Platform.Current.SQLiteProvider.BindInt(stmt, index, (bool)value ? 1 : 0);
                }
                else if (value is UInt32 || value is Int64)
                {
                    Platform.Current.SQLiteProvider.BindInt64(stmt, index, Convert.ToInt64(value));
                }
                else if (value is Single || value is Double || value is Decimal)
                {
                    Platform.Current.SQLiteProvider.BindDouble(stmt, index, Convert.ToDouble(value));
                }
                else if (value is DateTime)
                {
                    if (storeDateTimeAsTicks)
                    {
                        Platform.Current.SQLiteProvider.BindInt64(stmt, index, ((DateTime)value).Ticks);
                    }
                    else
                    {
                        Platform.Current.SQLiteProvider.BindText(stmt, index, ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss"), -1, NegativePointer);
                    }
                }
                else if (value.GetType().GetTypeInfo().IsEnum)
                {
                    Platform.Current.SQLiteProvider.BindInt(stmt, index, Convert.ToInt32(value));
                }
                else if (value is byte[])
                {
                    Platform.Current.SQLiteProvider.BindBlob(stmt, index, (byte[])value, ((byte[])value).Length, NegativePointer);
                }
                else if (value is Guid)
                {
                    Platform.Current.SQLiteProvider.BindText(stmt, index, ((Guid)value).ToString(), 72, NegativePointer);
                }
                else
                {
                    throw new NotSupportedException("Cannot store type: " + value.GetType());
                }
            }
        }

        class Binding
        {
            public string Name { get; set; }

            public object Value { get; set; }

            public int Index { get; set; }
        }

        object ReadCol(Object stmt, int index, ColType type, Type clrType)
        {
            if (type == ColType.Null)
            {
                return null;
            }
            if (clrType == typeof(String))
            {
                return Platform.Current.SQLiteProvider.ColumnString(stmt, index);
            }
            if (clrType == typeof(Int32))
            {
                return (int)Platform.Current.SQLiteProvider.ColumnInt(stmt, index);
            }
            if (clrType == typeof(Boolean))
            {
                return Platform.Current.SQLiteProvider.ColumnInt(stmt, index) == 1;
            }
            if (clrType == typeof(double))
            {
                return Platform.Current.SQLiteProvider.ColumnDouble(stmt, index);
            }
            if (clrType == typeof(float))
            {
                return (float)Platform.Current.SQLiteProvider.ColumnDouble(stmt, index);
            }
            if (clrType == typeof(DateTime))
            {
                if (conn.StoreDateTimeAsTicks)
                {
                    return new DateTime(Platform.Current.SQLiteProvider.ColumnInt64(stmt, index));
                }
                var text = Platform.Current.SQLiteProvider.ColumnString(stmt, index);
                return DateTime.Parse(text);
            }
            if (clrType.GetTypeInfo().IsEnum)
            {
                return Platform.Current.SQLiteProvider.ColumnInt(stmt, index);
            }
            if (clrType == typeof(Int64))
            {
                return Platform.Current.SQLiteProvider.ColumnInt64(stmt, index);
            }
            if (clrType == typeof(UInt32))
            {
                return (uint)Platform.Current.SQLiteProvider.ColumnInt64(stmt, index);
            }
            if (clrType == typeof(decimal))
            {
                return (decimal)Platform.Current.SQLiteProvider.ColumnDouble(stmt, index);
            }
            if (clrType == typeof(Byte))
            {
                return (byte)Platform.Current.SQLiteProvider.ColumnInt(stmt, index);
            }
            if (clrType == typeof(UInt16))
            {
                return (ushort)Platform.Current.SQLiteProvider.ColumnInt(stmt, index);
            }
            if (clrType == typeof(Int16))
            {
                return (short)Platform.Current.SQLiteProvider.ColumnInt(stmt, index);
            }
            if (clrType == typeof(sbyte))
            {
                return (sbyte)Platform.Current.SQLiteProvider.ColumnInt(stmt, index);
            }
            if (clrType == typeof(byte[]))
            {
                return Platform.Current.SQLiteProvider.ColumnByteArray(stmt, index);
            }
            if (clrType == typeof(Guid))
            {
                var text = Platform.Current.SQLiteProvider.ColumnString(stmt, index);
                return new Guid(text);
            }
            throw new NotSupportedException("Don't know how to read " + clrType);
        }
    }
}