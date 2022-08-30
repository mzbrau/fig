namespace Fig.Integration.SqlLookupTableService;

public interface ISqlQueryManager
{
    Task<Dictionary<string, string>> ExecuteQuery(string queryString);
}