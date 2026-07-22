using Fig.Api.Datalayer.Repositories;
using Fig.Api.Reports.Rendering.Components;
using Fig.Api.Reports.Rendering.Views;
using Fig.Common.Constants;
using Fig.Contracts.Reports;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Reports.Implementations;

public class ClientUptimeParameters
{
    [ReportParameter("Client", LookupKind = ReportParameterLookupKind.Clients)]
    public string ClientName { get; set; } = string.Empty;

    [ReportParameter("Instance")]
    public string? Instance { get; set; }

    [ReportParameter("From")]
    public DateTime From { get; set; }

    [ReportParameter("To")]
    public DateTime To { get; set; }
}

public class ClientUptimeReportModel
{
    public string ClientDisplay { get; set; } = string.Empty;
    public IReadOnlyList<SummaryCardItem> Summary { get; set; } = [];
    public double UptimePercent { get; set; }
    public double DowntimePercent { get; set; }
    public IReadOnlyList<TimelineItem> Timeline { get; set; } = [];
    public IReadOnlyList<OutageRow> Outages { get; set; } = [];
    public IReadOnlyList<ChartSlice> AvailabilitySlices { get; set; } = [];
    public IReadOnlyList<RunSessionLogRow> SessionLog { get; set; } = [];
}

public class OutageRow
{
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public string Duration { get; set; } = string.Empty;
}

public class RunSessionLogRow
{
    public DateTime TimestampUtc { get; set; }
    public string Event { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
}

public class ClientUptimeReport : ReportBase<ClientUptimeParameters, ClientUptimeReportModel>
{
    private const string UptimeColor = "#38a169";
    private const string DowntimeColor = "#e53e3e";
    private const int SessionLogLimit = 100;

    private static readonly string[] SessionEventTypes =
    [
        EventMessage.NewSession,
        EventMessage.ExpiredSession
    ];

    private readonly IEventLogRepository _eventLogRepository;
    private readonly IClientStatusRepository _clientStatusRepository;

    public ClientUptimeReport(
        IEventLogRepository eventLogRepository,
        IClientStatusRepository clientStatusRepository)
    {
        _eventLogRepository = eventLogRepository;
        _clientStatusRepository = clientStatusRepository;
    }

    public override string Id => "client-uptime";
    public override string Name => "Client Uptime Report";
    public override string Category => "Clients";
    public override string Description =>
        "Displays client availability over a date range. Availability is online whenever at least one run session is active (redundant instances do not count as downtime).";
    public override Type BodyComponentType => typeof(ClientUptimeReportView);
    public override ReportPageOrientation PageOrientation => ReportPageOrientation.Landscape;

    public override async Task<object> ExecuteAsync(ClientUptimeParameters parameters, CancellationToken cancellationToken = default)
    {
        if (parameters.From > parameters.To)
            throw new ReportParameterValidationException("From must be before To.");

        var from = EnsureUtc(parameters.From);
        var to = EnsureUtc(parameters.To);
        var instance = string.IsNullOrWhiteSpace(parameters.Instance) ? null : parameters.Instance;

        ThrowIfNoAccess(parameters.ClientName);
        var events = (await _eventLogRepository.GetClientEvents(from, to, parameters.ClientName, instance, SessionEventTypes))
            .OrderBy(e => e.Timestamp)
            .ToList();
        var client = await _clientStatusRepository.GetClientReadOnly(parameters.ClientName, instance);
        var activeSessions = client?.RunSessions?.ToList() ?? new List<ClientRunSessionBusinessEntity>();

        var result = UptimeCalculator.Calculate(from, to, events, activeSessions);

        return new ClientUptimeReportModel
        {
            ClientDisplay = ReportValueFormatter.FormatClientDisplay(parameters.ClientName, instance),
            UptimePercent = result.UptimePercent,
            DowntimePercent = result.DowntimePercent,
            Summary =
            [
                new SummaryCardItem("Uptime", FormatDuration(result.Uptime), $"{result.UptimePercent:0.##}%"),
                new SummaryCardItem("Downtime", FormatDuration(result.Downtime), $"{result.DowntimePercent:0.##}%"),
                new SummaryCardItem("Outages", result.Outages.Count.ToString()),
                new SummaryCardItem("Peak Concurrent Sessions", result.PeakConcurrentSessions.ToString())
            ],
            Timeline = result.OnlineIntervals
                .Select(i => new TimelineItem(i.StartUtc, "Online", $"Until {i.EndUtc:yyyy-MM-dd HH:mm:ss} UTC ({FormatDuration(i.Duration)})"))
                .Concat(result.Outages.Select(o =>
                    new TimelineItem(o.StartUtc, "Outage", $"Until {o.EndUtc:yyyy-MM-dd HH:mm:ss} UTC ({FormatDuration(o.Duration)})")))
                .OrderByDescending(t => t.TimestampUtc)
                .Take(60)
                .ToList(),
            Outages = result.Outages.Select(o => new OutageRow
            {
                StartUtc = o.StartUtc,
                EndUtc = o.EndUtc,
                Duration = FormatDuration(o.Duration)
            }).ToList(),
            AvailabilitySlices =
            [
                new ChartSlice("Uptime", Math.Round(result.UptimePercent, 2), UptimeColor),
                new ChartSlice("Downtime", Math.Round(result.DowntimePercent, 2), DowntimeColor)
            ],
            SessionLog = events
                .OrderByDescending(e => e.Timestamp)
                .Take(SessionLogLimit)
                .Select(ToSessionLogRow)
                .ToList()
        };
    }

    private static RunSessionLogRow ToSessionLogRow(EventLogBusinessEntity evt)
    {
        var isNew = evt.EventType == EventMessage.NewSession;
        return new RunSessionLogRow
        {
            TimestampUtc = evt.Timestamp,
            Event = isNew ? "Session Started" : "Session Ended",
            Detail = isNew
                ? (evt.NewValue ?? string.Empty)
                : (evt.OriginalValue ?? string.Empty)
        };
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalDays >= 1)
            return $"{(int)duration.TotalDays}d {duration.Hours}h {duration.Minutes}m";
        if (duration.TotalHours >= 1)
            return $"{(int)duration.TotalHours}h {duration.Minutes}m {duration.Seconds}s";
        if (duration.TotalMinutes >= 1)
            return $"{(int)duration.TotalMinutes}m {duration.Seconds}s";
        return $"{duration.TotalSeconds:0.#}s";
    }

    private static DateTime EnsureUtc(DateTime value)
        => value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value.ToUniversalTime(), DateTimeKind.Utc);
}
