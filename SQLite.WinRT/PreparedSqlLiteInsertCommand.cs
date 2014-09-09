using System;
using System.Diagnostics;

namespace SQLite.WinRT
{
    /// <summary>
    /// Since the insert never changed, we only need to prepare once.
    /// </summary>
    public class PreparedSqlLiteInsertCommand : IDisposable
    {
        public bool Initialized { get; set; }

        protected SQLiteConnection Connection { get; set; }

        public string CommandText { get; set; }

        protected Object Statement { get; set; }
        internal static readonly Object NullStatement = default(Object);

        internal PreparedSqlLiteInsertCommand(SQLiteConnection conn)
        {
            Connection = conn;
        }

        public int ExecuteNonQuery(object[] source)
        {
            if (Connection.Trace)
            {
                Debug.WriteLine("Executing: " + CommandText);
            }

            if (!Initialized)
            {
                Statement = Prepare();
                Initialized = true;
            }

            //bind the values.
            if (source != null)
            {
                for (var i = 0; i < source.Length; i++)
                {
                    SQLiteCommand.BindParameter(Statement, i + 1, source[i], Connection.StoreDateTimeAsTicks);
                }
            }
            var r = Platform.Current.SQLiteProvider.Step(Statement);

            switch (r)
            {
                case SQLiteResult.Done:
                    var rowsAffected = Platform.Current.SQLiteProvider.Changes(Connection.Handle);
                    Platform.Current.SQLiteProvider.Reset(Statement);
                    return rowsAffected;
                case SQLiteResult.Error:
                    var msg = Platform.Current.SQLiteProvider.GetErrorMessage(Connection.Handle);
                    Platform.Current.SQLiteProvider.Reset(Statement);
                    throw new SQLiteException(r, msg);
                default:
                    Platform.Current.SQLiteProvider.Reset(Statement);
                    throw new SQLiteException(r, r.ToString());
            }
        }

        protected virtual Object Prepare()
        {
            var stmt = Platform.Current.SQLiteProvider.Prepare2(Connection.Handle, CommandText);
            return stmt;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (Statement != NullStatement)
            {
                try
                {
                    Platform.Current.SQLiteProvider.Finalize(Statement);
                }
                finally
                {
                    Statement = NullStatement;
                    Connection = null;
                }
            }
        }

        ~PreparedSqlLiteInsertCommand()
        {
            Dispose(false);
        }
    }
}