using System.Threading.Tasks;

namespace SQLite.WinRT.Query
{
    public interface ISqlQuery
    {
        string BuildSqlStatement();
        int Execute();
        Task<int> ExecuteAsync();
    }
}
