namespace Fig.Api.Services;

public class HashValidationCacheCleanupService : BackgroundService
{
    private readonly ILogger<HashValidationCacheCleanupService> _logger;
    private readonly IHashValidationCache _hashValidationCache;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(10);

    public HashValidationCacheCleanupService(
        ILogger<HashValidationCacheCleanupService> logger,
        IHashValidationCache hashValidationCache)
    {
        _logger = logger;
        _hashValidationCache = hashValidationCache;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Hash Validation Cache Cleanup Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_cleanupInterval, stoppingToken);
                
                _logger.LogDebug("Running hash validation cache cleanup");
                _hashValidationCache.CleanupExpiredEntries();
                _logger.LogDebug("Hash validation cache cleanup completed");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown, don't log as error
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during hash validation cache cleanup");
                // Continue running even if cleanup fails
            }
        }

        _logger.LogInformation("Hash Validation Cache Cleanup Service stopped");
    }
}
