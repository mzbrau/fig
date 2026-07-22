using Fig.Api.Datalayer.Repositories;
using Fig.Api.Reports.Rendering.Components;
using Fig.Api.Reports.Rendering.Views;
using Fig.Client.Abstractions.Data;
using Fig.Contracts.Reports;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Reports.Implementations;

public class ConfigurationInventoryParameters
{
    [ReportParameter("Client", LookupKind = ReportParameterLookupKind.Clients)]
    public string? ClientName { get; set; }

    [ReportParameter("Instance")]
    public string? Instance { get; set; }

    [ReportParameter("Secrets Only")]
    public bool SecretsOnly { get; set; }
}

public class ConfigurationInventoryReportModel
{
    public string ScopeDisplay { get; set; } = string.Empty;
    public IReadOnlyList<SummaryCardItem> Summary { get; set; } = [];
    public IReadOnlyList<ConfigurationInventoryRow> Rows { get; set; } = [];
}

public class ConfigurationInventoryRow
{
    public string ClientDisplay { get; set; } = string.Empty;
    public string SettingName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Classification { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public string ExternallyManaged { get; set; } = string.Empty;
    public string EnvironmentSpecific { get; set; } = string.Empty;
    public string InitOnlyExport { get; set; } = string.Empty;
    public string SupportsLiveUpdate { get; set; } = string.Empty;
    public DateTime? LastChanged { get; set; }
}

public class ConfigurationInventoryReport : ReportBase<ConfigurationInventoryParameters, ConfigurationInventoryReportModel>
{
    private readonly ISettingClientRepository _settingClientRepository;

    public ConfigurationInventoryReport(ISettingClientRepository settingClientRepository)
    {
        _settingClientRepository = settingClientRepository;
    }

    public override string Id => "configuration-inventory";
    public override string Name => "Configuration Inventory Report";
    public override string Category => "Compliance";
    public override string Description =>
        "Inventories settings across clients with compliance flags (secret, classification, externally managed) without exposing secret values.";
    public override Type BodyComponentType => typeof(ConfigurationInventoryReportView);
    public override ReportPageOrientation PageOrientation => ReportPageOrientation.Landscape;

    public override async Task<object> ExecuteAsync(ConfigurationInventoryParameters parameters, CancellationToken cancellationToken = default)
    {
        var clientName = ReportDateRange.NormalizeOptionalClient(parameters.ClientName);
        var instance = string.IsNullOrWhiteSpace(parameters.Instance) ? null : parameters.Instance.Trim();

        IList<SettingClientBusinessEntity> clients;
        if (clientName is not null)
        {
            ThrowIfNoAccess(clientName);
            var client = await _settingClientRepository.GetClient(clientName, instance)
                         ?? throw new KeyNotFoundException($"Client '{clientName}' was not found.");
            clients = [client];
        }
        else
        {
            clients = await _settingClientRepository.GetAllClients(RequireAuthenticatedUser());
        }

        var inventory = SettingInventoryProjector.ProjectAll(clients, parameters.SecretsOnly);
        var classifiedCount = inventory.Count(r =>
            !string.Equals(r.Classification, nameof(Classification.Technical), StringComparison.OrdinalIgnoreCase));

        return new ConfigurationInventoryReportModel
        {
            ScopeDisplay = clientName is null
                ? "All clients"
                : ReportValueFormatter.FormatClientDisplay(clientName, instance),
            Summary =
            [
                new SummaryCardItem("Total Settings", inventory.Count.ToString()),
                new SummaryCardItem("Secrets", inventory.Count(r => r.IsSecret).ToString()),
                new SummaryCardItem("Externally Managed", inventory.Count(r => r.IsExternallyManaged).ToString()),
                new SummaryCardItem("Classified", classifiedCount.ToString(), "Non-Technical")
            ],
            Rows = inventory.Select(ToRow).ToList()
        };
    }

    private static ConfigurationInventoryRow ToRow(SettingInventoryRow row)
        => new()
        {
            ClientDisplay = row.ClientDisplay,
            SettingName = row.SettingName,
            Category = row.Category,
            Classification = row.Classification,
            Secret = Flag(row.IsSecret),
            ExternallyManaged = Flag(row.IsExternallyManaged),
            EnvironmentSpecific = Flag(row.EnvironmentSpecific),
            InitOnlyExport = Flag(row.InitOnlyExport),
            SupportsLiveUpdate = Flag(row.SupportsLiveUpdate),
            LastChanged = row.LastChanged
        };

    private static string Flag(bool value) => value ? "Yes" : "No";
}
