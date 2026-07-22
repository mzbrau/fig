using Fig.Api.Datalayer.Repositories;
using Fig.Api.Reports.Rendering.Components;
using Fig.Api.Reports.Rendering.Views;
using Fig.Common.Constants;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Reports.Implementations;

public class StaleConfigParameters
{
    [ReportParameter("From")]
    public DateTime From { get; set; }

    [ReportParameter("To")]
    public DateTime To { get; set; }

    [ReportParameter("Stale Days")]
    public int StaleDays { get; set; } = 90;
}

public class StaleConfigReportModel
{
    public IReadOnlyList<SummaryCardItem> Summary { get; set; } = [];
    public IReadOnlyList<StaleSettingRow> StaleSettings { get; set; } = [];
    public IReadOnlyList<SilentClientRow> NoSettingsRead { get; set; } = [];
    public IReadOnlyList<SilentClientRow> OrphanedSilentClients { get; set; } = [];
}

public class StaleSettingRow
{
    public string ClientDisplay { get; set; } = string.Empty;
    public string SettingName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime? LastChanged { get; set; }
    public string AgeDays { get; set; } = string.Empty;
}

public class SilentClientRow
{
    public string ClientDisplay { get; set; } = string.Empty;
    public DateTime? LastRegistration { get; set; }
    public int ActiveSessions { get; set; }
}

public class StaleConfigReport : ReportBase<StaleConfigParameters, StaleConfigReportModel>
{
    private static readonly string[] SettingsReadTypes = [EventMessage.SettingsRead];

    private static readonly string[] SessionEventTypes =
    [
        EventMessage.NewSession,
        EventMessage.ExpiredSession
    ];

    private readonly ISettingClientRepository _settingClientRepository;
    private readonly IClientStatusRepository _clientStatusRepository;
    private readonly IEventLogRepository _eventLogRepository;

    public StaleConfigReport(
        ISettingClientRepository settingClientRepository,
        IClientStatusRepository clientStatusRepository,
        IEventLogRepository eventLogRepository)
    {
        _settingClientRepository = settingClientRepository;
        _clientStatusRepository = clientStatusRepository;
        _eventLogRepository = eventLogRepository;
    }

    public override string Id => "stale-config";
    public override string Name => "Stale Config Report";
    public override string Category => "Analytics";
    public override string Description =>
        "Highlights settings that have not changed for a long time and clients that are silent (no settings reads or sessions) in the selected range.";
    public override Type BodyComponentType => typeof(StaleConfigReportView);
    public override ReportPageOrientation PageOrientation => ReportPageOrientation.Landscape;

    public override async Task<object> ExecuteAsync(StaleConfigParameters parameters, CancellationToken cancellationToken = default)
    {
        var (from, to) = ReportDateRange.Validate(parameters.From, parameters.To);
        if (parameters.StaleDays < 1)
            throw new ReportParameterValidationException("Stale Days must be at least 1.");

        var staleCutoff = DateTime.UtcNow.AddDays(-parameters.StaleDays);
        var clients = await _settingClientRepository.GetAllClients(RequireAuthenticatedUser());
        var statuses = await _clientStatusRepository.GetAllClients(RequireAuthenticatedUser());
        var statusByKey = statuses.ToDictionary(
            c => ClientKey(c.Name, c.Instance),
            c => c,
            StringComparer.OrdinalIgnoreCase);

        var inventory = SettingInventoryProjector.ProjectAll(clients);
        var staleSettings = BuildStaleSettings(inventory, staleCutoff);

        var settingsReadEvents = await _eventLogRepository.GetEventsByTypes(from, to, SettingsReadTypes, RequireAuthenticatedUser());
        var clientsWithSettingsRead = settingsReadEvents
            .Where(e => !string.IsNullOrWhiteSpace(e.ClientName))
            .Select(e => ClientKey(e.ClientName!, e.Instance))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var sessionEvents = await _eventLogRepository.GetEventsByTypes(from, to, SessionEventTypes, RequireAuthenticatedUser());
        var clientsWithSessionEvents = sessionEvents
            .Where(e => !string.IsNullOrWhiteSpace(e.ClientName))
            .Select(e => ClientKey(e.ClientName!, e.Instance))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var (noSettingsRead, orphanedSilent) = ClassifySilentClients(
            clients,
            statusByKey,
            clientsWithSettingsRead,
            clientsWithSessionEvents);

        return new StaleConfigReportModel
        {
            Summary =
            [
                new SummaryCardItem("Stale Settings", staleSettings.Count.ToString(), $"Older than {parameters.StaleDays}d"),
                new SummaryCardItem("No Settings Read", noSettingsRead.Count.ToString()),
                new SummaryCardItem("Orphaned / Silent", orphanedSilent.Count.ToString()),
                new SummaryCardItem("Total Clients", clients.Count.ToString())
            ],
            StaleSettings = staleSettings,
            NoSettingsRead = noSettingsRead,
            OrphanedSilentClients = orphanedSilent
        };
    }

    internal static IReadOnlyList<StaleSettingRow> BuildStaleSettings(
        IEnumerable<SettingInventoryRow> inventory,
        DateTime staleCutoff)
    {
        return inventory
            .Where(r => r.LastChanged is null || r.LastChanged < staleCutoff)
            .Select(r => new StaleSettingRow
            {
                ClientDisplay = r.ClientDisplay,
                SettingName = r.SettingName,
                Category = r.Category,
                LastChanged = r.LastChanged,
                AgeDays = r.LastChanged is null
                    ? "Never"
                    : ((int)(DateTime.UtcNow - r.LastChanged.Value).TotalDays).ToString()
            })
            .OrderByDescending(r => r.LastChanged.HasValue ? 0 : 1)
            .ThenBy(r => r.LastChanged ?? DateTime.MinValue)
            .ThenBy(r => r.ClientDisplay, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    internal static (List<SilentClientRow> NoSettingsRead, List<SilentClientRow> OrphanedSilent) ClassifySilentClients(
        IEnumerable<SettingClientBusinessEntity> clients,
        IReadOnlyDictionary<string, ClientStatusBusinessEntity> statusByKey,
        ISet<string> clientsWithSettingsRead,
        ISet<string> clientsWithSessionEvents)
    {
        var noSettingsRead = new List<SilentClientRow>();
        var orphanedSilent = new List<SilentClientRow>();

        foreach (var client in clients.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(c => c.Instance, StringComparer.OrdinalIgnoreCase))
        {
            var key = ClientKey(client.Name, client.Instance);
            statusByKey.TryGetValue(key, out var status);
            var activeSessions = status?.RunSessions?.Count ?? 0;
            var row = new SilentClientRow
            {
                ClientDisplay = ReportValueFormatter.FormatClientDisplay(client.Name, client.Instance),
                LastRegistration = client.LastRegistration,
                ActiveSessions = activeSessions
            };

            if (!clientsWithSettingsRead.Contains(key))
                noSettingsRead.Add(row);

            if (!clientsWithSessionEvents.Contains(key) && activeSessions == 0)
                orphanedSilent.Add(row);
        }

        return (noSettingsRead, orphanedSilent);
    }

    internal static string ClientKey(string name, string? instance)
        => string.IsNullOrWhiteSpace(instance) ? name : $"{name}|{instance}";
}
