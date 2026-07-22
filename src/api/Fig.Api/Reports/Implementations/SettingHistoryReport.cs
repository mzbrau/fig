using System.Net;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.ExtensionMethods;
using Fig.Api.Reports.Rendering;
using Fig.Api.Reports.Rendering.Components;
using Fig.Api.Reports.Rendering.Views;
using Fig.Common.Constants;
using Fig.Contracts;
using Fig.Contracts.ExtensionMethods;
using Fig.Contracts.Reports;
using Fig.Contracts.SettingDefinitions;
using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.BusinessEntities.SettingValues;

namespace Fig.Api.Reports.Implementations;

public class SettingHistoryParameters
{
    [ReportParameter("Client", LookupKind = ReportParameterLookupKind.Clients)]
    public string ClientName { get; set; } = string.Empty;

    [ReportParameter("Instance")]
    public string? Instance { get; set; }

    [ReportParameter("Setting", LookupKind = ReportParameterLookupKind.ClientSettings)]
    public string SettingName { get; set; } = string.Empty;
}

public class SettingHistoryReportModel
{
    public string ClientDisplay { get; set; } = string.Empty;
    public string SettingName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ValueType { get; set; } = string.Empty;
    public string DescriptionHtml { get; set; } = string.Empty;
    public bool IsSecret { get; set; }
    public bool IsDataGrid { get; set; }
    public string CurrentValueHtml { get; set; } = string.Empty;
    public IReadOnlyList<SummaryCardItem> Summary { get; set; } = [];
    public IReadOnlyList<TimelineItem> Timeline { get; set; } = [];
    public IReadOnlyList<SettingHistoryRow> Rows { get; set; } = [];
}

public class SettingHistoryRow
{
    public DateTime ChangedAt { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public string ValueHtml { get; set; } = string.Empty;
    public string? ChangeMessage { get; set; }
}

public class SettingHistoryReport : ReportBase<SettingHistoryParameters, SettingHistoryReportModel>
{
    private const string SeeBelow = "See below";

    private readonly ISettingClientRepository _settingClientRepository;
    private readonly ISettingHistoryRepository _settingHistoryRepository;

    public SettingHistoryReport(
        ISettingClientRepository settingClientRepository,
        ISettingHistoryRepository settingHistoryRepository)
    {
        _settingClientRepository = settingClientRepository;
        _settingHistoryRepository = settingHistoryRepository;
    }

    public override string Id => "setting-history";
    public override string Name => "Setting History Report";
    public override string Category => "Settings";
    public override string Description => "Displays current and historical values for a setting.";
    public override Type BodyComponentType => typeof(SettingHistoryReportView);

    public override async Task<object> ExecuteAsync(SettingHistoryParameters parameters, CancellationToken cancellationToken = default)
    {
        ThrowIfNoAccess(parameters.ClientName);
        var client = await _settingClientRepository.GetClient(parameters.ClientName, parameters.Instance)
                     ?? throw new KeyNotFoundException($"Client '{parameters.ClientName}' was not found.");

        var setting = client.Settings.FirstOrDefault(s =>
                          string.Equals(s.Name, parameters.SettingName, StringComparison.OrdinalIgnoreCase))
                      ?? throw new KeyNotFoundException($"Setting '{parameters.SettingName}' was not found on client '{parameters.ClientName}'.");

        var history = await _settingHistoryRepository.GetAll(client.Id, setting.Name);
        var isSecret = setting.IsSecret;
        var isDataGrid = IsDataGridSetting(setting);
        var definition = setting.GetDataGridDefinition();
        var category = string.IsNullOrWhiteSpace(setting.CategoryName) ? "General" : setting.CategoryName!;
        var currentValueHtml = FormatValueHtml(setting.Value, isSecret, isDataGrid, definition);

        var rows = history.Select(h => new SettingHistoryRow
        {
            ChangedAt = h.ChangedAt,
            ChangedBy = h.ChangedBy,
            ValueHtml = FormatValueHtml(h.Value, isSecret, IsDataGridValue(h.Value) || isDataGrid, definition),
            ChangeMessage = h.ChangeMessage
        }).ToList();

        var summaryCurrentValue = isSecret
            ? SecretConstants.SecretPlaceholder
            : isDataGrid
                ? SeeBelow
                : Truncate(ReportValueFormatter.FormatSettingValue(setting.Value), 40);

        var timeline = rows.Take(40).Select(r => new TimelineItem(
            r.ChangedAt,
            r.ChangedBy,
            TimelineDetail(r, isSecret, isDataGrid))).ToList();

        return new SettingHistoryReportModel
        {
            ClientDisplay = ReportValueFormatter.FormatClientDisplay(client.Name, client.Instance),
            SettingName = setting.Name,
            Category = category,
            ValueType = ReportValueFormatter.FormatFriendlyType(setting.ValueType),
            DescriptionHtml = ReportMarkdown.ToHtml(setting.Description),
            IsSecret = isSecret,
            IsDataGrid = isDataGrid,
            CurrentValueHtml = currentValueHtml,
            Summary =
            [
                new SummaryCardItem("History Entries", rows.Count.ToString()),
                new SummaryCardItem("Current Value", summaryCurrentValue),
                new SummaryCardItem("Secret", isSecret ? "Yes" : "No"),
                new SummaryCardItem("Category", category)
            ],
            Timeline = timeline,
            Rows = rows
        };
    }

    private static string TimelineDetail(SettingHistoryRow row, bool isSecret, bool isDataGrid)
    {
        if (isSecret)
            return SecretConstants.SecretPlaceholder;
        if (isDataGrid)
            return SeeBelow;
        return Truncate(StripTags(row.ValueHtml), 80);
    }

    private static string FormatValueHtml(
        SettingValueBaseBusinessEntity? value,
        bool isSecret,
        bool asDataGrid,
        DataGridDefinitionDataContract? definition)
        => FormatValueHtmlCore(value, isSecret, asDataGrid, definition);

    internal static string FormatValueHtmlCore(
        SettingValueBaseBusinessEntity? value,
        bool isSecret,
        bool asDataGrid,
        DataGridDefinitionDataContract? definition)
    {
        if (isSecret)
            return WebUtility.HtmlEncode(SecretConstants.SecretPlaceholder);

        if (asDataGrid || value is DataGridSettingBusinessEntity)
        {
            var rows = (value as DataGridSettingBusinessEntity)?.Value;
            return ReportDataGridHtml.Build(rows, definition);
        }

        return ReportValueFormatter.FormatValueAsHtml(value);
    }

    private static bool IsDataGridSetting(SettingBusinessEntity setting)
        => setting.Value is DataGridSettingBusinessEntity ||
           setting.ValueType.FigPropertyType() == FigPropertyType.DataGrid;

    private static bool IsDataGridValue(SettingValueBaseBusinessEntity? value)
        => value is DataGridSettingBusinessEntity;

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..(max - 1)] + "…";

    private static string StripTags(string html)
    {
        if (string.IsNullOrEmpty(html))
            return string.Empty;

        var result = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ");
        return WebUtility.HtmlDecode(result).Trim();
    }
}
