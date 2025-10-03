using Fig.Api.Datalayer.Repositories;
using Fig.Api.Services;
using Fig.Common.Timer;
using Fig.Datalayer.BusinessEntities;

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
                _logger.LogInformation("Starting data cleanup operation");
                
                var configurationRepository = scope.ServiceProvider.GetRequiredService<IConfigurationRepository>();
                var configuration = await configurationRepository.GetConfiguration();

                // Check if any cleanup is configured (any non-null cleanup days value)
                var hasCleanupConfigured = configuration.TimeMachineCleanupDays.HasValue ||
                                           configuration.EventLogsCleanupDays.HasValue ||
                                           configuration.ApiStatusCleanupDays.HasValue ||
                                           configuration.SettingHistoryCleanupDays.HasValue;
                
                if (!hasCleanupConfigured)
                {
                    _logger.LogDebug("Data cleanup is disabled - no cleanup days configured");
                    return;
                }

                var now = DateTime.UtcNow;
                var totalDeleted = 0;

                totalDeleted += await CleanUpTimeMachineRecords(configuration, scope, now);
                totalDeleted += await CleanUpEventLogs(configuration, scope, now);
                totalDeleted += await CleanUpApiStatus(configuration, scope, now);
                totalDeleted += await CleanUpSettingHistory(configuration, scope, now);

                _logger.LogInformation("Data cleanup completed successfully. Total records deleted: {TotalDeleted}", totalDeleted);
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

    private async Task<int> CleanUpTimeMachineRecords(FigConfigurationBusinessEntity configuration, IServiceScope scope, DateTime now)
    {
        // Clean up time machine data (checkpoint and checkpoint_data)
        if (configuration.TimeMachineCleanupDays is > 0)
        {
            var cutoffDate = now.AddDays(-configuration.TimeMachineCleanupDays.Value);
            _logger.LogInformation("Cleaning up time machine data older than {CutoffDate}", cutoffDate);
                    
            var checkPointRepository = scope.ServiceProvider.GetRequiredService<ICheckPointRepository>();
            var checkPointDataRepository = scope.ServiceProvider.GetRequiredService<ICheckPointDataRepository>();
                    
            // Delete checkpoint data first (child records)
            var dataDeleted = await checkPointDataRepository.DeleteOlderThan(cutoffDate);
            // Then delete checkpoints (parent records)
            var checkpointsDeleted = await checkPointRepository.DeleteOlderThan(cutoffDate);
            
            _logger.LogInformation("Deleted {CheckpointsDeleted} checkpoints and {DataDeleted} checkpoint data records", 
                checkpointsDeleted, dataDeleted);
            
            return dataDeleted + checkpointsDeleted;
        }

        return 0;
    }

    private async Task<int> CleanUpEventLogs(FigConfigurationBusinessEntity configuration, IServiceScope scope, DateTime now)
    {
        // Clean up event logs
        if (configuration.EventLogsCleanupDays is > 0)
        {
            var cutoffDate = now.AddDays(-configuration.EventLogsCleanupDays.Value);
            _logger.LogInformation("Cleaning up event logs older than {CutoffDate}", cutoffDate);
                    
            var eventLogRepository = scope.ServiceProvider.GetRequiredService<IEventLogRepository>();
            var deleted = await eventLogRepository.DeleteOlderThan(cutoffDate);

            _logger.LogInformation("Deleted {Deleted} event log records", deleted);
            return deleted;
        }

        return 0;
    }
    
    private async Task<int> CleanUpApiStatus(FigConfigurationBusinessEntity configuration, IServiceScope scope, DateTime now)
    {
        // Clean up API status
        if (configuration.ApiStatusCleanupDays is > 0)
        {
            var cutoffDate = now.AddDays(-configuration.ApiStatusCleanupDays.Value);
            _logger.LogInformation("Cleaning up API status records older than {CutoffDate}", cutoffDate);
                    
            var apiStatusRepository = scope.ServiceProvider.GetRequiredService<IApiStatusRepository>();
            var deleted = await apiStatusRepository.DeleteOlderThan(cutoffDate);

            _logger.LogInformation("Deleted {Deleted} API status records", deleted);

            return deleted;
        }

        return 0;
    }
    
    private async Task<int> CleanUpSettingHistory(FigConfigurationBusinessEntity configuration, IServiceScope scope, DateTime now)
    {
        // Clean up setting history
        if (configuration.SettingHistoryCleanupDays is > 0)
        {
            var cutoffDate = now.AddDays(-configuration.SettingHistoryCleanupDays.Value);
            _logger.LogInformation("Cleaning up setting history older than {CutoffDate}", cutoffDate);
                    
            var settingHistoryRepository = scope.ServiceProvider.GetRequiredService<ISettingHistoryRepository>();
            var deleted = await settingHistoryRepository.DeleteOlderThan(cutoffDate);

            _logger.LogInformation("Deleted {Deleted} setting history records", deleted);

            return deleted;
        }
        
        return 0;
    }
}
