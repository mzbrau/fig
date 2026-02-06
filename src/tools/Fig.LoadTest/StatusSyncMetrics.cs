using System;

namespace Fig.LoadTest;

public sealed class StatusSyncMetrics
{
    private readonly object _lock = new();
    private long _count;
    private long _minTicks = long.MaxValue;
    private long _maxTicks;
    private long _sumTicks;

    public void Record(TimeSpan duration)
    {
        var ticks = duration.Ticks;
        lock (_lock)
        {
            _count++;
            _sumTicks += ticks;
            if (ticks < _minTicks) _minTicks = ticks;
            if (ticks > _maxTicks) _maxTicks = ticks;
        }
    }

    public StatusSyncSummary Snapshot()
    {
        lock (_lock)
        {
            var count = _count;
            if (count == 0)
            {
                return new StatusSyncSummary(TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, 0);
            }

            var avgTicks = _sumTicks / count;
            return new StatusSyncSummary(
                TimeSpan.FromTicks(_minTicks),
                TimeSpan.FromTicks(_maxTicks),
                TimeSpan.FromTicks(avgTicks),
                count);
        }
    }
}

public sealed record StatusSyncSummary(TimeSpan Min, TimeSpan Max, TimeSpan Average, long Count);
