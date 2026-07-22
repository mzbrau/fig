using Fig.Api.Datalayer.Repositories;
using Fig.Api.Reports.Rendering.Components;
using Fig.Api.Reports.Rendering.Views;
using Fig.Common.Constants;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Reports.Implementations;

public class RestartLiveReloadDebtParameters
{
    [ReportParameter("From")]
    public DateTime From { get; set; }

    [ReportParameter("To")]
    public DateTime To { get; set; }
}

public class RestartLiveReloadDebtReportModel
{
    public IReadOnlyList<SummaryCardItem> Summary { get; set; } = [];
    public IReadOnlyList<LiveDebtSessionRow> LiveDebt { get; set; } = [];
    public IReadOnlyList<RestartLiveReloadEventRow> HistoricalEvents { get; set; } = [];
    public IReadOnlyList<TopRestartClientRow> TopRestartClients { get; set; } = [];
    public IReadOnlyList<ChartSlice> EventBreakdown { get; set; } = [];
}

public class LiveDebtSessionRow
{
    public string ClientDisplay { get; set; } = string.Empty;
    public string Hostname { get; set; } = string.Empty;
    public string RestartRequired { get; set; } = string.Empty;
    public string LiveReload { get; set; } = string.Empty;
    public string ApplicationVersion { get; set; } = string.Empty;
    public DateTime LastSeen { get; set; }
}

public class RestartLiveReloadEventRow
{
    public DateTime Timestamp { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string ClientDisplay { get; set; } = string.Empty;
    public string? User { get; set; }
    public string? Details { get; set; }
}

public class TopRestartClientRow
{
    public string ClientDisplay { get; set; } = string.Empty;
    public int RestartRequests { get; set; }
}

public class RestartLiveReloadDebtReport : ReportBase<RestartLiveReloadDebtParameters, RestartLiveReloadDebtReportModel>
{
    private static readonly string[] DebtEventTypes =
    [
        EventMessage.RestartRequested,
        EventMessage.LiveReloadChanged
    ];

    private readonly IClientStatusRepository _clientStatusRepository;
    private readonly IEventLogRepository _eventLogRepository;

    public RestartLiveReloadDebtReport(
        IClientStatusRepository clientStatusRepository,
        IEventLogRepository eventLogRepository)
    {
        _clientStatusRepository = clientStatusRepository;
        _eventLogRepository = eventLogRepository;
    }

    public override string Id => "restart-live-reload-debt";
    public override string Name => "Restart & Live-Reload Debt";
    public override string Category => "Operations";
    public override string Description =>
        "Highlights sessions that need a restart or have live reload disabled, plus historical restart and live-reload activity.";
    public override Type BodyComponentType => typeof(RestartLiveReloadDebtReportView);

    public override async Task<object> ExecuteAsync(RestartLiveReloadDebtParameters parameters, CancellationToken cancellationToken = default)
    {
        var (from, to) = ReportDateRange.Validate(parameters.From, parameters.To);

        var clients = await _clientStatusRepository.GetAllClients(RequireAuthenticatedUser());
        var liveDebt = clients
            .SelectMany(c => c.RunSessions
                .Where(s => s.RestartRequiredToApplySettings || !s.LiveReload)
                .Select(s => new LiveDebtSessionRow
                {
                    ClientDisplay = ReportValueFormatter.FormatClientDisplay(c.Name, c.Instance),
                    Hostname = s.Hostname ?? string.Empty,
                    RestartRequired = s.RestartRequiredToApplySettings ? "Yes" : "No",
                    LiveReload = s.LiveReload ? "Enabled" : "Disabled",
                    ApplicationVersion = s.ApplicationVersion,
                    LastSeen = s.LastSeen
                }))
            .OrderByDescending(r => r.RestartRequired)
            .ThenBy(r => r.LiveReload)
            .ThenBy(r => r.ClientDisplay, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var events = (await _eventLogRepository.GetEventsByTypes(from, to, DebtEventTypes, RequireAuthenticatedUser()))
            .OrderByDescending(e => e.Timestamp)
            .ToList();

        var historical = events.Select(ToEventRow).ToList();
        var restartEvents = events.Where(e => e.EventType == EventMessage.RestartRequested).ToList();
        var topRestartClients = EventAnalytics.TopBy(
                restartEvents,
                e => string.IsNullOrWhiteSpace(e.ClientName)
                    ? null
                    : ReportValueFormatter.FormatClientDisplay(e.ClientName, e.Instance))
            .Select(t => new TopRestartClientRow
            {
                ClientDisplay = t.Key,
                RestartRequests = t.Count
            })
            .ToList();

        return new RestartLiveReloadDebtReportModel
        {
            Summary =
            [
                new SummaryCardItem("Live Debt Sessions", liveDebt.Count.ToString()),
                new SummaryCardItem("Restart Required",
                    liveDebt.Count(r => r.RestartRequired == "Yes").ToString()),
                new SummaryCardItem("Live Reload Disabled",
                    liveDebt.Count(r => r.LiveReload == "Disabled").ToString()),
                new SummaryCardItem("Restart Requests", restartEvents.Count.ToString()),
                new SummaryCardItem("Live Reload Changes",
                    EventAnalytics.CountOfType(events, EventMessage.LiveReloadChanged).ToString())
            ],
            LiveDebt = liveDebt,
            HistoricalEvents = historical,
            TopRestartClients = topRestartClients,
            EventBreakdown = EventAnalytics.CountByEventType(events)
        };
    }

    private static RestartLiveReloadEventRow ToEventRow(EventLogBusinessEntity log)
    {
        var details = new List<string>();
        if (!string.IsNullOrWhiteSpace(log.Message))
            details.Add(log.Message!);
        if (!string.IsNullOrWhiteSpace(log.OriginalValue) || !string.IsNullOrWhiteSpace(log.NewValue))
            details.Add($"{log.OriginalValue} → {log.NewValue}");

        return new RestartLiveReloadEventRow
        {
            Timestamp = log.Timestamp,
            EventType = log.EventType,
            ClientDisplay = string.IsNullOrWhiteSpace(log.ClientName)
                ? string.Empty
                : ReportValueFormatter.FormatClientDisplay(log.ClientName, log.Instance),
            User = log.AuthenticatedUser,
            Details = details.Count == 0 ? null : string.Join(" · ", details)
        };
    }
}
