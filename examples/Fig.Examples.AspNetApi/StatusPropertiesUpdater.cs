using Fig.Client.Abstractions.StatusProperties;
using Microsoft.Extensions.Hosting;

namespace Fig.Examples.AspNetApi;

/// <summary>
/// Periodically updates custom status properties using Set, Update, Clear, and Current
/// so Connected Clients shows live changes after the next Fig status poll.
/// </summary>
public sealed class StatusPropertiesUpdater : BackgroundService
{
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(15);
    private const int ClearErrorEveryNTicks = 4;

    private readonly IFigStatusProperties<AspNetApiStatusProperties> _statusProperties;
    private readonly ILogger<StatusPropertiesUpdater> _logger;
    private readonly DateTime _startedUtc = DateTime.UtcNow;
    private readonly Guid _sessionId = Guid.NewGuid();
    private long _tick;

    public StatusPropertiesUpdater(
        IFigStatusProperties<AspNetApiStatusProperties> statusProperties,
        ILogger<StatusPropertiesUpdater> logger)
    {
        _statusProperties = statusProperties;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Set: individual property writes that merge into the in-memory bag
        _statusProperties.Set(x => x.InternalRunId, Guid.NewGuid().ToString("N"));
        _statusProperties.Set(x => x.CorrelationId, Guid.NewGuid());
        _statusProperties.Set(x => x.Region, "local");
        _statusProperties.Set(x => x.SessionId, _sessionId);
        _statusProperties.Set(x => x.Phase, WorkerPhase.WarmingUp);
        _statusProperties.Set(x => x.UnitCost, 1.25m);
        _statusProperties.Set(x => x.ShiftStart, new TimeOnly(9, 0));

        using var timer = new PeriodicTimer(UpdateInterval);
        do
        {
            _tick++;
            var now = DateTime.UtcNow;
            var phase = (_tick % 5) switch
            {
                0 => WorkerPhase.Idle,
                1 => WorkerPhase.WarmingUp,
                2 => WorkerPhase.Processing,
                3 => WorkerPhase.Draining,
                _ => WorkerPhase.Faulted
            };

            // Update: mutate several properties in one critical section
            _statusProperties.Update(x =>
            {
                x.LastTickUtc = now;
                x.TickCount = _tick;
                x.Uptime = now - _startedUtc;
                x.Phase = phase;
                x.IsHealthy = phase != WorkerPhase.Faulted;
                x.QueueDepth = (int)(_tick % 40);
                x.CpuSample = 10 + (_tick % 70) + Random.Shared.NextDouble();
                x.UnitCost = 1.25m + (_tick % 10) * 0.01m;
                x.LastSyncOffset = DateTimeOffset.UtcNow;
                x.BusinessDate = DateOnly.FromDateTime(now);
                x.ShiftStart = TimeOnly.FromDateTime(now);
                x.ContextJson = $"{{\"tick\":{_tick},\"phase\":\"{phase}\"}}";
                x.Region = _tick % 2 == 0 ? "local" : "eu-west";
            });

            // Clear: every N ticks drop LastErrorUtc; otherwise set a sample error time
            if (_tick % ClearErrorEveryNTicks == 0)
            {
                _statusProperties.Clear(x => x.LastErrorUtc);
            }
            else
            {
                _statusProperties.Set(x => x.LastErrorUtc, now.AddMinutes(-(_tick % 30)));
            }

            // Set with TextColor: cycle Usage HIGH (red) / LOW (green) / NORMAL (orange)
            var (usage, usageColor) = (_tick % 3) switch
            {
                0 => ("HIGH", "#E53935"),
                1 => ("LOW", "#43A047"),
                _ => ("NORMAL", "#FB8C00")
            };
            _statusProperties.Set(x => x.Usage, usage, usageColor);

            // Current: read a clone for logging (does not affect the stored bag)
            if (_tick % 3 == 0)
            {
                var snapshot = _statusProperties.Current;
                _logger.LogInformation(
                    "AspNetApi status snapshot: Phase={Phase}, Tick={Tick}, Usage={Usage}, Queue={Queue}, Healthy={Healthy}, LastError={LastError}",
                    snapshot.Phase,
                    snapshot.TickCount,
                    snapshot.Usage,
                    snapshot.QueueDepth,
                    snapshot.IsHealthy,
                    snapshot.LastErrorUtc);
            }
            else
            {
                _logger.LogDebug("Updated AspNetApi custom status properties (tick {Tick})", _tick);
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
