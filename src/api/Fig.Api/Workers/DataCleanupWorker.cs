using Fig.Api.Services;
using Fig.Common.Timer;
using Microsoft.Extensions.Options;

namespace Fig.Api.Workers;

/// <summary>
/// Background service that performs scheduled cleanup of old data from the database.
/// </summary>
public class DataCleanupWorker : BackgroundService
{
    private readonly Random _random = new ();
    private readonly ILogger<DataCleanupWorker> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IOptionsMonitor<ApiSettings> _settings;
    private readonly IPeriodicTimer _timer;
    

    public DataCleanupWorker(
        ILogger<DataCleanupWorker> logger,
        ITimerFactory timerFactory,
        IServiceScopeFactory serviceScopeFactory,
        IOptionsMonitor<ApiSettings> settings)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _settings = settings;
        var cleanupInterval = TimeSpan.FromMinutes(_random.Next(480, 720)); // Random interval between 8-12 hours
        _logger.LogInformation("Data cleanup worker will run every {Interval} minutes", cleanupInterval.TotalMinutes);
        _timer = timerFactory.Create(cleanupInterval); 
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_settings.CurrentValue.DisableDataCleanupWorker)
        {
            _logger.LogInformation("Data cleanup worker disabled by configuration");
            return;
        }

        _logger.LogInformation("Data cleanup worker starting");
        
        // Run cleanup on startup after a short delay
        await Task.Delay(TimeSpan.FromSeconds(_random.Next(30, 300)), stoppingToken); // Random delay between 30s and 5min
        if (_settings.CurrentValue.DisableDataCleanupWorker)
            return;

        await PerformCleanup();

        // Then run twice per day at midnight and noon UTC
        while (await _timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            if (!_settings.CurrentValue.DisableDataCleanupWorker)
                await PerformCleanup();
        }
    }

    private async Task PerformCleanup()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        
        try
        {
            var cleanupService = scope.ServiceProvider.GetRequiredService<IDataCleanupService>();
            await cleanupService.PerformCleanupAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during data cleanup operation");
        }
    }
}
