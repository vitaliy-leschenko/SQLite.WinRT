using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
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

            var stmt = Prepare();
            var r = Platform.Current.SQLiteProvider.Step(stmt);
            Finalize(stmt);
            switch (r)
            {
                case SQLiteResult.Done:
                    return Platform.Current.SQLiteProvider.Changes(conn.Handle);
                case SQLiteResult.Error:
                    var msg = Platform.Current.SQLiteProvider.GetErrorMessage(conn.Handle);
                    throw new SQLiteException(r, msg);
                default:
                    throw new SQLiteException(r, r.ToString());
            }
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
            Stopwatch watch = null;
            if (conn.TimeExecution)
            {
                watch = new Stopwatch();
                watch.Start();
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
                if (conn.TimeExecution && watch != null)
                {
                    watch.Stop();
                    Debug.WriteLine("Finished in {0} ms", watch.ElapsedMilliseconds);
                }

                Platform.Current.SQLiteProvider.Finalize(stmt);
            }
        }

        public IEnumerable<T> ExecuteDeferredQuery<T>(TableMapping map)
        {
            if (conn.Trace)
            {
                Debug.WriteLine("Executing: " + this);
            }

            Stopwatch watch = null;
            if (conn.TimeExecution)
            {
                watch = new Stopwatch();
                watch.Start();
            }

            var stmt = Prepare();
            try
            {
                var count = Platform.Current.SQLiteProvider.ColumnCount(stmt);
                var reader = new FieldReader(stmt);

                var t = Expression.Parameter(typeof (FieldReader), "t");
                var memberBindings = new List<MemberBinding>();
                for (var i = 0; i < count; i++)
                {
                    var name = Platform.Current.SQLiteProvider.ColumnName16(stmt, i);
                    var column = map.FindColumn(name);
                    if (column != null)
                    {
                        var method = FieldReader.GetReaderMethod(column.Property.PropertyType);
                        var val = Expression.Call(t, method, new Expression[] { Expression.Constant(i) });
                        memberBindings.Add(Expression.Bind(column.Property.SetMethod, val));
                    }
                }

                var ctor = Expression.New(map.MappedType);
                var lambda = Expression.Lambda<Func<FieldReader, T>>(Expression.MemberInit(ctor, memberBindings), t);
                var func = lambda.Compile();

                while (Platform.Current.SQLiteProvider.Step(stmt) == SQLiteResult.Row)
                {
                    yield return func(reader);
                }
            }
            finally
            {
                if (conn.TimeExecution && watch != null)
                {
                    watch.Stop();
                    Debug.WriteLine("Finished in {0} ms", watch.ElapsedMilliseconds);
                }
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

            var t = Expression.Parameter(typeof(FieldReader), "t");
            var method = FieldReader.GetReaderMethod(typeof(T));
            var eval = Expression.Call(t, method, new Expression[] { Expression.Constant(0) });
            var lambda = Expression.Lambda<Func<FieldReader, T>>(eval, t);
            var func = lambda.Compile();

            var stmt = Prepare();
            var reader = new FieldReader(stmt);
            if (Platform.Current.SQLiteProvider.Step(stmt) == SQLiteResult.Row)
            {
                val = func(reader);
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

        internal static IntPtr negativePointer = new IntPtr(-1);

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
                    Platform.Current.SQLiteProvider.BindText(stmt, index, (string)value, -1, negativePointer);
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
                        Platform.Current.SQLiteProvider.BindText(stmt, index, ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss"), -1, negativePointer);
                    }
                }
                else if (value.GetType().GetTypeInfo().IsEnum)
                {
                    Platform.Current.SQLiteProvider.BindInt(stmt, index, Convert.ToInt32(value));
                }
                else if (value is byte[])
                {
                    Platform.Current.SQLiteProvider.BindBlob(stmt, index, (byte[])value, ((byte[])value).Length, negativePointer);
                }
                else if (value is Guid)
                {
                    Platform.Current.SQLiteProvider.BindText(stmt, index, ((Guid)value).ToString(), 72, negativePointer);
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
    }
}