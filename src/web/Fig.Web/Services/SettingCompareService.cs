using Fig.Common.NetStandard.Json;
using Fig.Contracts.ImportExport;
using Fig.Web.ExtensionMethods;
using Fig.Web.Facades;
using Fig.Web.Models.Compare;
using Fig.Web.Models.Setting.ConfigurationModels.DataGrid;
using Newtonsoft.Json;

namespace Fig.Web.Services;

public class SettingCompareService : ISettingCompareService
{
    private readonly ISettingClientFacade _settingClientFacade;
    private readonly IDataFacade _dataFacade;

    public SettingCompareService(
        ISettingClientFacade settingClientFacade,
        IDataFacade dataFacade)
    {
        _settingClientFacade = settingClientFacade;
        _dataFacade = dataFacade;
    }

    public async Task<(IReadOnlyList<SettingCompareModel> Rows, CompareStatisticsModel Stats)> CompareAsync(
        FigDataExportDataContract exportData)
    {
        // Ensure settings are loaded
        await _settingClientFacade.LoadAllClients();

        var rows = new List<SettingCompareModel>();

        foreach (var exportClient in exportData.Clients)
        {
            var liveClient = _settingClientFacade.SettingClients
                .FirstOrDefault(c => c.Name == exportClient.Name && c.Instance == exportClient.Instance);

            // Fetch last-changed metadata for this client
            var lastChangedEntries = await _dataFacade.GetLastChangedForAllSettings(
                exportClient.Name, exportClient.Instance);
            var lastChangedLookup = lastChangedEntries?
                .ToDictionary(e => e.Name, e => e)
                ?? new Dictionary<string, Contracts.Settings.SettingValueDataContract>();

            var liveSettingsByName = liveClient?.Settings
                .Where(s => !s.IsGroupManaged)
                .ToDictionary(s => s.Name, s => s)
                ?? new Dictionary<string, Models.Setting.ISetting>();

            // Walk through export settings
            foreach (var exportSetting in exportClient.Settings)
            {
                lastChangedLookup.TryGetValue(exportSetting.Name, out var lastChanged);

                var exportDetails = exportSetting.LastChangedDetails;

                if (liveSettingsByName.TryGetValue(exportSetting.Name, out var liveSetting))
                {
                    var isAdvanced = liveSetting.Advanced || exportSetting.Advanced;

                    // Secret settings cannot be compared meaningfully
                    if (exportSetting.IsSecret || liveSetting.IsSecret)
                    {
                        rows.Add(new SettingCompareModel(
                            exportClient.Name,
                            exportClient.Instance,
                            exportSetting.Name,
                            null,
                            null,
                            CompareStatus.NotCompared,
                            isAdvanced,
                            lastChanged?.ChangedBy,
                            lastChanged?.ChangedAt,
                            lastChanged?.ChangeMessage,
                            exportDetails?.ChangedBy,
                            exportDetails?.ChangedAt,
                            exportDetails?.ChangeMessage));

                        liveSettingsByName.Remove(exportSetting.Name);
                        continue;
                    }

                    var isDataGrid = liveSetting is DataGridSettingConfigurationModel;
                    var liveVal = NormaliseValue(liveSetting.GetStringValue(int.MaxValue));
                    var exportVal = isDataGrid
                        ? SerializeExportValueForDataGrid(exportSetting)
                        : SerializeExportValue(exportSetting);
                    var rawExportJson = isDataGrid ? SerializeExportValueAsJson(exportSetting) : null;

                    var status = AreValuesEquivalent(liveVal, exportVal)
                        ? CompareStatus.Match
                        : CompareStatus.Different;

                    rows.Add(new SettingCompareModel(
                        exportClient.Name,
                        exportClient.Instance,
                        exportSetting.Name,
                        liveVal,
                        exportVal,
                        status,
                        isAdvanced,
                        lastChanged?.ChangedBy,
                        lastChanged?.ChangedAt,
                        lastChanged?.ChangeMessage,
                        exportDetails?.ChangedBy,
                        exportDetails?.ChangedAt,
                        exportDetails?.ChangeMessage)
                    {
                        RawExportJson = rawExportJson
                    });

                    liveSettingsByName.Remove(exportSetting.Name);
                }
                else
                {
                    rows.Add(new SettingCompareModel(
                        exportClient.Name,
                        exportClient.Instance,
                        exportSetting.Name,
                        null,
                        SerializeExportValue(exportSetting),
                        CompareStatus.OnlyInExport,
                        exportSetting.Advanced,
                        lastChanged?.ChangedBy,
                        lastChanged?.ChangedAt,
                        lastChanged?.ChangeMessage,
                        exportDetails?.ChangedBy,
                        exportDetails?.ChangedAt,
                        exportDetails?.ChangeMessage));
                }
            }

            // Remaining live settings that weren't in the export
            foreach (var (name, liveSetting) in liveSettingsByName)
            {
                if (liveSetting.IsGroupManaged)
                    continue;

                lastChangedLookup.TryGetValue(name, out var lastChanged);

                rows.Add(new SettingCompareModel(
                    exportClient.Name,
                    exportClient.Instance,
                    name,
                    NormaliseValue(liveSetting.GetStringValue(int.MaxValue)),
                    null,
                    CompareStatus.OnlyInLive,
                    liveSetting.Advanced,
                    lastChanged?.ChangedBy,
                    lastChanged?.ChangedAt,
                    lastChanged?.ChangeMessage));
            }
        }

        // Also add live clients that don't appear in the export at all
        foreach (var liveClient in _settingClientFacade.SettingClients.Where(c => !c.IsGroup))
        {
            var existsInExport = exportData.Clients.Any(c =>
                c.Name == liveClient.Name && c.Instance == liveClient.Instance);

            if (!existsInExport)
            {
                foreach (var liveSetting in liveClient.Settings.Where(s => !s.IsGroupManaged))
                {
                    rows.Add(new SettingCompareModel(
                        liveClient.Name,
                        liveClient.Instance,
                        liveSetting.Name,
                        NormaliseValue(liveSetting.GetStringValue(int.MaxValue)),
                        null,
                        CompareStatus.OnlyInLive,
                        liveSetting.Advanced));
                }
            }
        }

        var stats = BuildStats(rows);

        return (rows.AsReadOnly(), stats);
    }

    public async Task<(IReadOnlyList<SettingCompareModel> Rows, CompareStatisticsModel Stats)> CompareAsync(
        FigValueOnlyDataExportDataContract exportData)
    {
        await _settingClientFacade.LoadAllClients();

        var rows = new List<SettingCompareModel>();
        var exportClientIdentifiers = new HashSet<string>();

        foreach (var exportClient in exportData.Clients)
        {
            exportClientIdentifiers.Add(GetClientIdentifier(exportClient.Name, exportClient.Instance));

            var liveClient = _settingClientFacade.SettingClients
                .FirstOrDefault(c => c.Name == exportClient.Name && c.Instance == exportClient.Instance);

            var lastChangedEntries = await _dataFacade.GetLastChangedForAllSettings(
                exportClient.Name, exportClient.Instance);
            var lastChangedLookup = lastChangedEntries?
                .ToDictionary(e => e.Name, e => e)
                ?? new Dictionary<string, Contracts.Settings.SettingValueDataContract>();

            var liveSettingsByName = liveClient?.Settings
                .Where(s => !s.IsGroupManaged)
                .ToDictionary(s => s.Name, s => s)
                ?? new Dictionary<string, Models.Setting.ISetting>();

            foreach (var exportSetting in exportClient.Settings)
            {
                lastChangedLookup.TryGetValue(exportSetting.Name, out var lastChanged);

                var exportDetails = exportSetting.LastChangedDetails;

                if (liveSettingsByName.TryGetValue(exportSetting.Name, out var liveSetting))
                {
                    // Encrypted in export or secret in live → not comparable
                    if (exportSetting.IsEncrypted || liveSetting.IsSecret)
                    {
                        rows.Add(new SettingCompareModel(
                            exportClient.Name,
                            exportClient.Instance,
                            exportSetting.Name,
                            null,
                            null,
                            CompareStatus.NotCompared,
                            liveSetting.Advanced,
                            lastChanged?.ChangedBy,
                            lastChanged?.ChangedAt,
                            lastChanged?.ChangeMessage,
                            exportDetails?.ChangedBy,
                            exportDetails?.ChangedAt,
                            exportDetails?.ChangeMessage));

                        liveSettingsByName.Remove(exportSetting.Name);
                        continue;
                    }

                    var isDataGrid = liveSetting is DataGridSettingConfigurationModel;
                    var liveVal = NormaliseValue(liveSetting.GetStringValue(int.MaxValue));
                    var exportVal = isDataGrid
                        ? SerializeValueOnlyExportValueForDataGrid(exportSetting.Value)
                        : SerializeValueOnlyExportValue(exportSetting.Value);
                    var rawExportJson = isDataGrid
                        ? SerializeValueOnlyExportValueAsJson(exportSetting.Value)
                        : null;

                    var status = AreValuesEquivalent(liveVal, exportVal)
                        ? CompareStatus.Match
                        : CompareStatus.Different;

                    rows.Add(new SettingCompareModel(
                        exportClient.Name,
                        exportClient.Instance,
                        exportSetting.Name,
                        liveVal,
                        exportVal,
                        status,
                        liveSetting.Advanced,
                        lastChanged?.ChangedBy,
                        lastChanged?.ChangedAt,
                        lastChanged?.ChangeMessage,
                        exportDetails?.ChangedBy,
                        exportDetails?.ChangedAt,
                        exportDetails?.ChangeMessage)
                    {
                        RawExportJson = rawExportJson
                    });

                    liveSettingsByName.Remove(exportSetting.Name);
                }
                else
                {
                    rows.Add(new SettingCompareModel(
                        exportClient.Name,
                        exportClient.Instance,
                        exportSetting.Name,
                        null,
                        SerializeValueOnlyExportValue(exportSetting.Value),
                        CompareStatus.OnlyInExport,
                        false,
                        lastChanged?.ChangedBy,
                        lastChanged?.ChangedAt,
                        lastChanged?.ChangeMessage,
                        exportDetails?.ChangedBy,
                        exportDetails?.ChangedAt,
                        exportDetails?.ChangeMessage));
                }
            }

            // Remaining live settings not in the export
            foreach (var (name, liveSetting) in liveSettingsByName)
            {
                if (liveSetting.IsGroupManaged)
                    continue;

                lastChangedLookup.TryGetValue(name, out var lastChanged);

                rows.Add(new SettingCompareModel(
                    exportClient.Name,
                    exportClient.Instance,
                    name,
                    NormaliseValue(liveSetting.GetStringValue(int.MaxValue)),
                    null,
                    CompareStatus.OnlyInLive,
                    liveSetting.Advanced,
                    lastChanged?.ChangedBy,
                    lastChanged?.ChangedAt,
                    lastChanged?.ChangeMessage));
            }
        }

        // Live clients not in the export at all
        foreach (var liveClient in _settingClientFacade.SettingClients.Where(c => !c.IsGroup))
        {
            if (!exportClientIdentifiers.Contains(GetClientIdentifier(liveClient.Name, liveClient.Instance)))
            {
                foreach (var liveSetting in liveClient.Settings.Where(s => !s.IsGroupManaged))
                {
                    rows.Add(new SettingCompareModel(
                        liveClient.Name,
                        liveClient.Instance,
                        liveSetting.Name,
                        NormaliseValue(liveSetting.GetStringValue(int.MaxValue)),
                        null,
                        CompareStatus.OnlyInLive,
                        liveSetting.Advanced));
                }
            }
        }

        return (rows.AsReadOnly(), BuildStats(rows));
    }

    private static string GetClientIdentifier(string name, string? instance)
        => instance != null ? $"{name}|{instance}" : name;

    private static CompareStatisticsModel BuildStats(List<SettingCompareModel> rows) => new()
    {
        TotalSettings = rows.Count,
        MatchCount = rows.Count(r => r.Status == CompareStatus.Match),
        DifferenceCount = rows.Count(r => r.Status == CompareStatus.Different),
        OnlyInLiveCount = rows.Count(r => r.Status == CompareStatus.OnlyInLive),
        OnlyInExportCount = rows.Count(r => r.Status == CompareStatus.OnlyInExport),
        NotComparedCount = rows.Count(r => r.Status == CompareStatus.NotCompared)
    };

    private static string SerializeExportValue(SettingExportDataContract setting)
    {
        if (setting.Value is null)
            return string.Empty;

        try
        {
            var raw = setting.Value.GetValue();
            if (raw is null)
                return string.Empty;

            if (raw is string s)
                return s;

            return JsonConvert.SerializeObject(raw, JsonSettings.FigUserFacing);
        }
        catch
        {
            return setting.Value.ToString() ?? string.Empty;
        }
    }

    /// <summary>
    /// Serializes the export value for data grid settings using the same
    /// <see cref="Fig.Web.ExtensionMethods.DictionaryExtensionMethods.ToDataGridStringValue"/>
    /// format that <see cref="DataGridSettingConfigurationModel.GetStringValue"/> uses,
    /// so comparisons are meaningful.
    /// </summary>
    private static string SerializeExportValueForDataGrid(SettingExportDataContract setting)
    {
        if (setting.Value is null)
            return string.Empty;

        try
        {
            var raw = setting.Value.GetValue();
            if (raw is null)
                return string.Empty;

            // The raw value from the export will be a JArray; deserialize it through
            // JSON round-trip into the same List<Dictionary<string, object?>> structure
            // that the live data grid uses.
            var json = JsonConvert.SerializeObject(raw, JsonSettings.FigUserFacing);
            var rows = JsonConvert.DeserializeObject<List<Dictionary<string, object?>>>(json);
            return rows.ToDataGridStringValue(int.MaxValue, false);
        }
        catch
        {
            return SerializeExportValue(setting);
        }
    }

    /// <summary>
    /// Returns the raw JSON representation of a data grid export value so it can be
    /// correctly deserialized when applying as a pending change.
    /// </summary>
    private static string? SerializeExportValueAsJson(SettingExportDataContract setting)
    {
        if (setting.Value is null)
            return null;

        try
        {
            var raw = setting.Value.GetValue();
            if (raw is null)
                return null;

            return JsonConvert.SerializeObject(raw, JsonSettings.FigUserFacing);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Serializes a value-only export setting's value to a display string.
    /// The value is a raw <c>object?</c> (not wrapped in <see cref="SettingValueBaseDataContract"/>).
    /// </summary>
    private static string SerializeValueOnlyExportValue(object? value)
    {
        if (value is null)
            return string.Empty;

        if (value is string s)
            return s;

        try
        {
            return JsonConvert.SerializeObject(value, JsonSettings.FigUserFacing);
        }
        catch
        {
            return value.ToString() ?? string.Empty;
        }
    }

    /// <summary>
    /// Serializes a value-only export value for data grid comparison using the same
    /// display format as the live data grid.
    /// </summary>
    private static string SerializeValueOnlyExportValueForDataGrid(object? value)
    {
        if (value is null)
            return string.Empty;

        try
        {
            var json = JsonConvert.SerializeObject(value, JsonSettings.FigUserFacing);
            var rows = JsonConvert.DeserializeObject<List<Dictionary<string, object?>>>(json);
            return rows.ToDataGridStringValue(int.MaxValue, false);
        }
        catch
        {
            return SerializeValueOnlyExportValue(value);
        }
    }

    /// <summary>
    /// Returns the raw JSON for a value-only data grid value so it can be applied
    /// as a pending change.
    /// </summary>
    private static string? SerializeValueOnlyExportValueAsJson(object? value)
    {
        if (value is null)
            return null;

        try
        {
            return JsonConvert.SerializeObject(value, JsonSettings.FigUserFacing);
        }
        catch
        {
            return null;
        }
    }

    private static bool AreValuesEquivalent(string? live, string? export)
    {
        if (live == export)
            return true;

        if (string.IsNullOrEmpty(live) && string.IsNullOrEmpty(export))
            return true;

        // Normalise whitespace / formatting for rough comparison
        return string.Equals(
            live?.Trim(),
            export?.Trim(),
            StringComparison.Ordinal);
    }

    /// <summary>
    /// Treats the &lt;NOT SET&gt; sentinel produced by <see cref="Models.Setting.SettingConfigurationModel{T}.GetStringValue"/>
    /// as an empty string so it compares equal to an absent/empty export value.
    /// </summary>
    private static string? NormaliseValue(string? value)
    {
        if (value is null or "<NOT SET>")
            return string.Empty;

        return value;
    }
}
