using System.ComponentModel;
using Fig.Api.Services;
using Fig.Common.Timer;
using Microsoft.Extensions.Options;

namespace Fig.Api.Scheduling;

public class SchedulingWorker : BackgroundService
{
    private readonly ILogger<SchedulingWorker> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IPeriodicTimer _timer;

    public SchedulingWorker(ILogger<SchedulingWorker> logger,
        ITimerFactory timerFactory,
        IOptionsMonitor<ApiSettings> settings,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _timer = timerFactory.Create(TimeSpan.FromMilliseconds(settings.CurrentValue.SchedulingCheckIntervalMs));
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await _timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
            await EvaluateDeferredChanges();
    }

    private async Task EvaluateDeferredChanges()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var schedulingService = scope.ServiceProvider.GetService<ISchedulingService>();
        if (schedulingService is null)
            throw new InvalidOperationException("Unable to find Scheduling Service");

        await schedulingService.ExecuteDueChanges();
    }
}