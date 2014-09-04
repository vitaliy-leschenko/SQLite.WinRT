using System.Collections;

namespace SQLite.WinRT.Query
{
    public class StringConstraint<T> : Constraint<T, string>, IConstraint
    {
        public StringConstraint(ConstraintType condition, string constraintColumnName, SqlQuery<T> query) : base(condition, constraintColumnName, query)
        {
        }

        public new string ParameterValue { get; internal set; }
        public new string StartValue { get; internal set; }
        public new string EndValue { get; internal set; }

        object IConstraint.ParameterValue { get { return ParameterValue; } }
        object IConstraint.StartValue { get { return StartValue; } }
        object IConstraint.EndValue { get { return EndValue; } }

        public SqlQuery<T> Like(string val)
        {
            Comparison = Comparison.Like;
            ParameterValue = val;
            query.Constraints.Add(this);
            return query;
        }

        public SqlQuery<T> NotLike(string val)
        {
            Comparison = Comparison.NotLike;
            ParameterValue = val;
            query.Constraints.Add(this);
            return query;
        }
    }
}