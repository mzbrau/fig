using Fig.Api.Datalayer.Repositories;
using Fig.Api.Reports.Rendering.Components;
using Fig.Api.Reports.Rendering.Views;
using Fig.Common.Constants;
using Fig.Contracts.Reports;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Reports.Implementations;

public class IncidentCorrelationParameters
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

public class IncidentCorrelationReportModel
{
    public string ClientDisplay { get; set; } = string.Empty;
    public IReadOnlyList<SummaryCardItem> Summary { get; set; } = [];
    public IReadOnlyList<TimelineItem> Timeline { get; set; } = [];
    public IReadOnlyList<IncidentOutageRow> Outages { get; set; } = [];
}

public class IncidentOutageRow
{
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public string Duration { get; set; } = string.Empty;
}

public class IncidentCorrelationReport : ReportBase<IncidentCorrelationParameters, IncidentCorrelationReportModel>
{
    private static readonly string[] IncidentEventTypes =
    [
        EventMessage.SettingValueUpdated,
        EventMessage.ExternallyManagedSettingUpdatedByUser,
        EventMessage.RestartRequested,
        EventMessage.LiveReloadChanged,
        EventMessage.HealthStatusChanged,
        EventMessage.CheckPointCreated,
        EventMessage.CheckPointApplied,
        EventMessage.NoteAddedToCheckPoint,
        EventMessage.DataImported,
        EventMessage.DataImportFailed,
        EventMessage.DataImportStarted,
        EventMessage.ClientImported,
        EventMessage.WebHookSent,
        EventMessage.NewSession,
        EventMessage.ExpiredSession
    ];

    private static readonly HashSet<string> SettingUpdateTypes = new(StringComparer.Ordinal)
    {
        EventMessage.SettingValueUpdated,
        EventMessage.ExternallyManagedSettingUpdatedByUser
    };

    private static readonly HashSet<string> RestartTypes = new(StringComparer.Ordinal)
    {
        EventMessage.RestartRequested
    };

    private static readonly HashSet<string> LiveReloadTypes = new(StringComparer.Ordinal)
    {
        EventMessage.LiveReloadChanged
    };

    private static readonly HashSet<string> HealthTypes = new(StringComparer.Ordinal)
    {
        EventMessage.HealthStatusChanged
    };

    private static readonly HashSet<string> CheckpointTypes = new(StringComparer.Ordinal)
    {
        EventMessage.CheckPointCreated,
        EventMessage.CheckPointApplied,
        EventMessage.NoteAddedToCheckPoint
    };

    private static readonly HashSet<string> ImportTypes = new(StringComparer.Ordinal)
    {
        EventMessage.DataImported,
        EventMessage.DataImportFailed,
        EventMessage.DataImportStarted,
        EventMessage.ClientImported
    };

    private static readonly HashSet<string> WebhookTypes = new(StringComparer.Ordinal)
    {
        EventMessage.WebHookSent
    };

    private static readonly HashSet<string> SessionTypes = new(StringComparer.Ordinal)
    {
        EventMessage.NewSession,
        EventMessage.ExpiredSession
    };

    private readonly IEventLogRepository _eventLogRepository;
    private readonly IClientStatusRepository _clientStatusRepository;

    public IncidentCorrelationReport(
        IEventLogRepository eventLogRepository,
        IClientStatusRepository clientStatusRepository)
    {
        _eventLogRepository = eventLogRepository;
        _clientStatusRepository = clientStatusRepository;
    }

    public override string Id => "incident-correlation";
    public override string Name => "Incident Correlation Pack";
    public override string Category => "Analytics";
    public override string Description =>
        "Correlates setting changes, restarts, health, checkpoints, imports, webhooks, and session/outage activity for a client over a date range.";
    public override Type BodyComponentType => typeof(IncidentCorrelationReportView);
    public override ReportPageOrientation PageOrientation => ReportPageOrientation.Landscape;

    public override async Task<object> ExecuteAsync(IncidentCorrelationParameters parameters, CancellationToken cancellationToken = default)
    {
        var (from, to) = ReportDateRange.Validate(parameters.From, parameters.To);
        var instance = string.IsNullOrWhiteSpace(parameters.Instance) ? null : parameters.Instance.Trim();

        ThrowIfNoAccess(parameters.ClientName);
        var events = (await _eventLogRepository.GetClientEvents(
                from, to, parameters.ClientName, instance, IncidentEventTypes))
            .OrderBy(e => e.Timestamp)
            .ToList();

        var client = await _clientStatusRepository.GetClientReadOnly(parameters.ClientName, instance);
        var activeSessions = client?.RunSessions?.ToList() ?? new List<ClientRunSessionBusinessEntity>();
        var sessionEvents = events.Where(e => SessionTypes.Contains(e.EventType)).ToList();
        var uptime = UptimeCalculator.Calculate(from, to, sessionEvents, activeSessions);

        var settingUpdates = events.Count(e => SettingUpdateTypes.Contains(e.EventType));
        var restarts = events.Count(e => RestartTypes.Contains(e.EventType));
        var liveReloads = events.Count(e => LiveReloadTypes.Contains(e.EventType));
        var health = events.Count(e => HealthTypes.Contains(e.EventType));
        var checkpoints = events.Count(e => CheckpointTypes.Contains(e.EventType));
        var imports = events.Count(e => ImportTypes.Contains(e.EventType));
        var webhooks = events.Count(e => WebhookTypes.Contains(e.EventType));
        var sessions = sessionEvents.Count;

        return new IncidentCorrelationReportModel
        {
            ClientDisplay = ReportValueFormatter.FormatClientDisplay(parameters.ClientName, instance),
            Summary =
            [
                new SummaryCardItem("Setting Updates", settingUpdates.ToString()),
                new SummaryCardItem("Restarts", restarts.ToString()),
                new SummaryCardItem("Live Reload", liveReloads.ToString()),
                new SummaryCardItem("Health", health.ToString()),
                new SummaryCardItem("Checkpoints", checkpoints.ToString()),
                new SummaryCardItem("Imports", imports.ToString()),
                new SummaryCardItem("Webhooks", webhooks.ToString()),
                new SummaryCardItem("Session Events", sessions.ToString()),
                new SummaryCardItem("Outages", uptime.Outages.Count.ToString(), $"{uptime.UptimePercent:0.##}% uptime")
            ],
            Timeline = events
                .OrderByDescending(e => e.Timestamp)
                .Select(e => new TimelineItem(e.Timestamp, e.EventType, BuildDetail(e)))
                .ToList(),
            Outages = uptime.Outages.Select(o => new IncidentOutageRow
            {
                StartUtc = o.StartUtc,
                EndUtc = o.EndUtc,
                Duration = FormatDuration(o.Duration)
            }).ToList()
        };
    }

    private static string? BuildDetail(EventLogBusinessEntity log)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(log.SettingName))
            parts.Add(log.SettingName!);
        if (!string.IsNullOrWhiteSpace(log.Message))
            parts.Add(log.Message!);
        else if (!string.IsNullOrWhiteSpace(log.OriginalValue) || !string.IsNullOrWhiteSpace(log.NewValue))
            parts.Add("Value changed");
        if (!string.IsNullOrWhiteSpace(log.AuthenticatedUser))
            parts.Add(log.AuthenticatedUser!);
        if (!string.IsNullOrWhiteSpace(log.Hostname))
            parts.Add(log.Hostname!);
        return parts.Count == 0 ? null : string.Join(" · ", parts);
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
}
