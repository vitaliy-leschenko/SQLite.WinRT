namespace SQLite.WinRT.Query
{
    public static class ConstraintHelper
    {
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