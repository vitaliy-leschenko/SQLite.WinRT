namespace SQLite.WinRT.Query
{
    public interface ISetting<T>
    {
        string ColumnName { get; }
        object Value { get; }
        bool IsExpression { get; }
        string ExpressionValue { get; }
    }

    public class Setting<T, TValue> : ISetting<T>
    {
        public TValue Value { get; internal set; }
        public string ColumnName { get; internal set; }
        object ISetting<T>.Value { get { return Value; } }
        public bool IsExpression { get; internal set; }
        public Update<T> Query { get; internal set; }
        public string ExpressionValue { get; internal set; }

        public Update<T> EqualTo(TValue value)
        {
            Value = value;
            Query.SetStatements.Add(this);
            return Query;
        }

        public Update<T> EqualToExpression(string value)
        {
            ExpressionValue = value;
            IsExpression = true;
            Query.SetStatements.Add(this);
            return Query;
        }
    }
}