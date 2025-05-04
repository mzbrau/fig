using Fig.Api.Datalayer.Repositories;
using Fig.Api.Services;
using Fig.Api.Utils;
using Fig.Common.Timer;
using Microsoft.Extensions.Options;

namespace Fig.Api.Workers;

public class TimeMachineWorker : BackgroundService
{
    private const long DefaultInterval = 60000;
    private readonly ILogger<TimeMachineWorker> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IPeriodicTimer _timer;
    private readonly long _interval;

    public TimeMachineWorker(ILogger<TimeMachineWorker> logger,
        ITimerFactory timerFactory,
        IOptions<ApiSettings> settings,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _interval = settings.Value.TimeMachineCheckIntervalMs == 0 ? DefaultInterval : settings.Value.TimeMachineCheckIntervalMs;
        _timer = timerFactory.Create(TimeSpan.FromMilliseconds(_interval));
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting time machine worker with interval {Interval}ms", _interval);
        while (await _timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
            await EvaluateCheckPointTriggers();
    }

    private async Task EvaluateCheckPointTriggers()
    {
        // Create a new scope for each evaluation cycle
        using var scope = _serviceScopeFactory.CreateScope();
        
        try
        {
            var checkPointTriggerRepository = scope.ServiceProvider.GetRequiredService<ICheckPointTriggerRepository>();

            await checkPointTriggerRepository.DeleteHandledTriggers();
            
            var triggersToHandle = (await checkPointTriggerRepository.GetUnhandledTriggers()).ToList();
            if (triggersToHandle.Any())
            {
                var timeMachineService = scope.ServiceProvider.GetRequiredService<ITimeMachineService>();

                var messages = string.Join(", ", triggersToHandle.Select(a => a.AfterEvent).Distinct());
                var users = string.Join(", ", triggersToHandle.Select(a => a.User).Distinct());
                
                var record = new CheckPointTrigger(messages, users);

                await timeMachineService.CreateCheckPoint(record);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating checkpoint triggers");
        }
    }
}