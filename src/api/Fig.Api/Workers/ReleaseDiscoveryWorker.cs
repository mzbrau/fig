using Fig.Api.Services;
using Fig.Common.Timer;
using Microsoft.Extensions.Options;

namespace Fig.Api.Workers;

/// <summary>
/// Keeps the newest Fig release highlight cache warm so GET /releasehighlights never blocks on GitHub.
/// </summary>
public class ReleaseDiscoveryWorker : BackgroundService
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromHours(24);

    private readonly ILogger<ReleaseDiscoveryWorker> _logger;
    private readonly IOptionsMonitor<ApiSettings> _settings;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IPeriodicTimer _timer;

    public ReleaseDiscoveryWorker(
        ILogger<ReleaseDiscoveryWorker> logger,
        IOptionsMonitor<ApiSettings> settings,
        ITimerFactory timerFactory,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _settings = settings;
        _serviceScopeFactory = serviceScopeFactory;
        _timer = timerFactory.Create(RefreshInterval);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.CurrentValue.EnableGitHubReleaseDiscovery)
        {
            _logger.LogInformation("Release discovery worker is disabled because EnableGitHubReleaseDiscovery is false");
            return;
        }

        _logger.LogInformation("Release discovery worker starting");

        await RefreshDiscovery();

        while (await _timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            await RefreshDiscovery();
        }
    }

    public override void Dispose()
    {
        _timer.Dispose();
        base.Dispose();
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
