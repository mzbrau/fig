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
    private readonly CancellationTokenSource _shutdownCts = new();
    private readonly CancellationTokenRegistration _shutdownRegistration;
    private readonly object _shutdownLock = new();
    private volatile bool _isStopping;
    private bool _disposed;

    public CheckpointTriggerWorker(
        IEventDistributor eventDistributor,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<CheckpointTriggerWorker> logger,
        IHostApplicationLifetime applicationLifetime)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _shutdownRegistration = applicationLifetime.ApplicationStopping.Register(RequestShutdown);

        eventDistributor.Subscribe<CheckPointTrigger>(EventConstants.CheckPointTrigger, AddCheckPointTrigger);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, _shutdownCts.Token);
        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, linkedCts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected during host shutdown.
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        RequestShutdown();
        return base.StopAsync(cancellationToken);
    }

    private Task AddCheckPointTrigger(CheckPointTrigger trigger)
    {
        if (_isStopping)
            return Task.CompletedTask;

        var shutdownToken = _shutdownCts.Token;
        if (shutdownToken.IsCancellationRequested)
            return Task.CompletedTask;

        _logger.LogInformation("Queueing a checkpoint creation with message {Message}", trigger.Message);

        // Run in background to avoid blocking the caller and to ensure we don't interfere
        // with any existing database transaction (especially important for SQLite in tests)
        _ = Task.Run(async () =>
        {
            try
            {
                // Small delay to ensure any parent transaction has completed
                await Task.Delay(50, shutdownToken);

                if (shutdownToken.IsCancellationRequested)
                    return;

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
            catch (OperationCanceledException) when (shutdownToken.IsCancellationRequested)
            {
                // Expected during host shutdown.
            }
            catch (ObjectDisposedException) when (shutdownToken.IsCancellationRequested)
            {
                // Expected if shutdown disposes services while a queued trigger is being cancelled.
            }
            catch (ObjectDisposedException) when (_isStopping)
            {
                // Expected if shutdown has started and service disposal wins the cancellation race.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating checkpoint trigger");
            }
        }, shutdownToken);

        return Task.CompletedTask;
    }

    private void RequestShutdown()
    {
        lock (_shutdownLock)
        {
            _isStopping = true;
            if (!_disposed)
                _shutdownCts.Cancel();
        }
    }

    public override void Dispose()
    {
        var shouldDispose = false;
        lock (_shutdownLock)
        {
            if (!_disposed)
            {
                _isStopping = true;
                _shutdownCts.Cancel();
                _disposed = true;
                shouldDispose = true;
            }
        }

        if (!shouldDispose)
            return;

        _shutdownRegistration.Dispose();
        _shutdownCts.Dispose();
        base.Dispose();
    }
}
