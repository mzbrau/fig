using Fig.Client.Exceptions;
using Microsoft.Data.SqlClient;

namespace Fig.Integration.SqlLookupTableService;

public class SqlQueryManager : ISqlQueryManager
{
    private readonly ILogger<SqlQueryManager> _logger;
    private readonly ISettings _settings;

    public SqlQueryManager(ILogger<SqlQueryManager> logger, ISettings settings)
    {
        _logger = logger;
        _settings = settings;
    }

    public async Task<Dictionary<string, string>> ExecuteQuery(string queryString)
    {
        var result = new Dictionary<string, string>();

        await using var connection = new SqlConnection(string.Format(_settings.DatabaseConnectionString!,
            _settings.ConnectionStringPassword));

        try
        {
            await connection.OpenAsync();

            await using var command = new SqlCommand(queryString, connection);
            await using var reader = await command.ExecuteReaderAsync();
            while (reader.Read())
            {
                result.Add(reader.GetString(0), reader.GetString(1));
            }
        }
        catch (ArgumentException)
        {
            throw new ConfigurationException("Query returned duplicate keys");
        }
        finally
        {
            await connection.CloseAsync();
        }
        
        return result;
    }
}