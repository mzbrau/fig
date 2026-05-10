using Fig.Client;
using Fig.Client.Exceptions;
using Microsoft.Extensions.Hosting;

namespace Fig.Examples.AspNetApi;

public sealed class CurrentTimeUpdater : BackgroundService
{
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(90);
    private readonly ISettingUpdater<Settings> _settingUpdater;
    private readonly ILogger<CurrentTimeUpdater> _logger;

    public CurrentTimeUpdater(ISettingUpdater<Settings> settingUpdater, ILogger<CurrentTimeUpdater> logger)
    {
        _settingUpdater = settingUpdater;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting current time updater with {IntervalSeconds} second interval", UpdateInterval.TotalSeconds);

        await UpdateCurrentTime(stoppingToken);

        using var timer = new PeriodicTimer(UpdateInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await UpdateCurrentTime(stoppingToken);
        }
    }

    private async Task UpdateCurrentTime(CancellationToken stoppingToken)
    {
        var currentTimestamp = DateTime.UtcNow;

        try
        {
            await _settingUpdater
                .Set(s => s.CurrentTime, currentTimestamp)
                .WithMessage("AspNetApi example pushed the current timestamp")
                .ApplyAsync()
                .WaitAsync(stoppingToken);

            _logger.LogDebug("Updated CurrentTime setting to {CurrentTimestamp}", currentTimestamp);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (ConfigurationException ex)
        {
            _logger.LogWarning(ex, "CurrentTime update skipped because Fig is not initialized yet");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update the CurrentTime setting");
        }
    }
}
