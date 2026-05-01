using Fig.Api.Services;
using Fig.Common.Timer;
using Microsoft.Extensions.Options;

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
    private readonly IOptionsMonitor<ApiSettings> _settings;
    private readonly IPeriodicTimer _timer;

    public SessionCleanupWorker(
        ILogger<SessionCleanupWorker> logger,
        ITimerFactory timerFactory,
        IServiceScopeFactory serviceScopeFactory,
        IOptionsMonitor<ApiSettings> settings)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _settings = settings;
        // Run every 1-5 minutes with random interval to avoid synchronized load
        var cleanupInterval = TimeSpan.FromSeconds(_random.Next(60, 300));
        _logger.LogInformation("Session cleanup worker will run every {Interval} seconds", cleanupInterval.TotalSeconds);
        _timer = timerFactory.Create(cleanupInterval);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_settings.CurrentValue.DisableSessionCleanupWorker)
        {
            _logger.LogInformation("Session cleanup worker disabled by configuration");
            return;
        }

        _logger.LogInformation("Session cleanup worker starting");
        
        // Run cleanup on startup after a short delay
        await Task.Delay(TimeSpan.FromSeconds(_random.Next(10, 60)), stoppingToken); // Random delay between 10s and 1min
        if (_settings.CurrentValue.DisableSessionCleanupWorker)
            return;

        await PerformCleanup();

        // Then run periodically
        while (await _timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            if (!_settings.CurrentValue.DisableSessionCleanupWorker)
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
