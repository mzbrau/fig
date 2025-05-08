using Fig.Api.Constants;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Utils;
using Fig.Common.Events;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Workers;

public class CheckpointTriggerWorker : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<CheckpointTriggerWorker> _logger;

    public CheckpointTriggerWorker(IEventDistributor eventDistributor, IServiceScopeFactory serviceScopeFactory, ILogger<CheckpointTriggerWorker> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;

        eventDistributor.Subscribe<CheckPointTrigger>(EventConstants.CheckPointTrigger, AddCheckPointTrigger);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }

    private async Task AddCheckPointTrigger(CheckPointTrigger trigger)
    {
        _logger.LogInformation("Queueing a checkpoint creation with message {Message}", trigger.Message);
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetService<ICheckPointTriggerRepository>();
            if (repository is null)
                throw new InvalidOperationException("Unable to find checkpoint trigger repository");

            var triggerBusinessEntity = new CheckPointTriggerBusinessEntity
            {
                AfterEvent = trigger.Message,
                Timestamp = DateTime.UtcNow,
                User = trigger.User
            };

            await repository.AddTrigger(triggerBusinessEntity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating checkpoint trigger");
        }
    }
}