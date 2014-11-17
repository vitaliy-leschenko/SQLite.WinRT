using System.Collections;

namespace SQLite.WinRT.Query
{
    public class StringConstraint<T> : Constraint<T, string>
    {
        public StringConstraint(ConstraintType condition, string constraintColumnName, SqlQuery<T> query)
            : base(condition, constraintColumnName, query)
        {
        }

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