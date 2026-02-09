using System.Globalization;
using Fig.Common.Constants;
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
        return await CompareAsyncCore(
            exportData.Clients,
            exportClient => exportClient.Name,
            exportClient => exportClient.Instance,
            exportClient => exportClient.Settings,
            exportSetting => exportSetting.Name,
            exportSetting => exportSetting.LastChangedDetails,
            (exportSetting, liveSetting) => exportSetting.IsSecret || liveSetting.IsSecret,
            (exportSetting, liveSetting) => liveSetting.Advanced || exportSetting.Advanced,
            (exportSetting, isDataGrid) => isDataGrid
                ? SerializeExportValueForDataGrid(exportSetting)
                : SerializeExportValue(exportSetting),
            (exportSetting, isDataGrid) => isDataGrid
                ? SerializeExportValueAsJson(exportSetting)
                : null,
            exportSetting => SerializeExportValue(exportSetting),
            exportSetting => exportSetting.Advanced);
    }

    public async Task<(IReadOnlyList<SettingCompareModel> Rows, CompareStatisticsModel Stats)> CompareAsync(
        FigValueOnlyDataExportDataContract exportData)
    {
        return await CompareAsyncCore(
            exportData.Clients,
            exportClient => exportClient.Name,
            exportClient => exportClient.Instance,
            exportClient => exportClient.Settings,
            exportSetting => exportSetting.Name,
            exportSetting => exportSetting.LastChangedDetails,
            (exportSetting, liveSetting) => exportSetting.IsEncrypted || liveSetting.IsSecret,
            (_, liveSetting) => liveSetting.Advanced,
            (exportSetting, isDataGrid) => isDataGrid
                ? SerializeValueOnlyExportValueForDataGrid(exportSetting.Value)
                : SerializeValueOnlyExportValue(exportSetting.Value),
            (exportSetting, isDataGrid) => isDataGrid
                ? SerializeValueOnlyExportValueAsJson(exportSetting.Value)
                : null,
            exportSetting => SerializeValueOnlyExportValue(exportSetting.Value),
            _ => false);
    }

    private async Task<(IReadOnlyList<SettingCompareModel> Rows, CompareStatisticsModel Stats)> CompareAsyncCore<TExportClient, TExportSetting>(
        IEnumerable<TExportClient> exportClients,
        Func<TExportClient, string> getClientName,
        Func<TExportClient, string?> getClientInstance,
        Func<TExportClient, IEnumerable<TExportSetting>> getExportSettings,
        Func<TExportSetting, string> getSettingName,
        Func<TExportSetting, SettingLastChangedDataContract?> getExportLastChangedDetails,
        Func<TExportSetting, Models.Setting.ISetting, bool> isNotComparable,
        Func<TExportSetting, Models.Setting.ISetting, bool> getComparedIsAdvanced,
        Func<TExportSetting, bool, string> getExportValue,
        Func<TExportSetting, bool, string?> getExportRawJson,
        Func<TExportSetting, string> getOnlyInExportValue,
        Func<TExportSetting, bool> getOnlyInExportIsAdvanced)
    {
        await _settingClientFacade.LoadAllClients();

        // Single bulk call to fetch last-changed metadata for all clients at once
        var lastChangedByClient = await FetchBulkLastChanged();

        var rows = new List<SettingCompareModel>();
        var exportClientIdentifiers = new HashSet<string>();

        foreach (var exportClient in exportClients)
        {
            var clientName = getClientName(exportClient);
            var clientInstance = getClientInstance(exportClient);

            exportClientIdentifiers.Add(GetClientIdentifier(clientName, clientInstance));

            var liveClient = _settingClientFacade.SettingClients
                .FirstOrDefault(c => c.Name == clientName && c.Instance == clientInstance);

            // Retrieve pre-fetched last-changed metadata for this client
            var lastChangedLookup = lastChangedByClient.TryGetValue((clientName, clientInstance), out var entries)
                ? entries
                : new Dictionary<string, Contracts.Settings.SettingValueDataContract>();

            var liveSettingsByName = liveClient?.Settings
                .ToDictionary(s => s.Name, s => s)
                ?? new Dictionary<string, Models.Setting.ISetting>();

            foreach (var exportSetting in getExportSettings(exportClient))
            {
                var settingName = getSettingName(exportSetting);
                lastChangedLookup.TryGetValue(settingName, out var lastChanged);

                var exportDetails = getExportLastChangedDetails(exportSetting);

                if (liveSettingsByName.TryGetValue(settingName, out var liveSetting))
                {
                    var isAdvanced = getComparedIsAdvanced(exportSetting, liveSetting);

                    if (isNotComparable(exportSetting, liveSetting))
                    {
                        rows.Add(new SettingCompareModel(
                            clientName,
                            clientInstance,
                            settingName,
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

                        liveSettingsByName.Remove(settingName);
                        continue;
                    }

                    var isDataGrid = liveSetting is DataGridSettingConfigurationModel;
                    var liveVal = NormaliseValue(liveSetting.GetStringValue(int.MaxValue));
                    var exportVal = getExportValue(exportSetting, isDataGrid);
                    var rawExportJson = getExportRawJson(exportSetting, isDataGrid);

                    var normalisedLiveVal = NormaliseEnumDisplayValue(
                        liveVal,
                        exportVal,
                        IsEnumExportSetting(exportSetting));

                    var status = isDataGrid && liveSetting is DataGridSettingConfigurationModel dataGridSetting
                        && TryCompareDataGrid(dataGridSetting, rawExportJson, out var dataGridMatch)
                        ? dataGridMatch
                            ? CompareStatus.Match
                            : CompareStatus.Different
                        : AreValuesEquivalent(normalisedLiveVal, exportVal)
                            ? CompareStatus.Match
                            : CompareStatus.Different;

                    rows.Add(new SettingCompareModel(
                        clientName,
                        clientInstance,
                        settingName,
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

                    liveSettingsByName.Remove(settingName);
                }
                else
                {
                    rows.Add(new SettingCompareModel(
                        clientName,
                        clientInstance,
                        settingName,
                        null,
                        getOnlyInExportValue(exportSetting),
                        CompareStatus.OnlyInExport,
                        getOnlyInExportIsAdvanced(exportSetting),
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
                lastChangedLookup.TryGetValue(name, out var lastChanged);

                rows.Add(new SettingCompareModel(
                    clientName,
                    clientInstance,
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
                foreach (var liveSetting in liveClient.Settings)
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

    /// <summary>
    /// Fetches last-changed metadata for all clients in a single HTTP call and returns
    /// a dictionary keyed by (clientName, instance).
    /// </summary>
    private async Task<Dictionary<(string Name, string? Instance), Dictionary<string, Contracts.Settings.SettingValueDataContract>>> FetchBulkLastChanged()
    {
        var bulkResult = await _dataFacade.GetLastChangedForAllClientsSettings();
        if (bulkResult is null)
            return new Dictionary<(string Name, string? Instance), Dictionary<string, Contracts.Settings.SettingValueDataContract>>();

        return bulkResult.ToDictionary(
            c => (c.Name, c.Instance),
            c => c.Settings.ToDictionary(s => s.Name, s => s));
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

        if (IsBoolEquivalent(live, export))
            return true;

        return string.Equals(live, export, StringComparison.Ordinal);
    }

    private static bool IsBoolEquivalent(string live, string export)
    {
        return bool.TryParse(live, out var liveBool)
            && bool.TryParse(export, out var exportBool)
            && liveBool == exportBool;
    }

    private static bool IsEnumExportSetting<TExportSetting>(TExportSetting exportSetting)
    {
        return exportSetting is SettingExportDataContract fullExport
            && fullExport.ValueType.IsEnum;
    }

    private static string? NormaliseEnumDisplayValue(string? live, string? export, bool isEnum)
    {
        if (string.IsNullOrWhiteSpace(live))
            return live;

        if (!isEnum)
            return live;

        if (!live.Contains("->", StringComparison.Ordinal))
            return live;

        return StripEnumDisplaySuffix(live);
    }

    private static string StripEnumDisplaySuffix(string value)
    {
        var separatorIndex = value.IndexOf("->", StringComparison.Ordinal);
        if (separatorIndex < 0)
            return value;

        return value[..separatorIndex].Trim();
    }

    private static bool TryCompareDataGrid(
        DataGridSettingConfigurationModel liveSetting,
        string? rawExportJson,
        out bool isMatch)
    {
        isMatch = false;

        if (string.IsNullOrWhiteSpace(rawExportJson))
            return false;

        try
        {
            var exportRows = JsonConvert.DeserializeObject<List<Dictionary<string, object?>>>(rawExportJson);
            if (exportRows is null)
                return false;

            var liveRows = liveSetting.GetValue() as List<Dictionary<string, object?>>
                ?? new List<Dictionary<string, object?>>();

            isMatch = AreDataGridRowsEquivalent(liveRows, exportRows);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool AreDataGridRowsEquivalent(
        IReadOnlyList<Dictionary<string, object?>> liveRows,
        IReadOnlyList<Dictionary<string, object?>> exportRows)
    {
        if (liveRows.Count != exportRows.Count)
            return false;

        for (var i = 0; i < liveRows.Count; i++)
        {
            var liveRow = liveRows[i];
            var exportRow = exportRows[i];

            if (liveRow.Count != exportRow.Count)
                return false;

            foreach (var (key, liveValue) in liveRow)
            {
                if (!exportRow.TryGetValue(key, out var exportValue))
                    return false;

                if (IsSecretPlaceholder(liveValue) || IsSecretPlaceholder(exportValue))
                    continue;

                var liveString = Convert.ToString(liveValue, CultureInfo.InvariantCulture);
                var exportString = Convert.ToString(exportValue, CultureInfo.InvariantCulture);

                if (!AreValuesEquivalent(liveString, exportString))
                    return false;
            }
        }

        return true;
    }

    private static bool IsSecretPlaceholder(object? value)
    {
        return value is string stringValue
            && string.Equals(stringValue, SecretConstants.SecretPlaceholder, StringComparison.Ordinal);
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
