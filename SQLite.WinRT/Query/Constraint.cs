using System.Collections;

namespace SQLite.WinRT.Query
{
    public class Constraint
    {
        private readonly ConstraintType condition;
        private readonly string constraintColumnName;
        private readonly SqlQuery query;

        public Constraint(ConstraintType condition, string constraintColumnName, SqlQuery query)
        {
            this.condition = condition;
            this.constraintColumnName = constraintColumnName;
            this.query = query;
        }

        public Comparison Comparison { get; internal set; }
        public object ParameterValue { get; internal set; }
        public object StartValue { get; internal set; }
        public object EndValue { get; internal set; }
        public IEnumerable InValues { get; set; }

        public string ColumnName
        {
            get { return constraintColumnName; }
        }

        public ConstraintType Condition
        {
            get { return condition; }
        }

        public SqlQuery Like(string val)
        {
            Comparison = Comparison.Like;
            ParameterValue = val;
            query.Constraints.Add(this);
            return query;
        }

        public SqlQuery NotLike(string val)
        {
            Comparison = Comparison.NotLike;
            ParameterValue = val;
            query.Constraints.Add(this);
            return query;
        }

        public SqlQuery IsGreaterThan(object val)
        {
            Comparison = Comparison.GreaterThan;
            ParameterValue = val;
            query.Constraints.Add(this);
            return query;
        }

        public SqlQuery IsGreaterThanOrEqualTo(object val)
        {
            Comparison = Comparison.GreaterOrEquals;
            ParameterValue = val;
            query.Constraints.Add(this);
            return query;
        }

        public SqlQuery In(IEnumerable vals)
        {
            InValues = vals;
            Comparison = Comparison.In;
            query.Constraints.Add(this);
            return query;
        }

        public SqlQuery In(params object[] vals)
        {
            InValues = vals;
            Comparison = Comparison.In;
            query.Constraints.Add(this);
            return query;
        }

        public SqlQuery NotIn(IEnumerable vals)
        {
            InValues = vals;
            Comparison = Comparison.NotIn;
            query.Constraints.Add(this);
            return query;
        }

        public SqlQuery NotIn(params object[] vals)
        {
            InValues = vals;
            Comparison = Comparison.NotIn;
            query.Constraints.Add(this);
            return query;
        }

        public SqlQuery IsLessThan(object val)
        {
            Comparison = Comparison.LessThan;
            ParameterValue = val;
            query.Constraints.Add(this);
            return query;
        }

        public SqlQuery IsLessThanOrEqualTo(object val)
        {
            Comparison = Comparison.LessOrEquals;
            ParameterValue = val;
            query.Constraints.Add(this);
            return query;
        }

        public SqlQuery IsNotNull()
        {
            Comparison = Comparison.IsNot;
            ParameterValue = null;
            query.Constraints.Add(this);
            return query;
        }

        public SqlQuery IsNull()
        {
            Comparison = Comparison.Is;
            ParameterValue = null;
            query.Constraints.Add(this);
            return query;
        }

        public SqlQuery IsBetweenAnd(object val1, object val2)
        {
            Comparison = Comparison.BetweenAnd;
            StartValue = val1;
            EndValue = val2;
            query.Constraints.Add(this);
            return query;
        }

        public SqlQuery IsEqualTo(object val)
        {
            Comparison = Comparison.Equals;
            ParameterValue = null;
            query.Constraints.Add(this);
            return query;
        }

        public SqlQuery IsNotEqualTo(object val)
        {
            Comparison = Comparison.NotEquals;
            ParameterValue = null;
            query.Constraints.Add(this);
            return query;
        }

        public static string GetComparisonOperator(Comparison comp)
        {
            string sOut;
            switch (comp)
            {
                case Comparison.Blank:
                    sOut = SqlComparison.Blank;
                    break;
                case Comparison.GreaterThan:
                    sOut = SqlComparison.Greater;
                    break;
                case Comparison.GreaterOrEquals:
                    sOut = SqlComparison.GreaterOrEqual;
                    break;
                case Comparison.LessThan:
                    sOut = SqlComparison.Less;
                    break;
                case Comparison.LessOrEquals:
                    sOut = SqlComparison.LessOrEqual;
                    break;
                case Comparison.Like:
                    sOut = SqlComparison.Like;
                    break;
                case Comparison.NotEquals:
                    sOut = SqlComparison.NotEqual;
                    break;
                case Comparison.NotLike:
                    sOut = SqlComparison.NotLike;
                    break;
                case Comparison.Is:
                    sOut = SqlComparison.Is;
                    break;
                case Comparison.IsNot:
                    sOut = SqlComparison.IsNot;
                    break;
                case Comparison.OpenParentheses:
                    sOut = "(";
                    break;
                case Comparison.CloseParentheses:
                    sOut = ")";
                    break;
                case Comparison.In:
                    sOut = " IN ";
                    break;
                case Comparison.NotIn:
                    sOut = " NOT IN ";
                    break;
                case Comparison.StartsWith:
                    sOut = SqlComparison.Like;
                    break;
                case Comparison.EndsWith:
                    sOut = SqlComparison.Like;
                    break;
                default:
                    sOut = SqlComparison.Equal;
                    break;
            }
            return sOut;
        }
    }
}