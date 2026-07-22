using Fig.Api.Datalayer.Repositories;
using Fig.Api.Reports.Rendering.Components;
using Fig.Api.Reports.Rendering.Views;
using Fig.Common.Constants;
using Fig.Contracts.Reports;
using Fig.Contracts.SettingGroups;
using Fig.Datalayer.BusinessEntities;
using Newtonsoft.Json;

namespace Fig.Api.Reports.Implementations;

public class BlastRadiusParameters
{
    [ReportParameter("Client", LookupKind = ReportParameterLookupKind.Clients)]
    public string ClientName { get; set; } = string.Empty;

    [ReportParameter("Instance")]
    public string? Instance { get; set; }

    [ReportParameter("Setting", LookupKind = ReportParameterLookupKind.ClientSettings)]
    public string SettingName { get; set; } = string.Empty;

    [ReportParameter("Include Group Members")]
    public bool IncludeGroupMembers { get; set; } = true;
}

public class BlastRadiusReportModel
{
    public string TargetDisplay { get; set; } = string.Empty;
    public IReadOnlyList<SummaryCardItem> Summary { get; set; } = [];
    public IReadOnlyList<BlastRadiusGroupRow> MatchingGroups { get; set; } = [];
    public IReadOnlyList<BlastRadiusAffectedRow> Affected { get; set; } = [];
    public IReadOnlyList<BlastRadiusChangeRow> RecentChanges { get; set; } = [];
}

public class BlastRadiusGroupRow
{
    public string GroupName { get; set; } = string.Empty;
    public int MemberCount { get; set; }
}

public class BlastRadiusAffectedRow
{
    public string ClientDisplay { get; set; } = string.Empty;
    public string SettingName { get; set; } = string.Empty;
    public string SupportsLiveUpdate { get; set; } = string.Empty;
    public int LiveSessions { get; set; }
    public int RestartRequiredSessions { get; set; }
}

public class BlastRadiusChangeRow
{
    public DateTime Timestamp { get; set; }
    public string ClientDisplay { get; set; } = string.Empty;
    public string? SettingName { get; set; }
    public string? User { get; set; }
    public string? Message { get; set; }
}

public class BlastRadiusReport : ReportBase<BlastRadiusParameters, BlastRadiusReportModel>
{
    private static readonly JsonSerializerSettings GroupJsonSettings = new()
    {
        TypeNameHandling = TypeNameHandling.None
    };

    private static readonly string[] RelatedChangeTypes =
    [
        EventMessage.SettingValueUpdated,
        EventMessage.ExternallyManagedSettingUpdatedByUser
    ];

    private readonly ISettingClientRepository _settingClientRepository;
    private readonly ISettingGroupRepository _settingGroupRepository;
    private readonly IClientStatusRepository _clientStatusRepository;
    private readonly IEventLogRepository _eventLogRepository;

    public BlastRadiusReport(
        ISettingClientRepository settingClientRepository,
        ISettingGroupRepository settingGroupRepository,
        IClientStatusRepository clientStatusRepository,
        IEventLogRepository eventLogRepository)
    {
        _settingClientRepository = settingClientRepository;
        _settingGroupRepository = settingGroupRepository;
        _clientStatusRepository = clientStatusRepository;
        _eventLogRepository = eventLogRepository;
    }

    public override string Id => "blast-radius";
    public override string Name => "Blast Radius Report";
    public override string Category => "Analytics";
    public override string Description =>
        "Shows which clients and settings are affected when a setting changes, including setting-group members, live sessions, and recent related updates.";
    public override Type BodyComponentType => typeof(BlastRadiusReportView);
    public override ReportPageOrientation PageOrientation => ReportPageOrientation.Landscape;

    public override async Task<object> ExecuteAsync(BlastRadiusParameters parameters, CancellationToken cancellationToken = default)
    {
        var instance = string.IsNullOrWhiteSpace(parameters.Instance) ? null : parameters.Instance.Trim();
        ThrowIfNoAccess(parameters.ClientName);
        var client = await _settingClientRepository.GetClient(parameters.ClientName, instance)
                     ?? throw new KeyNotFoundException($"Client '{parameters.ClientName}' was not found.");

        var setting = client.Settings.FirstOrDefault(s =>
                          string.Equals(s.Name, parameters.SettingName, StringComparison.OrdinalIgnoreCase))
                      ?? throw new KeyNotFoundException(
                          $"Setting '{parameters.SettingName}' was not found on client '{parameters.ClientName}'.");

        var groups = await _settingGroupRepository.GetAllGroups();
        var matchingGroups = new List<(SettingGroupBusinessEntity Group, List<SourceSettingDataContract> Members)>();

        foreach (var group in groups)
        {
            var members = DeserializeMembers(group.GroupSettingsJson);
            if (members.Any(m =>
                    string.Equals(m.ClientName, parameters.ClientName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(m.SettingName, parameters.SettingName, StringComparison.OrdinalIgnoreCase)))
            {
                matchingGroups.Add((group, members));
            }
        }

        var affectedKeys = ExpandAffectedKeys(
            parameters.ClientName,
            setting.Name,
            parameters.IncludeGroupMembers,
            matchingGroups.Select(g => g.Members));

        var allClients = await _settingClientRepository.GetAllClients(RequireAuthenticatedUser());
        var allStatuses = await _clientStatusRepository.GetAllClients(RequireAuthenticatedUser());
        var statusByKey = allStatuses.ToDictionary(
            c => ClientKey(c.Name, c.Instance),
            c => c,
            StringComparer.OrdinalIgnoreCase);

        var affectedRows = BuildAffectedRows(affectedKeys, allClients, statusByKey);

        var to = DateTime.UtcNow;
        var from = to.AddDays(-7);
        var relatedEvents = (await _eventLogRepository.GetEventsByTypes(from, to, RelatedChangeTypes, RequireAuthenticatedUser()))
            .Where(e => !string.IsNullOrWhiteSpace(e.ClientName) &&
                        !string.IsNullOrWhiteSpace(e.SettingName) &&
                        affectedKeys.Contains((e.ClientName!, e.SettingName!)))
            .OrderByDescending(e => e.Timestamp)
            .ToList();

        var totalLiveSessions = affectedRows.Sum(r => r.LiveSessions);
        var restartDebt = affectedRows.Sum(r => r.RestartRequiredSessions);

        return new BlastRadiusReportModel
        {
            TargetDisplay =
                $"{ReportValueFormatter.FormatClientDisplay(parameters.ClientName, instance)} · {setting.Name}",
            Summary =
            [
                new SummaryCardItem("Matching Groups", matchingGroups.Count.ToString()),
                new SummaryCardItem("Affected Settings", affectedKeys.Count.ToString()),
                new SummaryCardItem("Affected Client Rows", affectedRows.Count.ToString()),
                new SummaryCardItem("Live Sessions", totalLiveSessions.ToString()),
                new SummaryCardItem("Restart Required", restartDebt.ToString()),
                new SummaryCardItem("Target Live Update", setting.SupportsLiveUpdate ? "Yes" : "No")
            ],
            MatchingGroups = matchingGroups
                .Select(g => new BlastRadiusGroupRow
                {
                    GroupName = g.Group.Name,
                    MemberCount = g.Members.Count
                })
                .OrderBy(g => g.GroupName, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Affected = affectedRows,
            RecentChanges = relatedEvents.Select(e => new BlastRadiusChangeRow
            {
                Timestamp = e.Timestamp,
                ClientDisplay = ReportValueFormatter.FormatClientDisplay(e.ClientName!, e.Instance),
                SettingName = e.SettingName,
                User = e.AuthenticatedUser,
                Message = e.Message
            }).ToList()
        };
    }

    internal static HashSet<(string ClientName, string SettingName)> ExpandAffectedKeys(
        string clientName,
        string settingName,
        bool includeGroupMembers,
        IEnumerable<IReadOnlyList<SourceSettingDataContract>> matchingGroupMembers)
    {
        var affectedKeys = new HashSet<(string ClientName, string SettingName)>(new ClientSettingComparer())
        {
            (clientName, settingName)
        };

        if (!includeGroupMembers)
            return affectedKeys;

        foreach (var members in matchingGroupMembers)
        {
            foreach (var member in members)
                affectedKeys.Add((member.ClientName, member.SettingName));
        }

        return affectedKeys;
    }

    internal static IReadOnlyList<BlastRadiusAffectedRow> BuildAffectedRows(
        IEnumerable<(string ClientName, string SettingName)> affectedKeys,
        IEnumerable<SettingClientBusinessEntity> allClients,
        IReadOnlyDictionary<string, ClientStatusBusinessEntity> statusByKey)
    {
        var clientsList = allClients.ToList();
        var affectedRows = new List<BlastRadiusAffectedRow>();
        foreach (var (clientName, settingName) in affectedKeys
                     .OrderBy(k => k.ClientName, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(k => k.SettingName, StringComparer.OrdinalIgnoreCase))
        {
            var matchingClients = clientsList
                .Where(c => string.Equals(c.Name, clientName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matchingClients.Count == 0)
            {
                affectedRows.Add(new BlastRadiusAffectedRow
                {
                    ClientDisplay = clientName,
                    SettingName = settingName,
                    SupportsLiveUpdate = "Unknown",
                    LiveSessions = 0,
                    RestartRequiredSessions = 0
                });
                continue;
            }

            foreach (var affectedClient in matchingClients.OrderBy(c => c.Instance, StringComparer.OrdinalIgnoreCase))
            {
                var affectedSetting = affectedClient.Settings.FirstOrDefault(s =>
                    string.Equals(s.Name, settingName, StringComparison.OrdinalIgnoreCase));
                statusByKey.TryGetValue(ClientKey(affectedClient.Name, affectedClient.Instance), out var status);
                var sessions = status?.RunSessions?.ToList() ?? [];

                affectedRows.Add(new BlastRadiusAffectedRow
                {
                    ClientDisplay = ReportValueFormatter.FormatClientDisplay(affectedClient.Name, affectedClient.Instance),
                    SettingName = settingName,
                    SupportsLiveUpdate = affectedSetting is null
                        ? "Unknown"
                        : (affectedSetting.SupportsLiveUpdate ? "Yes" : "No"),
                    LiveSessions = sessions.Count,
                    RestartRequiredSessions = sessions.Count(s => s.RestartRequiredToApplySettings)
                });
            }
        }

        return affectedRows;
    }

    private static List<SourceSettingDataContract> DeserializeMembers(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        var grouped = JsonConvert.DeserializeObject<List<GroupedSettingDataContract>>(json, GroupJsonSettings)
                      ?? [];
        return grouped
            .SelectMany(g => g.SourceSettings ?? [])
            .Where(s => !string.IsNullOrWhiteSpace(s.ClientName) && !string.IsNullOrWhiteSpace(s.SettingName))
            .ToList();
    }

    private static string ClientKey(string name, string? instance)
        => string.IsNullOrWhiteSpace(instance) ? name : $"{name}|{instance}";

    private sealed class ClientSettingComparer : IEqualityComparer<(string ClientName, string SettingName)>
    {
        public bool Equals((string ClientName, string SettingName) x, (string ClientName, string SettingName) y)
            => string.Equals(x.ClientName, y.ClientName, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(x.SettingName, y.SettingName, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((string ClientName, string SettingName) obj)
            => HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.ClientName),
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.SettingName));
    }
}
