using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Reports;

public record FleetUptimeRow(
    string ClientName,
    string? Instance,
    string ClientDisplay,
    double UptimePercent,
    double DowntimePercent,
    TimeSpan Downtime,
    int OutageCount,
    int PeakConcurrentSessions);

public static class FleetUptimeAggregator
{
    public static IReadOnlyList<FleetUptimeRow> Aggregate(
        DateTime fromUtc,
        DateTime toUtc,
        IEnumerable<(string ClientName, string? Instance, IReadOnlyList<EventLogBusinessEntity> Events, IReadOnlyList<ClientRunSessionBusinessEntity> ActiveSessions)> clients)
    {
        var rows = new List<FleetUptimeRow>();

        foreach (var client in clients)
        {
            var result = UptimeCalculator.Calculate(fromUtc, toUtc, client.Events, client.ActiveSessions);
            rows.Add(new FleetUptimeRow(
                client.ClientName,
                client.Instance,
                ReportValueFormatter.FormatClientDisplay(client.ClientName, client.Instance),
                result.UptimePercent,
                result.DowntimePercent,
                result.Downtime,
                result.Outages.Count,
                result.PeakConcurrentSessions));
        }

        return rows
            .OrderBy(r => r.UptimePercent)
            .ThenBy(r => r.ClientDisplay, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
