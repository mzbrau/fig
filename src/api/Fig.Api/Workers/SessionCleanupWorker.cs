using Fig.Api.Services;
using Fig.Common.Timer;

namespace Fig.Api.Workers;

/// <summary>
/// Background service that performs scheduled cleanup of expired client run sessions.
/// Runs more frequently than DataCleanupWorker to ensure timely session cleanup without
/// race conditions from concurrent client status sync requests.
/// </summary>
public class SessionCleanupWorker : BackgroundService
{
    private readonly Random _random = new();
    private readonly ILogger<SessionCleanupWorker> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IPeriodicTimer _timer;

    public SessionCleanupWorker(
        ILogger<SessionCleanupWorker> logger,
        ITimerFactory timerFactory,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        // Run every 1-5 seconds (reduced from minutes for faster cleanup and test compatibility)
        var cleanupInterval = TimeSpan.FromSeconds(_random.Next(1, 5));
        _logger.LogInformation("Session cleanup worker will run every {Interval} seconds", cleanupInterval.TotalSeconds);
        _timer = timerFactory.Create(cleanupInterval);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Session cleanup worker starting");
        
        // Run cleanup on startup after a short delay (reduced for faster response in tests)
        await Task.Delay(TimeSpan.FromMilliseconds(_random.Next(100, 500)), stoppingToken); // Random delay between 100ms and 500ms
        await PerformCleanup();

        // Then run periodically
        while (await _timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            await PerformCleanup();
        }
    }

    private async Task PerformCleanup()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        
        try
        {
            var sessionCleanupService = scope.ServiceProvider.GetRequiredService<ISessionCleanupService>();
            await sessionCleanupService.RemoveExpiredSessionsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during session cleanup operation");
        }
    }
}
