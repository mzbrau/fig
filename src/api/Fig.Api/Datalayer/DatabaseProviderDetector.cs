using System.Data.Common;

namespace Fig.Api.Datalayer;

/// <summary>
/// Detects the database provider from an optional explicit setting and/or connection string heuristics.
/// </summary>
public static class DatabaseProviderDetector
{
    /// <summary>
    /// Resolves the database provider.
    /// </summary>
    /// <param name="connectionString">The connection string to inspect.</param>
    /// <param name="explicitProvider">Optional override from ApiSettings.DatabaseProvider.</param>
    public static DatabaseProvider Detect(string? connectionString, DatabaseProvider? explicitProvider = null)
    {
        if (explicitProvider.HasValue)
            return explicitProvider.Value;

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

        if (IsSqlite(connectionString))
            return DatabaseProvider.Sqlite;

        if (IsPostgreSql(connectionString))
            return DatabaseProvider.PostgreSql;

        return DatabaseProvider.SqlServer;
    }

    public static bool IsSqlite(string connectionString)
    {
        var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };

        object? uri = null;
        if (builder.TryGetValue("Data Source", out var dataSource) ||
            builder.TryGetValue("URI", out uri))
        {
            var dataSourceValue = dataSource?.ToString() ?? uri?.ToString() ?? string.Empty;
            if (!string.IsNullOrEmpty(dataSourceValue) &&
                (dataSourceValue.EndsWith(".sqlite", StringComparison.OrdinalIgnoreCase) ||
                 dataSourceValue.EndsWith(".db", StringComparison.OrdinalIgnoreCase) ||
                 dataSourceValue.Equals(":memory:", StringComparison.OrdinalIgnoreCase) ||
                 dataSourceValue.StartsWith("file:", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsPostgreSql(string connectionString)
    {
        var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };

        // Restrict "postgres" matching to host-like keys so SQL Server catalogs,
        // passwords, or application names containing "postgres" are not misclassified.
        foreach (var key in new[] { "Host", "Server", "Data Source" })
        {
            if (builder.TryGetValue(key, out var hostValue) &&
                hostValue?.ToString()?.Contains("postgres", StringComparison.OrdinalIgnoreCase) == true)
            {
                return true;
            }
        }

        // Npgsql uses Host= (SQL Server uses Server= / Data Source=)
        if (builder.ContainsKey("Host"))
            return true;

        // Npgsql keyword; SQL Server typically uses "User Id" / "User ID"
        if (builder.ContainsKey("Username"))
            return true;

        if (builder.TryGetValue("Port", out var port) &&
            int.TryParse(port?.ToString(), out var portNumber) &&
            portNumber == 5432)
        {
            return true;
        }

        return false;
    }
}
