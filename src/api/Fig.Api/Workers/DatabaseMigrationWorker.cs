using Fig.Api.Services;

namespace Fig.Api.Workers;

/// <summary>
/// High priority background service that runs database migrations before other services start.
/// This service runs to completion before other background services are allowed to start.
/// </summary>
public class DatabaseMigrationWorker : IHostedService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<DatabaseMigrationWorker> _logger;
    private readonly IHostApplicationLifetime _applicationLifetime;

    public DatabaseMigrationWorker(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<DatabaseMigrationWorker> logger,
        IHostApplicationLifetime applicationLifetime)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _applicationLifetime = applicationLifetime;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("DatabaseMigrationWorker starting - running database migrations synchronously before other services...");
            using var scope = _serviceScopeFactory.CreateScope();
            var databaseMigrationService = scope.ServiceProvider.GetRequiredService<IDatabaseMigrationService>();
            await databaseMigrationService.RunMigrationsAsync();
            _logger.LogInformation("DatabaseMigrationWorker completed successfully - other services can now start");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "DatabaseMigrationWorker failed. Application will shutdown");
            _applicationLifetime.StopApplication();
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // Nothing to stop; migrations run only at startup
        return Task.CompletedTask;
    }
}
