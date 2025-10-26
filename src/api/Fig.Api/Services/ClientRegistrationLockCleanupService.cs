namespace Fig.Api.Services;

public class ClientRegistrationLockCleanupService : BackgroundService
{
    private readonly ILogger<ClientRegistrationLockCleanupService> _logger;
    private readonly IClientRegistrationLockService _lockService;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(10);

    public ClientRegistrationLockCleanupService(
        ILogger<ClientRegistrationLockCleanupService> logger,
        IClientRegistrationLockService lockService)
    {
        _logger = logger;
        _lockService = lockService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Client Registration Lock Cleanup Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_cleanupInterval, stoppingToken);
                
                _logger.LogDebug("Running client registration lock cleanup");
                _lockService.CleanupUnusedLocks();
                _logger.LogDebug("Client registration lock cleanup completed");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown, don't log as error
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during client registration lock cleanup");
                // Continue running even if cleanup fails
            }
        }

        _logger.LogInformation("Client Registration Lock Cleanup Service stopped");
    }
}
