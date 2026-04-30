using System.Threading.Channels;
using Fig.Api.Constants;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Utils;
using Fig.Common.Events;
using Fig.Datalayer.BusinessEntities;
using Microsoft.Extensions.DependencyInjection;

namespace Fig.Api.Workers;

public class CheckpointTriggerWorker : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<CheckpointTriggerWorker> _logger;
    private readonly Channel<CheckPointTrigger> _channel = Channel.CreateUnbounded<CheckPointTrigger>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });

    public CheckpointTriggerWorker(IEventDistributor eventDistributor, IServiceScopeFactory serviceScopeFactory, ILogger<CheckpointTriggerWorker> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;

        eventDistributor.Subscribe<CheckPointTrigger>(EventConstants.CheckPointTrigger, AddCheckPointTrigger);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var trigger in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await Task.Delay(50, stoppingToken);

                using var scope = _serviceScopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<ICheckPointTriggerRepository>();

                var triggerBusinessEntity = new CheckPointTriggerBusinessEntity
                {
                    AfterEvent = trigger.Message,
                    Timestamp = DateTime.UtcNow,
                    User = trigger.User
                };

                await repository.AddTrigger(triggerBusinessEntity);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating checkpoint trigger");
            }
        }
    }

    private Task AddCheckPointTrigger(CheckPointTrigger trigger)
    {
        if (_channel.Writer.TryWrite(trigger))
        {
            _logger.LogInformation("Queued a checkpoint creation with message {Message}", trigger.Message);
        }
        else
        {
            _logger.LogWarning("Dropped checkpoint creation with message {Message} because the worker is stopping", trigger.Message);
        }

        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _channel.Writer.TryComplete();
        return base.StopAsync(cancellationToken);
    }
}
