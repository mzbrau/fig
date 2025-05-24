using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Fig.Integration.SqlLookupTableService;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly ILogger<DatabaseHealthCheck> _logger;
    private readonly IOptionsMonitor<Settings> _settings;

    public DatabaseHealthCheck(ILogger<DatabaseHealthCheck> logger, IOptionsMonitor<Settings> settings)
    {
        _logger = logger;
        _settings = settings;
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        await using var connection = new SqlConnection(string.Format(_settings.CurrentValue.DatabaseConnectionString!,
            _settings.CurrentValue.ConnectionStringPassword));

        try
        {
            await connection.OpenAsync(cancellationToken);
            await connection.CloseAsync();
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to database");
            return HealthCheckResult.Unhealthy("Failed to connect to database", ex);
        }
    }
}