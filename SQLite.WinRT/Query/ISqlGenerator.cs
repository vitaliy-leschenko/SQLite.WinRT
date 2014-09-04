using System.Collections.Generic;

namespace SQLite.WinRT.Query
{
    public interface ISqlGenerator
    {
        string BuildUpdateStatement();
        string BuildDeleteStatement();
        List<object> Parameters { get; }
    }
}
