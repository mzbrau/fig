using Fig.Api.Constants;
using Fig.Api.Services;
using Fig.Common.Events;

namespace Fig.Api.Utils;

public class CheckpointWorker : BackgroundService
{
    private readonly IEventDistributor _eventDistributor;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public CheckpointWorker(IEventDistributor eventDistributor, IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
        
        eventDistributor.Subscribe<string>(EventConstants.CheckPointRequired, CreateCheckPoint);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }
    
    private void CreateCheckPoint(string message)
    {
        Task.Run(() =>
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var timeMachineService = scope.ServiceProvider.GetService<ITimeMachineService>();
            if (timeMachineService is null)
                throw new InvalidOperationException("Unable to find TimeMachine service");

            timeMachineService.CreateCheckPoint(message);
        });
    }
}