using System.Collections;

namespace SQLite.WinRT.Query
{
    public class Constraint<T, TValue> : IConstraint
    {
        private readonly ConstraintType condition;
        private readonly string constraintColumnName;
        protected readonly SqlQuery<T> query;

        public Constraint(ConstraintType condition, string constraintColumnName, SqlQuery<T> query)
        {
            this.condition = condition;
            this.constraintColumnName = constraintColumnName;
            this.query = query;
        }

        public TValue ParameterValue { get; internal set; }
        public TValue StartValue { get; internal set; }
        public TValue EndValue { get; internal set; }

        public Comparison Comparison { get; internal set; }

        object IConstraint.ParameterValue { get { return ParameterValue; } }
        object IConstraint.StartValue { get { return StartValue; } }
        object IConstraint.EndValue { get { return EndValue; } }
        public IEnumerable InValues { get; set; }

        public string ColumnName
        {
            get { return constraintColumnName; }
        }

        public ConstraintType Condition
        {
            get { return condition; }
        }

        public SqlQuery<T> IsGreaterThan(TValue val)
        {
            Comparison = Comparison.GreaterThan;
            ParameterValue = val;
            query.Constraints.Add(this);
            return query;
        }

        public SqlQuery<T> IsGreaterThanOrEqualTo(TValue val)
        {
            Comparison = Comparison.GreaterOrEquals;
            ParameterValue = val;
            query.Constraints.Add(this);
            return query;
        }

        public SqlQuery<T> In(IEnumerable vals)
        {
            InValues = vals;
            Comparison = Comparison.In;
            query.Constraints.Add(this);
            return query;
        }

        public SqlQuery<T> In(params TValue[] vals)
        {
            InValues = vals;
            Comparison = Comparison.In;
            query.Constraints.Add(this);
            return query;
        }

        public SqlQuery<T> NotIn(IEnumerable vals)
        {
            InValues = vals;
            Comparison = Comparison.NotIn;
            query.Constraints.Add(this);
            return query;
        }

        public SqlQuery<T> NotIn(params TValue[] vals)
        {
            InValues = vals;
            Comparison = Comparison.NotIn;
            query.Constraints.Add(this);
            return query;
        }

        public SqlQuery<T> IsLessThan(TValue val)
        {
            Comparison = Comparison.LessThan;
            ParameterValue = val;
            query.Constraints.Add(this);
            return query;
        }

        public SqlQuery<T> IsLessThanOrEqualTo(TValue val)
        {
            Comparison = Comparison.LessOrEquals;
            ParameterValue = val;
            query.Constraints.Add(this);
            return query;
        }

        public SqlQuery<T> IsNotNull()
        {
            Comparison = Comparison.IsNot;
            ParameterValue = default(TValue);
            query.Constraints.Add(this);
            return query;
        }

        public SqlQuery<T> IsNull()
        {
            Comparison = Comparison.Is;
            ParameterValue = default(TValue);
            query.Constraints.Add(this);
            return query;
        }

        public SqlQuery<T> IsBetweenAnd(TValue start, TValue end)
        {
            Comparison = Comparison.BetweenAnd;
            StartValue = start;
            EndValue = end;
            query.Constraints.Add(this);
            return query;
        }

        public SqlQuery<T> IsEqualTo(TValue val)
        {
            Comparison = Comparison.Equals;
            ParameterValue = val;
            query.Constraints.Add(this);
            return query;
        }

        public SqlQuery<T> IsNotEqualTo(TValue val)
        {
            Comparison = Comparison.NotEquals;
            ParameterValue = val;
            query.Constraints.Add(this);
            return query;
        }
    }
}