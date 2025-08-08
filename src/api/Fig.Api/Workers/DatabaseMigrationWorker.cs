using Fig.Api.Services;

namespace Fig.Api.Workers;

/// <summary>
/// High priority background service that runs database migrations before other services start.
/// This service runs to completion before other background services are allowed to start.
/// </summary>
public class DatabaseMigrationWorker : BackgroundService
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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Database migration worker starting...");
            
            using var scope = _serviceScopeFactory.CreateScope();

            var databaseMigrationService = scope.ServiceProvider.GetRequiredService<IDatabaseMigrationService>();

            // Run migrations before allowing other services to start
            await databaseMigrationService.RunMigrationsAsync();
            
            _logger.LogInformation("Database migration worker completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Database migration worker failed. Application will shutdown");
            
            // Stop the application if migrations fail
            _applicationLifetime.StopApplication();
            throw;
        }
    }
}
