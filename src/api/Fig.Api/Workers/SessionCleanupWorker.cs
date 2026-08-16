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
    private readonly TimeSpan _startupDelay;

    public SessionCleanupWorker(
        ILogger<SessionCleanupWorker> logger,
        ITimerFactory timerFactory,
        IServiceScopeFactory serviceScopeFactory)
        : this(logger, timerFactory, serviceScopeFactory, startupDelay: null, cleanupInterval: null)
    {
    }

    internal SessionCleanupWorker(
        ILogger<SessionCleanupWorker> logger,
        ITimerFactory timerFactory,
        IServiceScopeFactory serviceScopeFactory,
        TimeSpan? startupDelay,
        TimeSpan? cleanupInterval)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _startupDelay = startupDelay ?? TimeSpan.FromSeconds(_random.Next(10, 60)); // Random delay between 10s and 1min
        // Run every 1-5 minutes with random interval to avoid synchronized load
        var interval = cleanupInterval ?? TimeSpan.FromSeconds(_random.Next(60, 300));
        _logger.LogInformation("Session cleanup worker will run every {Interval} seconds", interval.TotalSeconds);
        _timer = timerFactory.Create(interval);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Session cleanup worker starting");
        
        // Run cleanup on startup after a short delay
        await Task.Delay(_startupDelay, stoppingToken);
        await PerformCleanup();

        // Then run periodically
        while (await _timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            await PerformCleanup();
        }
    }

    internal async Task PerformCleanup()
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
