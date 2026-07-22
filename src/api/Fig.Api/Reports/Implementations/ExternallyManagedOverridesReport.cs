using Fig.Api.Datalayer.Repositories;
using Fig.Api.Reports.Rendering.Components;
using Fig.Api.Reports.Rendering.Views;
using Fig.Common.Constants;
using Fig.Contracts.Reports;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Reports.Implementations;

public class ExternallyManagedOverridesParameters
{
    [ReportParameter("From")]
    public DateTime From { get; set; }

    [ReportParameter("To")]
    public DateTime To { get; set; }

    [ReportParameter("Client", LookupKind = ReportParameterLookupKind.Clients)]
    public string? ClientName { get; set; }

    [ReportParameter("Instance")]
    public string? Instance { get; set; }
}

public class ExternallyManagedOverridesReportModel
{
    public string ScopeDisplay { get; set; } = string.Empty;
    public IReadOnlyList<SummaryCardItem> Summary { get; set; } = [];
    public IReadOnlyList<ExternallyManagedInventoryRow> Inventory { get; set; } = [];
    public IReadOnlyList<OverrideEventRow> OverrideEvents { get; set; } = [];
    public IReadOnlyList<TopOverriddenSettingRow> TopOverriddenSettings { get; set; } = [];
}

public class ExternallyManagedInventoryRow
{
    public string ClientDisplay { get; set; } = string.Empty;
    public string SettingName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Classification { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public DateTime? LastChanged { get; set; }
}

public class OverrideEventRow
{
    public DateTime Timestamp { get; set; }
    public string ClientDisplay { get; set; } = string.Empty;
    public string? SettingName { get; set; }
    public string? User { get; set; }
    public string? Message { get; set; }
}

public class TopOverriddenSettingRow
{
    public string SettingKey { get; set; } = string.Empty;
    public int OverrideCount { get; set; }
}

public class ExternallyManagedOverridesReport : ReportBase<ExternallyManagedOverridesParameters, ExternallyManagedOverridesReportModel>
{
    private static readonly string[] OverrideEventTypes =
    [
        EventMessage.ExternallyManagedSettingUpdatedByUser
    ];

    private readonly ISettingClientRepository _settingClientRepository;
    private readonly IEventLogRepository _eventLogRepository;

    public ExternallyManagedOverridesReport(
        ISettingClientRepository settingClientRepository,
        IEventLogRepository eventLogRepository)
    {
        _settingClientRepository = settingClientRepository;
        _eventLogRepository = eventLogRepository;
    }

    public override string Id => "externally-managed-overrides";
    public override string Name => "Externally Managed Overrides Report";
    public override string Category => "Compliance";
    public override string Description =>
        "Lists externally managed settings and user override activity over a date range.";
    public override Type BodyComponentType => typeof(ExternallyManagedOverridesReportView);
    public override ReportPageOrientation PageOrientation => ReportPageOrientation.Landscape;

    public override async Task<object> ExecuteAsync(ExternallyManagedOverridesParameters parameters, CancellationToken cancellationToken = default)
    {
        var (from, to) = ReportDateRange.Validate(parameters.From, parameters.To);
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

        var inventory = SettingInventoryProjector.ProjectAll(clients)
            .Where(r => r.IsExternallyManaged)
            .Select(r => new ExternallyManagedInventoryRow
            {
                ClientDisplay = r.ClientDisplay,
                SettingName = r.SettingName,
                Category = r.Category,
                Classification = r.Classification,
                Secret = r.IsSecret ? "Yes" : "No",
                LastChanged = r.LastChanged
            })
            .ToList();

        var overrideEvents = (await _eventLogRepository.GetEventsByTypes(
                from, to, OverrideEventTypes, RequireAuthenticatedUser(), clientName, instance))
            .OrderByDescending(e => e.Timestamp)
            .ToList();

        var eventRows = overrideEvents.Select(e => new OverrideEventRow
        {
            Timestamp = e.Timestamp,
            ClientDisplay = string.IsNullOrWhiteSpace(e.ClientName)
                ? string.Empty
                : ReportValueFormatter.FormatClientDisplay(e.ClientName, e.Instance),
            SettingName = e.SettingName,
            User = e.AuthenticatedUser,
            Message = e.Message
        }).ToList();

        var topOverridden = EventAnalytics.TopBy(
                overrideEvents,
                e => string.IsNullOrWhiteSpace(e.SettingName)
                    ? null
                    : $"{ReportValueFormatter.FormatClientDisplay(e.ClientName ?? string.Empty, e.Instance)} · {e.SettingName}")
            .Select(t => new TopOverriddenSettingRow
            {
                SettingKey = t.Key,
                OverrideCount = t.Count
            })
            .ToList();

        return new ExternallyManagedOverridesReportModel
        {
            ScopeDisplay = clientName is null
                ? "All clients"
                : ReportValueFormatter.FormatClientDisplay(clientName, instance),
            Summary =
            [
                new SummaryCardItem("Externally Managed Settings", inventory.Count.ToString()),
                new SummaryCardItem("Override Events", eventRows.Count.ToString()),
                new SummaryCardItem("Distinct Settings Overridden", topOverridden.Count.ToString()),
                new SummaryCardItem("Secret Externally Managed", inventory.Count(r => r.Secret == "Yes").ToString())
            ],
            Inventory = inventory,
            OverrideEvents = eventRows,
            TopOverriddenSettings = topOverridden
        };
    }
}
