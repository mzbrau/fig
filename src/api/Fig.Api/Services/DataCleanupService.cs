using Fig.Api.Datalayer.Repositories;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Services;

/// <summary>
/// Service for performing data cleanup operations on old records in the database.
/// Removes expired data based on configured retention periods.
/// </summary>
public class DataCleanupService : IDataCleanupService
{
    private readonly ILogger<DataCleanupService> _logger;
    private readonly IConfigurationRepository _configurationRepository;
    private readonly ICheckPointRepository _checkPointRepository;
    private readonly ICheckPointDataRepository _checkPointDataRepository;
    private readonly IEventLogRepository _eventLogRepository;
    private readonly IApiStatusRepository _apiStatusRepository;
    private readonly ISettingHistoryRepository _settingHistoryRepository;

    public DataCleanupService(
        ILogger<DataCleanupService> logger,
        IConfigurationRepository configurationRepository,
        ICheckPointRepository checkPointRepository,
        ICheckPointDataRepository checkPointDataRepository,
        IEventLogRepository eventLogRepository,
        IApiStatusRepository apiStatusRepository,
        ISettingHistoryRepository settingHistoryRepository)
    {
        _logger = logger;
        _configurationRepository = configurationRepository;
        _checkPointRepository = checkPointRepository;
        _checkPointDataRepository = checkPointDataRepository;
        _eventLogRepository = eventLogRepository;
        _apiStatusRepository = apiStatusRepository;
        _settingHistoryRepository = settingHistoryRepository;
    }

    public async Task<int> PerformCleanupAsync()
    {
        _logger.LogInformation("Starting data cleanup operation");
        
        var configuration = await _configurationRepository.GetConfiguration();

        // Check if any cleanup is configured (any non-null cleanup days value)
        var hasCleanupConfigured = configuration.TimeMachineCleanupDays.HasValue ||
                                   configuration.EventLogsCleanupDays.HasValue ||
                                   configuration.ApiStatusCleanupDays.HasValue ||
                                   configuration.SettingHistoryCleanupDays.HasValue;
        
        if (!hasCleanupConfigured)
        {
            _logger.LogDebug("Data cleanup is disabled - no cleanup days configured");
            return 0;
        }

        var now = DateTime.UtcNow;
        var totalDeleted = 0;

        totalDeleted += await CleanUpTimeMachineRecords(configuration, now);
        totalDeleted += await CleanUpEventLogs(configuration, now);
        totalDeleted += await CleanUpApiStatus(configuration, now);
        totalDeleted += await CleanUpSettingHistory(configuration, now);

        _logger.LogInformation("Data cleanup completed successfully. Total records deleted: {TotalDeleted}", totalDeleted);
        
        return totalDeleted;
    }

    private async Task<int> CleanUpTimeMachineRecords(FigConfigurationBusinessEntity configuration, DateTime now)
    {
        // Clean up time machine data (checkpoint and checkpoint_data)
        if (configuration.TimeMachineCleanupDays is > 0)
        {
            var cutoffDate = now.AddDays(-configuration.TimeMachineCleanupDays.Value);
            _logger.LogInformation("Cleaning up time machine data older than {CutoffDate}", cutoffDate);
                    
            // Delete checkpoint data first (child records)
            var dataDeleted = await _checkPointDataRepository.DeleteOlderThan(cutoffDate);
            // Then delete checkpoints (parent records)
            var checkpointsDeleted = await _checkPointRepository.DeleteOlderThan(cutoffDate);
            
            _logger.LogInformation("Deleted {CheckpointsDeleted} checkpoints and {DataDeleted} checkpoint data records", 
                checkpointsDeleted, dataDeleted);
            
            return dataDeleted + checkpointsDeleted;
        }

        return 0;
    }

    private async Task<int> CleanUpEventLogs(FigConfigurationBusinessEntity configuration, DateTime now)
    {
        // Clean up event logs
        if (configuration.EventLogsCleanupDays is > 0)
        {
            var cutoffDate = now.AddDays(-configuration.EventLogsCleanupDays.Value);
            _logger.LogInformation("Cleaning up event logs older than {CutoffDate}", cutoffDate);
                    
            var deleted = await _eventLogRepository.DeleteOlderThan(cutoffDate);

            _logger.LogInformation("Deleted {Deleted} event log records", deleted);
            return deleted;
        }

        return 0;
    }
    
    private async Task<int> CleanUpApiStatus(FigConfigurationBusinessEntity configuration, DateTime now)
    {
        // Clean up API status
        if (configuration.ApiStatusCleanupDays is > 0)
        {
            var cutoffDate = now.AddDays(-configuration.ApiStatusCleanupDays.Value);
            _logger.LogInformation("Cleaning up API status records older than {CutoffDate}", cutoffDate);
                    
            var deleted = await _apiStatusRepository.DeleteOlderThan(cutoffDate);

            _logger.LogInformation("Deleted {Deleted} API status records", deleted);

            return deleted;
        }

        return 0;
    }
    
    private async Task<int> CleanUpSettingHistory(FigConfigurationBusinessEntity configuration, DateTime now)
    {
        // Clean up setting history
        if (configuration.SettingHistoryCleanupDays is > 0)
        {
            var cutoffDate = now.AddDays(-configuration.SettingHistoryCleanupDays.Value);
            _logger.LogInformation("Cleaning up setting history older than {CutoffDate}", cutoffDate);
                    
            var deleted = await _settingHistoryRepository.DeleteOlderThan(cutoffDate);

            _logger.LogInformation("Deleted {Deleted} setting history records", deleted);

            return deleted;
        }
        
        return 0;
    }
}
