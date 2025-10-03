using Fig.Api.Services;
using Fig.Common.Timer;

namespace Fig.Api.Workers;

/// <summary>
/// Background service that performs scheduled cleanup of old data from the database.
/// Runs on startup and then twice per day at midnight and noon UTC.
/// Uses a locking mechanism to ensure only one API instance performs cleanup at a time.
/// </summary>
public class DataCleanupWorker : BackgroundService
{
    private readonly ILogger<DataCleanupWorker> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IPeriodicTimer _timer;
    private const int CheckIntervalMinutes = 60; // Check every hour

    public DataCleanupWorker(
        ILogger<DataCleanupWorker> logger,
        ITimerFactory timerFactory,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _timer = timerFactory.Create(TimeSpan.FromMinutes(CheckIntervalMinutes));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Data cleanup worker starting");
        
        // Run cleanup on startup after a short delay
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        await PerformCleanup();

        // Then run twice per day at midnight and noon UTC
        while (await _timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            // Run at midnight (00:00-00:59) or noon (12:00-12:59)
            if (now.Hour is 0 or 12 && now.Minute < CheckIntervalMinutes)
            {
                await PerformCleanup();
            }
        }
    }

    private async Task PerformCleanup()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        
        try
        {
            var lockService = scope.ServiceProvider.GetRequiredService<IDataCleanupLockService>();
            
            // Try to acquire lock
            if (!await lockService.TryAcquireLockAsync())
            {
                _logger.LogDebug("Could not acquire cleanup lock, skipping this cycle");
                return;
            }

            try
            {
                var cleanupService = scope.ServiceProvider.GetRequiredService<IDataCleanupService>();
                await cleanupService.PerformCleanupAsync();
            }
            finally
            {
                // Always release the lock
                await lockService.ReleaseLockAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during data cleanup operation");
        }
    }
}
