using Fig.Api.Constants;
using Fig.Api.Services;
using Fig.Common.Events;

namespace Fig.Api.Utils;

public class CheckpointWorker : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<CheckpointWorker> _logger;

    public CheckpointWorker(IEventDistributor eventDistributor, IServiceScopeFactory serviceScopeFactory, ILogger<CheckpointWorker> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;

        eventDistributor.Subscribe<string>(EventConstants.CheckPointRequired, CreateCheckPoint);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }
    
    private void CreateCheckPoint(string message)
    {
        _logger.LogInformation("Queueing a checkpoint creation with message {Message}", message);
        Task.Run(() =>
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var timeMachineService = scope.ServiceProvider.GetService<ITimeMachineService>();
                if (timeMachineService is null)
                    throw new InvalidOperationException("Unable to find TimeMachine service");

                timeMachineService.CreateCheckPoint(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating checkpoint");
            }
        });
    }
}