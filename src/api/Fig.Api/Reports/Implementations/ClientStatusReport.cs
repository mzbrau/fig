using Fig.Api.Datalayer.Repositories;
using Fig.Api.Reports.Rendering;
using Fig.Api.Reports.Rendering.Components;
using Fig.Api.Reports.Rendering.Views;
using Fig.Contracts;
using Fig.Contracts.ExtensionMethods;
using Fig.Contracts.Reports;
using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.BusinessEntities.SettingValues;

namespace Fig.Api.Reports.Implementations;

public class ClientStatusParameters
{
    [ReportParameter("Client", LookupKind = ReportParameterLookupKind.Clients)]
    public string ClientName { get; set; } = string.Empty;

    [ReportParameter("Instance")]
    public string? Instance { get; set; }
}

public class ClientStatusReportModel
{
    public string ClientDisplay { get; set; } = string.Empty;
    public string DescriptionHtml { get; set; } = string.Empty;
    public IReadOnlyList<SummaryCardItem> Summary { get; set; } = [];
    public IReadOnlyList<ClientStatusGroup> Groups { get; set; } = [];
}

public class ClientStatusGroup
{
    public string Category { get; set; } = "General";
    public IReadOnlyList<ClientStatusSettingRow> Settings { get; set; } = [];
}

public class ClientStatusSettingRow
{
    public string Name { get; set; } = string.Empty;
    public string DescriptionHtml { get; set; } = string.Empty;
    public string ValueHtml { get; set; } = string.Empty;
    public string ValueType { get; set; } = string.Empty;
}

public class ClientStatusReport : ReportBase<ClientStatusParameters, ClientStatusReportModel>
{
    private readonly ISettingClientRepository _settingClientRepository;

    public ClientStatusReport(ISettingClientRepository settingClientRepository)
    {
        _settingClientRepository = settingClientRepository;
    }

    public override string Id => "client-status";
    public override string Name => "Client Status Report";
    public override string Category => "Clients";
    public override string Description => "Displays all settings for a client, with secret values masked.";
    public override Type BodyComponentType => typeof(ClientStatusReportView);

    public override async Task<object> ExecuteAsync(ClientStatusParameters parameters, CancellationToken cancellationToken = default)
    {
        ThrowIfNoAccess(parameters.ClientName);
        var client = await _settingClientRepository.GetClient(parameters.ClientName, parameters.Instance)
                     ?? throw new KeyNotFoundException($"Client '{parameters.ClientName}' was not found.");

        // Snapshot before any further session activity; GetClientReadOnly evicts and can break collection access.
        var allSettings = client.Settings.ToList();
        var secretCount = allSettings.Count(s => s.IsSecret);

        var groups = allSettings
            .GroupBy(s => string.IsNullOrWhiteSpace(s.CategoryName) ? "General" : s.CategoryName!)
            .OrderBy(g => g.Key)
            .Select(g => new ClientStatusGroup
            {
                Category = g.Key,
                Settings = g.OrderBy(s => s.Name).Select(ToRow).ToList()
            })
            .ToList();

        return new ClientStatusReportModel
        {
            ClientDisplay = ReportValueFormatter.FormatClientDisplay(client.Name, client.Instance),
            DescriptionHtml = ReportMarkdown.ToHtml(client.Description),
            Summary =
            [
                new SummaryCardItem("Settings", allSettings.Count.ToString()),
                new SummaryCardItem("Secret Settings", secretCount.ToString()),
                new SummaryCardItem("Categories", groups.Count.ToString())
            ],
            Groups = groups
        };
    }

    private static ClientStatusSettingRow ToRow(SettingBusinessEntity setting)
        => new()
        {
            Name = setting.Name,
            DescriptionHtml = ReportMarkdown.ToHtml(setting.Description),
            ValueHtml = FormatValueHtml(setting),
            ValueType = ReportValueFormatter.FormatFriendlyType(setting.ValueType)
        };

    private static string FormatValueHtml(SettingBusinessEntity setting)
        => FormatValueHtmlCore(setting);

    internal static string FormatValueHtmlCore(SettingBusinessEntity setting)
    {
        if (setting.IsSecret)
            return ReportDataGridHtml.SecretMask;

        if (setting.Value is DataGridSettingBusinessEntity ||
            setting.ValueType.FigPropertyType() == FigPropertyType.DataGrid)
            return ReportDataGridHtml.Build(setting);

        return ReportValueFormatter.FormatValueAsHtml(setting.Value);
    }
}
