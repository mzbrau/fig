using Fig.Api.Datalayer.Repositories;
using Fig.Api.Reports.Rendering;
using Fig.Api.Reports.Rendering.Components;
using Fig.Api.Reports.Rendering.Views;
using Fig.Api.Secrets;
using Fig.Contracts.Reports;
using Fig.Contracts.SettingGroups;
using Fig.Datalayer.BusinessEntities;
using Newtonsoft.Json;

namespace Fig.Api.Reports.Implementations;

public class SettingGroupsCoverageParameters
{
    [ReportParameter("Group Name", LookupKind = ReportParameterLookupKind.Groups)]
    public string? GroupName { get; set; }
}

public class SettingGroupsCoverageReportModel
{
    public string ScopeDisplay { get; set; } = string.Empty;
    public IReadOnlyList<SummaryCardItem> Summary { get; set; } = [];
    public IReadOnlyList<SettingGroupMembershipRow> Membership { get; set; } = [];
    public IReadOnlyList<SettingGroupDivergenceRow> Divergences { get; set; } = [];
}

public class SettingGroupMembershipRow
{
    public string GroupName { get; set; } = string.Empty;
    public string GroupedSettingName { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string SettingName { get; set; } = string.Empty;
}

public class SettingGroupDivergenceRow
{
    public string GroupName { get; set; } = string.Empty;
    public string GroupedSettingName { get; set; } = string.Empty;
    public string ClientDisplay { get; set; } = string.Empty;
    public string SettingName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string DistinctValues { get; set; } = string.Empty;
}

public class SettingGroupsCoverageReport : ReportBase<SettingGroupsCoverageParameters, SettingGroupsCoverageReportModel>
{
    private static readonly JsonSerializerSettings GroupSettingsJsonSettings = new()
    {
        TypeNameHandling = TypeNameHandling.None
    };

    private readonly ISettingGroupRepository _settingGroupRepository;
    private readonly ISettingClientRepository _settingClientRepository;
    private readonly ISecretStoreHandler _secretStoreHandler;

    public SettingGroupsCoverageReport(
        ISettingGroupRepository settingGroupRepository,
        ISettingClientRepository settingClientRepository,
        ISecretStoreHandler secretStoreHandler)
    {
        _settingGroupRepository = settingGroupRepository;
        _settingClientRepository = settingClientRepository;
        _secretStoreHandler = secretStoreHandler;
    }

    public override string Id => "setting-groups-coverage";
    public override string Name => "Setting Groups Coverage Report";
    public override string Category => "Compliance";
    public override string Description =>
        "Maps setting group membership and flags members whose current values diverge across the group.";
    public override Type BodyComponentType => typeof(SettingGroupsCoverageReportView);
    public override ReportPageOrientation PageOrientation => ReportPageOrientation.Landscape;

    public override async Task<object> ExecuteAsync(SettingGroupsCoverageParameters parameters, CancellationToken cancellationToken = default)
    {
        var groupFilter = string.IsNullOrWhiteSpace(parameters.GroupName)
            ? null
            : parameters.GroupName.Trim();

        var groups = (await _settingGroupRepository.GetAllGroups())
            .Where(g => groupFilter is null ||
                        string.Equals(g.Name, groupFilter, StringComparison.OrdinalIgnoreCase))
            .OrderBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var clients = await _settingClientRepository.GetAllClients(RequireAuthenticatedUser());
        foreach (var client in clients)
            await _secretStoreHandler.HydrateSecrets(client);

        var clientsByName = clients
            .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<SettingClientBusinessEntity>)g.ToList(), StringComparer.OrdinalIgnoreCase);

        var membership = new List<SettingGroupMembershipRow>();
        var divergences = new List<SettingGroupDivergenceRow>();
        var groupedSettingCount = 0;

        foreach (var group in groups)
        {
            var groupedSettings = DeserializeGroupedSettings(group.GroupSettingsJson);
            groupedSettingCount += groupedSettings.Count;

            foreach (var groupedSetting in groupedSettings)
            {
                var sources = groupedSetting.SourceSettings ?? [];
                foreach (var source in sources)
                {
                    membership.Add(new SettingGroupMembershipRow
                    {
                        GroupName = group.Name,
                        GroupedSettingName = groupedSetting.Name,
                        ClientName = source.ClientName,
                        SettingName = source.SettingName
                    });
                }

                AppendDivergences(group.Name, groupedSetting, clientsByName, divergences);
            }
        }

        return new SettingGroupsCoverageReportModel
        {
            ScopeDisplay = groupFilter ?? "All groups",
            Summary =
            [
                new SummaryCardItem("Groups", groups.Count.ToString()),
                new SummaryCardItem("Grouped Settings", groupedSettingCount.ToString()),
                new SummaryCardItem("Membership Links", membership.Count.ToString()),
                new SummaryCardItem("Divergence Rows", divergences.Count.ToString())
            ],
            Membership = membership
                .OrderBy(r => r.GroupName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(r => r.GroupedSettingName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(r => r.ClientName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(r => r.SettingName, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Divergences = divergences
                .OrderBy(r => r.GroupName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(r => r.GroupedSettingName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(r => r.ClientDisplay, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }

    private static void AppendDivergences(
        string groupName,
        GroupedSettingDataContract groupedSetting,
        IReadOnlyDictionary<string, IReadOnlyList<SettingClientBusinessEntity>> clientsByName,
        List<SettingGroupDivergenceRow> divergences)
        => AppendDivergencesCore(groupName, groupedSetting, clientsByName, divergences);

    internal static void AppendDivergencesCore(
        string groupName,
        GroupedSettingDataContract groupedSetting,
        IReadOnlyDictionary<string, IReadOnlyList<SettingClientBusinessEntity>> clientsByName,
        List<SettingGroupDivergenceRow> divergences)
    {
        var memberValues = new List<(string ClientDisplay, string SettingName, string CompareValue, string DisplayValue)>();

        foreach (var source in groupedSetting.SourceSettings ?? [])
        {
            if (!clientsByName.TryGetValue(source.ClientName, out var matchingClients))
            {
                memberValues.Add((source.ClientName, source.SettingName, "(missing client)", "(missing client)"));
                continue;
            }

            var found = false;
            foreach (var client in matchingClients)
            {
                var setting = client.Settings.FirstOrDefault(s =>
                    string.Equals(s.Name, source.SettingName, StringComparison.Ordinal));
                if (setting is null)
                    continue;

                found = true;
                var compareValue = ReportValueFormatter.FormatSettingValue(setting.Value);
                var displayValue = setting.IsSecret
                    ? ReportDataGridHtml.SecretMask
                    : compareValue;
                memberValues.Add((
                    ReportValueFormatter.FormatClientDisplay(client.Name, client.Instance),
                    source.SettingName,
                    compareValue,
                    displayValue));
            }

            if (!found)
                memberValues.Add((source.ClientName, source.SettingName, "(missing setting)", "(missing setting)"));
        }

        var distinct = memberValues
            .Select(m => m.CompareValue)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (distinct.Count <= 1)
            return;

        foreach (var member in memberValues)
        {
            divergences.Add(new SettingGroupDivergenceRow
            {
                GroupName = groupName,
                GroupedSettingName = groupedSetting.Name,
                ClientDisplay = member.ClientDisplay,
                SettingName = member.SettingName,
                Value = member.DisplayValue,
                DistinctValues = distinct.Count.ToString()
            });
        }
    }

    private static List<GroupedSettingDataContract> DeserializeGroupedSettings(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        return JsonConvert.DeserializeObject<List<GroupedSettingDataContract>>(json, GroupSettingsJsonSettings)
               ?? [];
    }
}
