using Fig.Api.Services;
using Fig.Common.Timer;

namespace Fig.Api.Workers;

/// <summary>
/// Background service that performs scheduled cleanup of old data from the database.
/// </summary>
public class DataCleanupWorker : BackgroundService
{
    private readonly Random _random = new ();
    private readonly ILogger<DataCleanupWorker> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IPeriodicTimer _timer;
    

    public DataCleanupWorker(
        ILogger<DataCleanupWorker> logger,
        ITimerFactory timerFactory,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        var cleanupInterval = TimeSpan.FromMinutes(_random.Next(480, 720)); // Random interval between 8-12 hours
        _logger.LogInformation("Data cleanup worker will run every {Interval} minutes", cleanupInterval.TotalMinutes);
        _timer = timerFactory.Create(cleanupInterval); 
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Data cleanup worker starting");
        
        // Run cleanup on startup after a short delay
        await Task.Delay(TimeSpan.FromSeconds(_random.Next(30, 300)), stoppingToken); // Random delay between 30s and 5min
        await PerformCleanup();

        // Then run twice per day at midnight and noon UTC
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
            var cleanupService = scope.ServiceProvider.GetRequiredService<IDataCleanupService>();
            await cleanupService.PerformCleanupAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during data cleanup operation");
        }
    }
}
