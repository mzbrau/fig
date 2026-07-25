using Fig.Api.Services;
using Fig.Common.Timer;

namespace Fig.Api.Workers;

/// <summary>
/// Keeps the newest Fig release highlight cache warm so GET /releasehighlights never blocks on GitHub.
/// </summary>
public class ReleaseDiscoveryWorker : BackgroundService
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromHours(1);

    private readonly ILogger<ReleaseDiscoveryWorker> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IPeriodicTimer _timer;

    public ReleaseDiscoveryWorker(
        ILogger<ReleaseDiscoveryWorker> logger,
        ITimerFactory timerFactory,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _timer = timerFactory.Create(RefreshInterval);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Release discovery worker starting");

        await RefreshDiscovery();

        while (await _timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            await RefreshDiscovery();
        }
    }

    private async Task RefreshDiscovery()
    {
        using var scope = _serviceScopeFactory.CreateScope();

        try
        {
            var discoveryService = scope.ServiceProvider.GetRequiredService<IFigReleaseDiscoveryService>();
            await discoveryService.RefreshAsync();
            _logger.LogDebug("Release discovery cache refreshed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing GitHub release discovery cache");
        }
    }
}
