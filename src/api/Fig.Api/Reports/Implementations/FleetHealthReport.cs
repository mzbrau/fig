using Fig.Api.Datalayer.Repositories;
using Fig.Api.Reports.Rendering.Components;
using Fig.Api.Reports.Rendering.Views;
using Fig.Common.Constants;
using Fig.Contracts.Health;
using Fig.Contracts.Reports;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Reports.Implementations;

public class FleetHealthParameters
{
    [ReportParameter("From")]
    public DateTime From { get; set; }

    [ReportParameter("To")]
    public DateTime To { get; set; }

    [ReportParameter("Client", LookupKind = ReportParameterLookupKind.Clients)]
    public string? ClientName { get; set; }

    [ReportParameter("Instance")]
    public string? Instance { get; set; }

    [ReportParameter("Min Sessions")]
    public int MinSessions { get; set; } = 1;
}

public class FleetHealthReportModel
{
    public string ScopeDisplay { get; set; } = string.Empty;
    public IReadOnlyList<SummaryCardItem> Summary { get; set; } = [];
    public IReadOnlyList<FleetClientRow> ZeroSessionClients { get; set; } = [];
    public IReadOnlyList<FleetUnhealthySessionRow> UnhealthySessions { get; set; } = [];
    public IReadOnlyList<FleetClientRow> BelowMinSessions { get; set; } = [];
    public IReadOnlyList<FleetRestartDebtRow> RestartRequiredDebt { get; set; } = [];
    public IReadOnlyList<FleetUptimeTableRow> UptimeRows { get; set; } = [];
}

public class FleetClientRow
{
    public string ClientDisplay { get; set; } = string.Empty;
    public int SessionCount { get; set; }
    public DateTime? LastRegistration { get; set; }
}

public class FleetUnhealthySessionRow
{
    public string ClientDisplay { get; set; } = string.Empty;
    public string Hostname { get; set; } = string.Empty;
    public string HealthStatus { get; set; } = string.Empty;
    public string ApplicationVersion { get; set; } = string.Empty;
    public DateTime LastSeen { get; set; }
}

public class FleetRestartDebtRow
{
    public string ClientDisplay { get; set; } = string.Empty;
    public string Hostname { get; set; } = string.Empty;
    public string ApplicationVersion { get; set; } = string.Empty;
    public DateTime LastSeen { get; set; }
}

public class FleetUptimeTableRow
{
    public string ClientDisplay { get; set; } = string.Empty;
    public string UptimePercent { get; set; } = string.Empty;
    public string DowntimePercent { get; set; } = string.Empty;
    public string Downtime { get; set; } = string.Empty;
    public int OutageCount { get; set; }
    public int PeakConcurrentSessions { get; set; }
}

public class FleetHealthReport : ReportBase<FleetHealthParameters, FleetHealthReportModel>
{
    private static readonly string[] SessionEventTypes =
    [
        EventMessage.NewSession,
        EventMessage.ExpiredSession
    ];

    private readonly IClientStatusRepository _clientStatusRepository;
    private readonly IEventLogRepository _eventLogRepository;

    public FleetHealthReport(
        IClientStatusRepository clientStatusRepository,
        IEventLogRepository eventLogRepository)
    {
        _clientStatusRepository = clientStatusRepository;
        _eventLogRepository = eventLogRepository;
    }

    public override string Id => "fleet-health";
    public override string Name => "Fleet Health & Availability";
    public override string Category => "Operations";
    public override string Description =>
        "Shows live fleet session health and period uptime across clients, including under-provisioned and restart-required debt.";
    public override Type BodyComponentType => typeof(FleetHealthReportView);
    public override ReportPageOrientation PageOrientation => ReportPageOrientation.Landscape;

    public override async Task<object> ExecuteAsync(FleetHealthParameters parameters, CancellationToken cancellationToken = default)
    {
        var (from, to) = ReportDateRange.Validate(parameters.From, parameters.To);
        var clientName = ReportDateRange.NormalizeOptionalClient(parameters.ClientName);
        var instance = string.IsNullOrWhiteSpace(parameters.Instance) ? null : parameters.Instance.Trim();
        var minSessions = parameters.MinSessions < 0 ? 0 : parameters.MinSessions;

        if (clientName is not null)
            ThrowIfNoAccess(clientName);

        var clients = (await _clientStatusRepository.GetAllClients(RequireAuthenticatedUser()))
            .Where(c => clientName is null || string.Equals(c.Name, clientName, StringComparison.OrdinalIgnoreCase))
            .Where(c => instance is null || string.Equals(c.Instance ?? string.Empty, instance, StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => c.Instance ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var zeroSessionClients = clients
            .Where(c => c.RunSessions.Count == 0)
            .Select(ToClientRow)
            .ToList();

        var belowMinSessions = clients
            .Where(c => c.RunSessions.Count < minSessions)
            .Select(ToClientRow)
            .ToList();

        var unhealthySessions = clients
            .SelectMany(c => c.RunSessions
                .Where(s => s.HealthStatus != FigHealthStatus.Healthy)
                .Select(s => new FleetUnhealthySessionRow
                {
                    ClientDisplay = ReportValueFormatter.FormatClientDisplay(c.Name, c.Instance),
                    Hostname = s.Hostname ?? string.Empty,
                    HealthStatus = s.HealthStatus.ToString(),
                    ApplicationVersion = s.ApplicationVersion,
                    LastSeen = s.LastSeen
                }))
            .OrderBy(r => r.ClientDisplay, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.Hostname, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var restartDebt = clients
            .SelectMany(c => c.RunSessions
                .Where(s => s.RestartRequiredToApplySettings)
                .Select(s => new FleetRestartDebtRow
                {
                    ClientDisplay = ReportValueFormatter.FormatClientDisplay(c.Name, c.Instance),
                    Hostname = s.Hostname ?? string.Empty,
                    ApplicationVersion = s.ApplicationVersion,
                    LastSeen = s.LastSeen
                }))
            .OrderBy(r => r.ClientDisplay, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.Hostname, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var sessionEvents = await _eventLogRepository.GetEventsByTypes(from, to, SessionEventTypes, RequireAuthenticatedUser(), clientName, instance);
        var eventsByClient = sessionEvents
            .Where(e => !string.IsNullOrWhiteSpace(e.ClientName))
            .GroupBy(e => (Name: e.ClientName!, Instance: e.Instance), ClientKeyComparer.Instance)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<EventLogBusinessEntity>)g.OrderBy(e => e.Timestamp).ToList());

        var uptimeInputs = clients.Select(c =>
        {
            var key = (c.Name, c.Instance);
            eventsByClient.TryGetValue(key, out var events);
            return (
                c.Name,
                c.Instance,
                Events: events ?? Array.Empty<EventLogBusinessEntity>(),
                ActiveSessions: (IReadOnlyList<ClientRunSessionBusinessEntity>)c.RunSessions.ToList());
        });

        var uptime = FleetUptimeAggregator.Aggregate(from, to, uptimeInputs);
        var totalDowntime = uptime.Aggregate(TimeSpan.Zero, (acc, r) => acc + r.Downtime);
        var avgUptime = uptime.Count == 0 ? 0 : uptime.Average(r => r.UptimePercent);

        return new FleetHealthReportModel
        {
            ScopeDisplay = clientName is null
                ? "All clients"
                : ReportValueFormatter.FormatClientDisplay(clientName, instance),
            Summary =
            [
                new SummaryCardItem("Clients", clients.Count.ToString()),
                new SummaryCardItem("Zero Sessions", zeroSessionClients.Count.ToString()),
                new SummaryCardItem("Unhealthy Sessions", unhealthySessions.Count.ToString()),
                new SummaryCardItem($"Below Min Sessions ({minSessions})", belowMinSessions.Count.ToString()),
                new SummaryCardItem("Restart Required", restartDebt.Count.ToString()),
                new SummaryCardItem("Avg Uptime", $"{avgUptime:0.##}%"),
                new SummaryCardItem("Total Downtime", FormatDuration(totalDowntime))
            ],
            ZeroSessionClients = zeroSessionClients,
            UnhealthySessions = unhealthySessions,
            BelowMinSessions = belowMinSessions,
            RestartRequiredDebt = restartDebt,
            UptimeRows = uptime.Select(r => new FleetUptimeTableRow
            {
                ClientDisplay = r.ClientDisplay,
                UptimePercent = $"{r.UptimePercent:0.##}%",
                DowntimePercent = $"{r.DowntimePercent:0.##}%",
                Downtime = FormatDuration(r.Downtime),
                OutageCount = r.OutageCount,
                PeakConcurrentSessions = r.PeakConcurrentSessions
            }).ToList()
        };
    }

    private static FleetClientRow ToClientRow(ClientStatusBusinessEntity client)
        => new()
        {
            ClientDisplay = ReportValueFormatter.FormatClientDisplay(client.Name, client.Instance),
            SessionCount = client.RunSessions.Count,
            LastRegistration = client.LastRegistration
        };

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

    private sealed class ClientKeyComparer : IEqualityComparer<(string Name, string? Instance)>
    {
        public static readonly ClientKeyComparer Instance = new();

        public bool Equals((string Name, string? Instance) x, (string Name, string? Instance) y)
            => string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase)
               && string.Equals(x.Instance ?? string.Empty, y.Instance ?? string.Empty, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((string Name, string? Instance) obj)
            => HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Name),
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Instance ?? string.Empty));
    }
}
