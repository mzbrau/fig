using System.Threading.Channels;
using Fig.Api.Constants;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Utils;
using Fig.Common.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Fig.Api.Workers;

/// <summary>
/// Applies best-effort side effects for client settings GET off the request hot path:
/// run-session LastSettingLoadUtc and SettingsRead event log.
/// </summary>
public class SettingsReadSideEffectWorker : BackgroundService
{
    private const int QueueCapacity = 1024;

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<SettingsReadSideEffectWorker> _logger;
    private readonly Channel<SettingsReadSideEffect> _channel = Channel.CreateBounded<SettingsReadSideEffect>(
        new BoundedChannelOptions(QueueCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait // TryWrite returns false when full
        });

    public SettingsReadSideEffectWorker(
        IEventDistributor eventDistributor,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<SettingsReadSideEffectWorker> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;

        eventDistributor.Subscribe<SettingsReadSideEffect>(EventConstants.SettingsReadSideEffect, Enqueue);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var sideEffect in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var runSessionRepository = scope.ServiceProvider.GetRequiredService<IClientRunSessionRepository>();
                var eventLogRepository = scope.ServiceProvider.GetRequiredService<IEventLogRepository>();
                var eventLogFactory = scope.ServiceProvider.GetRequiredService<IEventLogFactory>();

                if (sideEffect.RunSessionId != Guid.Empty)
                {
                    await runSessionRepository.TouchLastSettingLoadUtc(
                        sideEffect.ClientId, sideEffect.RunSessionId, sideEffect.LoadedUtc);
                }

                await eventLogRepository.Add(
                    eventLogFactory.SettingsRead(sideEffect.ClientId, sideEffect.ClientName, sideEffect.Instance));
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error while applying settings-read side effects for client {ClientName}",
                    sideEffect.ClientName);
            }
        }
    }

    private Task Enqueue(SettingsReadSideEffect sideEffect)
    {
        if (_channel.Writer.TryWrite(sideEffect))
        {
            _logger.LogDebug(
                "Queued settings-read side effects for client {ClientName}",
                sideEffect.ClientName);
        }
        else
        {
            var reason = _channel.Reader.Completion.IsCompleted
                ? "the worker is stopping"
                : "the queue is full";
            _logger.LogWarning(
                "Dropped settings-read side effects for client {ClientName} because {Reason}",
                sideEffect.ClientName,
                reason);
        }

        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _channel.Writer.TryComplete();
        return base.StopAsync(cancellationToken);
    }
}
