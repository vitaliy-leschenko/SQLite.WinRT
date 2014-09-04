using System.Collections;

namespace SQLite.WinRT.Query
{
    public interface IConstraint
    {
        Comparison Comparison { get; }
        object ParameterValue { get; }
        object StartValue { get; }
        object EndValue { get; }
        IEnumerable InValues { get; set; }

        string ColumnName { get; }

        ConstraintType Condition { get; }
    }
}