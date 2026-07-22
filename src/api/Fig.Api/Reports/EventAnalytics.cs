using Fig.Api.Reports.Rendering.Components;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Reports;

public static class EventAnalytics
{
    public static IReadOnlyList<ChartSlice> CountByEventType(IEnumerable<EventLogBusinessEntity> events, int? top = null)
    {
        var groups = events
            .GroupBy(e => e.EventType ?? "Unknown")
            .Select(g => new ChartSlice(g.Key, g.Count()))
            .OrderByDescending(s => s.Value)
            .ToList();

        if (top is > 0 && groups.Count > top)
            return groups.Take(top.Value).ToList();

        return groups;
    }

    public static IReadOnlyList<(string Key, int Count)> TopBy(
        IEnumerable<EventLogBusinessEntity> events,
        Func<EventLogBusinessEntity, string?> keySelector,
        int top = 10)
    {
        return events
            .Select(keySelector)
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .GroupBy(k => k!, StringComparer.OrdinalIgnoreCase)
            .Select(g => (Key: g.Key, Count: g.Count()))
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Take(top)
            .ToList();
    }

    public static IReadOnlyList<(DateTime Day, int Count)> DailySeries(
        IEnumerable<EventLogBusinessEntity> events,
        DateTime fromUtc,
        DateTime toUtc)
    {
        var counts = events
            .GroupBy(e => e.Timestamp.ToUniversalTime().Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var result = new List<(DateTime Day, int Count)>();
        for (var day = fromUtc.Date; day <= toUtc.Date; day = day.AddDays(1))
            result.Add((day, counts.GetValueOrDefault(day)));

        return result;
    }

    public static int CountOfType(IEnumerable<EventLogBusinessEntity> events, string eventType)
        => events.Count(e => string.Equals(e.EventType, eventType, StringComparison.Ordinal));
}
