using Fig.Api.Datalayer.Repositories;
using Fig.Api.Services;
using Fig.Api.Utils;
using Fig.Common.Timer;
using Microsoft.Extensions.Options;

namespace Fig.Api.Scheduling;

public class SchedulingWorker : BackgroundService
{
    private readonly ILogger<SchedulingWorker> _logger;
    private readonly IOptions<ApiSettings> _settings;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IPeriodicTimer _timer;

    public SchedulingWorker(ILogger<SchedulingWorker> logger,
        ITimerFactory timerFactory,
        IOptions<ApiSettings> settings,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _settings = settings;
        _serviceScopeFactory = serviceScopeFactory;
        _timer = timerFactory.Create(TimeSpan.FromMilliseconds(settings.Value.SchedulingCheckIntervalMs));
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting scheduling worker with interval {Interval}ms", _settings.Value.SchedulingCheckIntervalMs);
        while (await _timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
            await EvaluateDeferredChanges();
    }

    private async Task EvaluateDeferredChanges()
    {
        // Create a new scope for each evaluation cycle
        using var scope = _serviceScopeFactory.CreateScope();
        
        try
        {
            // Get all required services within this scope
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
                            change.ChangeSet!.Schedule!.ApplyAtUtc = null; // Otherwise we'll get an endless loop of schedules
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