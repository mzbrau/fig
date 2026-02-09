using System;
using System.Collections.Generic;

namespace Fig.LoadTest;

public sealed class StatusSyncMetrics
{
    private readonly object _lock = new();
    private long _count;
    private long _minTicks = long.MaxValue;
    private long _maxTicks;
    private long _sumTicks;
    private readonly List<long> _samplesTicks = new();

    public void Record(TimeSpan duration)
    {
        var ticks = duration.Ticks;
        lock (_lock)
        {
            _count++;
            _sumTicks += ticks;
            _samplesTicks.Add(ticks);
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
                return new StatusSyncSummary(
                    TimeSpan.Zero,
                    TimeSpan.Zero,
                    TimeSpan.Zero,
                    TimeSpan.Zero,
                    TimeSpan.Zero,
                    0,
                    Array.Empty<long>());
            }

            var avgTicks = _sumTicks / count;
            var samples = _samplesTicks.ToArray();
            var medianTicks = CalculateMedianTicks(samples);
            var stdDevTicks = CalculateSampleStdDevTicks(samples, _sumTicks / (double)count);
            return new StatusSyncSummary(
                TimeSpan.FromTicks(_minTicks),
                TimeSpan.FromTicks(_maxTicks),
                TimeSpan.FromTicks(avgTicks),
                TimeSpan.FromTicks((long)Math.Round(medianTicks)),
                TimeSpan.FromTicks((long)Math.Round(stdDevTicks)),
                count,
                samples);
        }
    }

    private static double CalculateMedianTicks(long[] samples)
    {
        Array.Sort(samples);
        var mid = samples.Length / 2;
        if (samples.Length % 2 == 1)
        {
            return samples[mid];
        }

        return (samples[mid - 1] + samples[mid]) / 2.0;
    }

    private static double CalculateSampleStdDevTicks(long[] samples, double meanTicks)
    {
        if (samples.Length < 2)
        {
            return 0;
        }

        double sumSq = 0;
        foreach (var sample in samples)
        {
            var delta = sample - meanTicks;
            sumSq += delta * delta;
        }

        var variance = sumSq / (samples.Length - 1);
        return Math.Sqrt(variance);
    }
}

public sealed record StatusSyncSummary(
    TimeSpan Min,
    TimeSpan Max,
    TimeSpan Average,
    TimeSpan Median,
    TimeSpan StandardDeviation,
    long Count,
    IReadOnlyList<long> SamplesTicks);
