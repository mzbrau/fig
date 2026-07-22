using Fig.Client.Abstractions.Data;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Reports;

public record SettingInventoryRow(
    string ClientName,
    string? Instance,
    string ClientDisplay,
    string SettingName,
    string Category,
    string Classification,
    bool IsSecret,
    bool IsExternallyManaged,
    bool EnvironmentSpecific,
    bool InitOnlyExport,
    bool SupportsLiveUpdate,
    string? LookupTableKey,
    string? LookupKeySettingName,
    DateTime? LastChanged);

public static class SettingInventoryProjector
{
    public static SettingInventoryRow Project(SettingClientBusinessEntity client, SettingBusinessEntity setting)
        => new(
            client.Name,
            client.Instance,
            ReportValueFormatter.FormatClientDisplay(client.Name, client.Instance),
            setting.Name,
            string.IsNullOrWhiteSpace(setting.CategoryName) ? "General" : setting.CategoryName!,
            setting.Classification.ToString(),
            setting.IsSecret,
            setting.IsExternallyManaged,
            setting.EnvironmentSpecific == true,
            setting.InitOnlyExport == true,
            setting.SupportsLiveUpdate,
            setting.LookupTableKey,
            setting.LookupKeySettingName,
            setting.LastChanged);

    public static IReadOnlyList<SettingInventoryRow> ProjectAll(
        IEnumerable<SettingClientBusinessEntity> clients,
        bool secretsOnly = false)
    {
        var rows = new List<SettingInventoryRow>();
        foreach (var client in clients)
        {
            foreach (var setting in client.Settings)
            {
                if (secretsOnly && !setting.IsSecret)
                    continue;
                rows.Add(Project(client, setting));
            }
        }

        return rows
            .OrderBy(r => r.ClientDisplay, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.SettingName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
