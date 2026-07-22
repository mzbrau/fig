using System.Globalization;
using System.Text.RegularExpressions;
using Fig.Common.Constants;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Reports;

public static class UptimeCalculator
{
    private static readonly Regex UptimeSecondsRegex = new(@"up time:([0-9]+(?:\.[0-9]+)?)s",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static UptimeReportResult Calculate(
        DateTime fromUtc,
        DateTime toUtc,
        IEnumerable<EventLogBusinessEntity> sessionEvents,
        IEnumerable<ClientRunSessionBusinessEntity> activeSessions)
    {
        if (toUtc < fromUtc)
            throw new ArgumentException("From must be before To.");

        var range = toUtc - fromUtc;
        if (range <= TimeSpan.Zero)
            return new UptimeReportResult(fromUtc, toUtc, TimeSpan.Zero, TimeSpan.Zero, 0, 0, 0, [], []);

        var eventsList = sessionEvents.ToList();
        var activeList = activeSessions.ToList();
        var sessionIntervals = BuildSessionIntervals(fromUtc, toUtc, eventsList, activeList);
        var peakConcurrent = ComputePeakConcurrentSessions(fromUtc, eventsList, activeList);
        var intervals = MergeIntervals(sessionIntervals);
        var uptime = TimeSpan.Zero;
        foreach (var interval in intervals)
            uptime += interval.EndUtc - interval.StartUtc;

        if (uptime > range)
            uptime = range;

        var downtime = range - uptime;
        var uptimePercent = range.TotalSeconds <= 0 ? 0 : uptime.TotalSeconds / range.TotalSeconds * 100.0;
        var downtimePercent = 100.0 - uptimePercent;

        var outages = BuildOutages(fromUtc, toUtc, intervals);

        return new UptimeReportResult(
            fromUtc,
            toUtc,
            uptime,
            downtime,
            uptimePercent,
            downtimePercent,
            peakConcurrent,
            intervals,
            outages);
    }

    private static List<UptimeInterval> BuildSessionIntervals(
        DateTime fromUtc,
        DateTime toUtc,
        IEnumerable<EventLogBusinessEntity> sessionEvents,
        IEnumerable<ClientRunSessionBusinessEntity> activeSessions)
    {
        var intervals = new List<UptimeInterval>();
        var openStarts = new Stack<DateTime>();

        foreach (var evt in sessionEvents.OrderBy(e => e.Timestamp))
        {
            if (evt.EventType == EventMessage.NewSession)
            {
                openStarts.Push(Clamp(evt.Timestamp, fromUtc, toUtc));
            }
            else if (evt.EventType == EventMessage.ExpiredSession)
            {
                var end = Clamp(evt.Timestamp, fromUtc, toUtc);
                DateTime start;
                if (openStarts.Count > 0)
                {
                    start = openStarts.Pop();
                }
                else if (TryParseUptimeSeconds(evt.OriginalValue, out var seconds))
                {
                    start = Clamp(evt.Timestamp.AddSeconds(-seconds), fromUtc, toUtc);
                }
                else
                {
                    continue;
                }

                if (end > start)
                    intervals.Add(new UptimeInterval(start, end, true));
            }
        }

        while (openStarts.Count > 0)
        {
            var start = openStarts.Pop();
            if (toUtc > start)
                intervals.Add(new UptimeInterval(start, toUtc, true));
        }

        foreach (var session in activeSessions)
        {
            var start = Clamp(session.StartTimeUtc, fromUtc, toUtc);
            var end = Clamp(session.LastSeen > toUtc ? toUtc : session.LastSeen, fromUtc, toUtc);
            if (end < toUtc)
                end = toUtc;
            if (end > start)
                intervals.Add(new UptimeInterval(start, end, true));
        }

        return intervals;
    }

    /// <summary>
    /// Peak number of overlapping run sessions. Uses the event stream so active DB sessions
    /// that already have an in-range NewSession are not double-counted.
    /// </summary>
    private static int ComputePeakConcurrentSessions(
        DateTime fromUtc,
        IReadOnlyList<EventLogBusinessEntity> sessionEvents,
        IReadOnlyList<ClientRunSessionBusinessEntity> activeSessions)
    {
        var current = 0;
        var peak = 0;

        foreach (var evt in sessionEvents.OrderBy(e => e.Timestamp))
        {
            if (evt.EventType == EventMessage.NewSession)
            {
                current++;
                if (current > peak)
                    peak = current;
            }
            else if (evt.EventType == EventMessage.ExpiredSession)
            {
                if (current > 0)
                {
                    current--;
                }
                else
                {
                    // Session started before the window; it contributed at least one concurrent slot.
                    if (peak < 1)
                        peak = 1;
                }
            }
        }

        // Active sessions that began before the window were never counted via NewSession above.
        var preexistingActive = activeSessions.Count(s => s.StartTimeUtc < fromUtc);
        peak = Math.Max(peak, current + preexistingActive);

        if (peak == 0 && activeSessions.Count > 0)
            peak = activeSessions.Count;

        return peak;
    }

    private static List<UptimeInterval> BuildOutages(DateTime fromUtc, DateTime toUtc, List<UptimeInterval> online)
    {
        var outages = new List<UptimeInterval>();
        var cursor = fromUtc;
        foreach (var onlineInterval in online.OrderBy(i => i.StartUtc))
        {
            if (onlineInterval.StartUtc > cursor)
                outages.Add(new UptimeInterval(cursor, onlineInterval.StartUtc, false));
            if (onlineInterval.EndUtc > cursor)
                cursor = onlineInterval.EndUtc;
        }

        if (cursor < toUtc)
            outages.Add(new UptimeInterval(cursor, toUtc, false));

        return outages;
    }

    private static List<UptimeInterval> MergeIntervals(List<UptimeInterval> intervals)
    {
        if (intervals.Count == 0)
            return intervals;

        var ordered = intervals.OrderBy(i => i.StartUtc).ToList();
        var merged = new List<UptimeInterval> { ordered[0] };
        for (var i = 1; i < ordered.Count; i++)
        {
            var last = merged[^1];
            var current = ordered[i];
            if (current.StartUtc <= last.EndUtc)
            {
                merged[^1] = last with { EndUtc = current.EndUtc > last.EndUtc ? current.EndUtc : last.EndUtc };
            }
            else
            {
                merged.Add(current);
            }
        }

        return merged;
    }

    private static DateTime Clamp(DateTime value, DateTime fromUtc, DateTime toUtc)
    {
        if (value < fromUtc)
            return fromUtc;
        if (value > toUtc)
            return toUtc;
        return value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    public static bool TryParseUptimeSeconds(string? originalValue, out double seconds)
    {
        seconds = 0;
        if (string.IsNullOrWhiteSpace(originalValue))
            return false;

        var match = UptimeSecondsRegex.Match(originalValue);
        if (!match.Success)
            return false;

        return double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out seconds);
    }
}

public record UptimeInterval(DateTime StartUtc, DateTime EndUtc, bool IsOnline)
{
    public TimeSpan Duration => EndUtc - StartUtc;
}

public record UptimeReportResult(
    DateTime FromUtc,
    DateTime ToUtc,
    TimeSpan Uptime,
    TimeSpan Downtime,
    double UptimePercent,
    double DowntimePercent,
    int PeakConcurrentSessions,
    IReadOnlyList<UptimeInterval> OnlineIntervals,
    IReadOnlyList<UptimeInterval> Outages);
