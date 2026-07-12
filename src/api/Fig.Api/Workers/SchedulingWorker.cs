using Fig.Api.Datalayer.Repositories;
using Fig.Api.Services;
using Fig.Api.Utils;
using Microsoft.Extensions.Options;

namespace Fig.Api.Workers;

public class SchedulingWorker : BackgroundService
{
    private const long DefaultInterval = 60000;
    private readonly ILogger<SchedulingWorker> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IOptionsMonitor<ApiSettings> _settings;

    public SchedulingWorker(ILogger<SchedulingWorker> logger,
        IOptionsMonitor<ApiSettings> settings,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _settings = settings;
        _serviceScopeFactory = serviceScopeFactory;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting scheduling worker with interval {Interval}ms", GetIntervalMs());

        await EvaluateDeferredChanges();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(GetIntervalMs()), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            await EvaluateDeferredChanges();
        }
    }

    private long GetIntervalMs()
    {
        var configured = _settings.CurrentValue.SchedulingCheckIntervalMs;
        return configured == 0 ? DefaultInterval : configured;
    }

    internal long GetIntervalMsForTests() => GetIntervalMs();

    private async Task EvaluateDeferredChanges()
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var deferredChangeRepository = scope.ServiceProvider.GetRequiredService<IDeferredChangeRepository>();

            var changesToExecute = (await deferredChangeRepository.GetChangesToExecute(DateTime.UtcNow)).ToList();

            if (changesToExecute.Any())
            {
                var settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();
                settingsService.SetAuthenticatedUser(new ServiceUser());
            
                foreach (var change in changesToExecute.Where(c => c.ChangeSet is not null && c.Id is not null))
                {
                    try
                    {
                        _logger.LogInformation("Applying scheduled change for client {ClientName}, instance {Instance}", 
                            change.ClientName, change.Instance);

                        if (change.ChangeSet?.Schedule?.ApplyAtUtc is not null)
                        {
                            change.ChangeSet!.Schedule!.ApplyAtUtc = null;
                            await deferredChangeRepository.UpdateDeferredChange(change);
                        }
                        
                        await settingsService.UpdateSettingValues(change.ClientName, change.Instance, change.ChangeSet!);
                        await deferredChangeRepository.Remove(change.Id!.Value);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error applying scheduled change for client {ClientName}, instance {Instance}",
                            change.ClientName, change.Instance);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating deferred changes");
        }
    }
}
