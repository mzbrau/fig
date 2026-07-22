using Fig.Api.Datalayer.Repositories;
using Fig.Api.Reports.Rendering.Components;
using Fig.Api.Reports.Rendering.Views;
using Fig.Common.Constants;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Reports.Implementations;

public class AnomalyQuietPeriodParameters
{
    [ReportParameter("From")]
    public DateTime From { get; set; }

    [ReportParameter("To")]
    public DateTime To { get; set; }
}

public class AnomalyQuietPeriodReportModel
{
    public IReadOnlyList<SummaryCardItem> Summary { get; set; } = [];
    public IReadOnlyList<AnomalyMetricRow> Anomalies { get; set; } = [];
    public IReadOnlyList<QuietClientRow> QuietClients { get; set; } = [];
}

public class AnomalyMetricRow
{
    public string Metric { get; set; } = string.Empty;
    public int PeriodCount { get; set; }
    public int BaselineCount { get; set; }
    public string Flagged { get; set; } = string.Empty;
}

public class QuietClientRow
{
    public string ClientDisplay { get; set; } = string.Empty;
    public DateTime? LastRegistration { get; set; }
    public int BaselineSessionEvents { get; set; }
    public int ActiveSessions { get; set; }
}

public class AnomalyQuietPeriodReport : ReportBase<AnomalyQuietPeriodParameters, AnomalyQuietPeriodReportModel>
{
    private static readonly string[] TrackedEventTypes =
    [
        EventMessage.LoginFailed,
        EventMessage.InvalidClientSecretAttempt,
        EventMessage.SettingValueUpdated,
        EventMessage.ExternallyManagedSettingUpdatedByUser,
        EventMessage.NewSession,
        EventMessage.ExpiredSession
    ];

    private static readonly HashSet<string> SessionEventTypes = new(StringComparer.Ordinal)
    {
        EventMessage.NewSession,
        EventMessage.ExpiredSession
    };

    private readonly IEventLogRepository _eventLogRepository;
    private readonly IClientStatusRepository _clientStatusRepository;
    private readonly ISettingClientRepository _settingClientRepository;

    public AnomalyQuietPeriodReport(
        IEventLogRepository eventLogRepository,
        IClientStatusRepository clientStatusRepository,
        ISettingClientRepository settingClientRepository)
    {
        _eventLogRepository = eventLogRepository;
        _clientStatusRepository = clientStatusRepository;
        _settingClientRepository = settingClientRepository;
    }

    public override string Id => "anomaly-quiet-period";
    public override string Name => "Anomaly / Quiet Period Report";
    public override string Category => "Analytics";
    public override string Description =>
        "Compares the selected period against an equal-length baseline to flag anomalies and identify previously active clients that went quiet.";
    public override Type BodyComponentType => typeof(AnomalyQuietPeriodReportView);

    public override async Task<object> ExecuteAsync(AnomalyQuietPeriodParameters parameters, CancellationToken cancellationToken = default)
    {
        var (from, to) = ReportDateRange.Validate(parameters.From, parameters.To);
        var span = to - from;
        var baselineFrom = from - span;
        var baselineTo = from;

        var periodEvents = await _eventLogRepository.GetEventsByTypes(from, to, TrackedEventTypes, RequireAuthenticatedUser());
        var baselineEvents = await _eventLogRepository.GetEventsByTypes(baselineFrom, baselineTo, TrackedEventTypes, RequireAuthenticatedUser());

        var anomalies = AnomalyDetector.Detect(periodEvents, baselineEvents);
        var anomalyRows = anomalies.Select(a => new AnomalyMetricRow
        {
            Metric = a.Name,
            PeriodCount = a.PeriodCount,
            BaselineCount = a.BaselineCount,
            Flagged = a.IsAnomaly ? "Yes" : "No"
        }).ToList();

        var statuses = await _clientStatusRepository.GetAllClients(RequireAuthenticatedUser());
        var clients = await _settingClientRepository.GetAllClients(RequireAuthenticatedUser());
        var statusByKey = statuses.ToDictionary(
            c => ClientKey(c.Name, c.Instance),
            c => c,
            StringComparer.OrdinalIgnoreCase);

        var baselineSessionCounts = baselineEvents
            .Where(e => SessionEventTypes.Contains(e.EventType) && !string.IsNullOrWhiteSpace(e.ClientName))
            .GroupBy(e => ClientKey(e.ClientName!, e.Instance), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        var periodSessionClients = periodEvents
            .Where(e => SessionEventTypes.Contains(e.EventType) && !string.IsNullOrWhiteSpace(e.ClientName))
            .Select(e => ClientKey(e.ClientName!, e.Instance))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var quietClients = IdentifyQuietClients(
            clients,
            statusByKey,
            baselineSessionCounts,
            periodSessionClients,
            from);

        return new AnomalyQuietPeriodReportModel
        {
            Summary =
            [
                new SummaryCardItem("Period Events", periodEvents.Count.ToString()),
                new SummaryCardItem("Baseline Events", baselineEvents.Count.ToString()),
                new SummaryCardItem("Flagged Anomalies", anomalies.Count(a => a.IsAnomaly).ToString()),
                new SummaryCardItem("Quiet Clients", quietClients.Count.ToString())
            ],
            Anomalies = anomalyRows,
            QuietClients = quietClients
        };
    }

    internal static IReadOnlyList<QuietClientRow> IdentifyQuietClients(
        IEnumerable<SettingClientBusinessEntity> clients,
        IReadOnlyDictionary<string, ClientStatusBusinessEntity> statusByKey,
        IReadOnlyDictionary<string, int> baselineSessionCounts,
        ISet<string> periodSessionClients,
        DateTime periodFromUtc)
    {
        var quietClients = new List<QuietClientRow>();
        foreach (var client in clients)
        {
            var key = ClientKey(client.Name, client.Instance);
            statusByKey.TryGetValue(key, out var status);
            var activeSessions = status?.RunSessions?.Count ?? 0;
            baselineSessionCounts.TryGetValue(key, out var baselineSessionEvents);

            var wasPreviouslyActive = baselineSessionEvents > 0 ||
                                      (client.LastRegistration is not null && client.LastRegistration < periodFromUtc);

            if (!wasPreviouslyActive)
                continue;

            if (periodSessionClients.Contains(key) || activeSessions > 0)
                continue;

            quietClients.Add(new QuietClientRow
            {
                ClientDisplay = ReportValueFormatter.FormatClientDisplay(client.Name, client.Instance),
                LastRegistration = client.LastRegistration,
                BaselineSessionEvents = baselineSessionEvents,
                ActiveSessions = activeSessions
            });
        }

        return quietClients
            .OrderBy(c => c.ClientDisplay, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    internal static string ClientKey(string name, string? instance)
        => string.IsNullOrWhiteSpace(instance) ? name : $"{name}|{instance}";
}
