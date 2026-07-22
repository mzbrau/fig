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

public class ClientHistoryParameters
{
    [ReportParameter("Client", LookupKind = ReportParameterLookupKind.Clients)]
    public string ClientName { get; set; } = string.Empty;

    [ReportParameter("Instance")]
    public string? Instance { get; set; }

    [ReportParameter("From")]
    public DateTime From { get; set; }

    [ReportParameter("To")]
    public DateTime To { get; set; }
}

public class ClientHistoryReportModel
{
    public string ClientDisplay { get; set; } = string.Empty;
    public IReadOnlyList<SummaryCardItem> Summary { get; set; } = [];
    public IReadOnlyList<TimelineItem> Timeline { get; set; } = [];
    public IReadOnlyList<ClientHistoryRow> Rows { get; set; } = [];
    public IReadOnlyList<ClientRegistrationHistoryRow> Registrations { get; set; } = [];
}

public class ClientHistoryRow
{
    public DateTime Timestamp { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? SettingName { get; set; }
    public string? AuthenticatedUser { get; set; }
    public string DetailsHtml { get; set; } = string.Empty;
}

public class ClientRegistrationHistoryRow
{
    public DateTime RegistrationDateUtc { get; set; }
    public string ClientVersion { get; set; } = string.Empty;
}

public class ClientHistoryReport : ReportBase<ClientHistoryParameters, ClientHistoryReportModel>
{
    private static readonly string[] ClientHistoryEventTypes =
    [
        EventMessage.SettingValueUpdated,
        EventMessage.InitialRegistration,
        EventMessage.RegistrationNoChange,
        EventMessage.RegistrationWithChange,
        EventMessage.ClientDeleted,
        EventMessage.ClientInstanceCreated,
        EventMessage.ExternallyManagedSettingUpdatedByUser,
        EventMessage.ClientSecretChanged,
        EventMessage.LiveReloadChanged,
        EventMessage.RestartRequested,
        EventMessage.HealthStatusChanged,
        EventMessage.NewSession,
        EventMessage.ExpiredSession,
        EventMessage.ClientDescriptionUpdated
    ];

    private readonly IEventLogRepository _eventLogRepository;
    private readonly IClientRegistrationHistoryRepository _registrationHistoryRepository;
    private readonly ISettingClientRepository _settingClientRepository;

    public ClientHistoryReport(
        IEventLogRepository eventLogRepository,
        IClientRegistrationHistoryRepository registrationHistoryRepository,
        ISettingClientRepository settingClientRepository)
    {
        _eventLogRepository = eventLogRepository;
        _registrationHistoryRepository = registrationHistoryRepository;
        _settingClientRepository = settingClientRepository;
    }

    public override string Id => "client-history";
    public override string Name => "Client History Report";
    public override string Category => "Clients";
    public override string Description => "Displays registrations, configuration changes, and events for a client.";
    public override Type BodyComponentType => typeof(ClientHistoryReportView);

    public override async Task<object> ExecuteAsync(ClientHistoryParameters parameters, CancellationToken cancellationToken = default)
    {
        if (parameters.From > parameters.To)
            throw new ReportParameterValidationException("From must be before To.");

        var from = EnsureUtc(parameters.From);
        var to = EnsureUtc(parameters.To);
        var instance = string.IsNullOrWhiteSpace(parameters.Instance) ? null : parameters.Instance;

        ThrowIfNoAccess(parameters.ClientName);
        var events = await _eventLogRepository.GetClientEvents(from, to, parameters.ClientName, instance, ClientHistoryEventTypes);
        var registrations = (await _registrationHistoryRepository.GetAll())
            .Where(r => string.Equals(r.ClientName, parameters.ClientName, StringComparison.OrdinalIgnoreCase))
            .Where(r => r.RegistrationDateUtc >= from && r.RegistrationDateUtc <= to)
            .OrderByDescending(r => r.RegistrationDateUtc)
            .ToList();

        var dataGridDefinitions = await LoadDataGridDefinitions(parameters.ClientName, instance);
        var rows = events.OrderByDescending(e => e.Timestamp)
            .Select(e => ToRow(e, dataGridDefinitions))
            .ToList();

        return new ClientHistoryReportModel
        {
            ClientDisplay = ReportValueFormatter.FormatClientDisplay(parameters.ClientName, instance),
            Summary =
            [
                new SummaryCardItem("Events", rows.Count.ToString()),
                new SummaryCardItem("Registrations", registrations.Count.ToString()),
                new SummaryCardItem("Setting Changes",
                    rows.Count(r => r.EventType == EventMessage.SettingValueUpdated).ToString())
            ],
            Timeline = events.OrderByDescending(e => e.Timestamp).Take(50)
                .Select(e => new TimelineItem(e.Timestamp, e.EventType, BuildTimelineDetail(e))).ToList(),
            Rows = rows,
            Registrations = registrations.Select(r => new ClientRegistrationHistoryRow
            {
                RegistrationDateUtc = r.RegistrationDateUtc,
                ClientVersion = r.ClientVersion
            }).ToList()
        };
    }

    private async Task<IReadOnlyDictionary<string, DataGridDefinitionDataContract>> LoadDataGridDefinitions(
        string clientName,
        string? instance)
    {
        var client = await _settingClientRepository.GetClient(clientName, instance);
        if (client is null)
            return new Dictionary<string, DataGridDefinitionDataContract>(StringComparer.OrdinalIgnoreCase);

        return client.Settings
            .Where(IsDataGridSetting)
            .Select(s => (Name: s.Name, Definition: s.GetDataGridDefinition()))
            .Where(x => x.Definition is not null)
            .ToDictionary(x => x.Name, x => x.Definition!, StringComparer.OrdinalIgnoreCase);
    }

    private static ClientHistoryRow ToRow(
        EventLogBusinessEntity log,
        IReadOnlyDictionary<string, DataGridDefinitionDataContract> dataGridDefinitions)
        => new()
        {
            Timestamp = log.Timestamp,
            EventType = log.EventType,
            SettingName = log.SettingName,
            AuthenticatedUser = log.AuthenticatedUser,
            DetailsHtml = BuildTableDetailHtml(log, dataGridDefinitions)
        };

    private static string? BuildTimelineDetail(EventLogBusinessEntity log)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(log.Message))
            parts.Add(log.Message);
        else if (!string.IsNullOrWhiteSpace(log.OriginalValue) || !string.IsNullOrWhiteSpace(log.NewValue))
            parts.Add("Value changed");
        if (!string.IsNullOrWhiteSpace(log.Hostname))
            parts.Add(log.Hostname);
        return parts.Count == 0 ? null : string.Join(" · ", parts);
    }

    private static string BuildTableDetailHtml(
        EventLogBusinessEntity log,
        IReadOnlyDictionary<string, DataGridDefinitionDataContract> dataGridDefinitions)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(log.Message))
            parts.Add(WebUtility.HtmlEncode(log.Message));

        if (!string.IsNullOrWhiteSpace(log.OriginalValue) || !string.IsNullOrWhiteSpace(log.NewValue))
        {
            DataGridDefinitionDataContract? definition = null;
            if (!string.IsNullOrWhiteSpace(log.SettingName))
                dataGridDefinitions.TryGetValue(log.SettingName, out definition);

            if (definition is not null)
            {
                var originalHtml = ReportDataGridHtml.BuildFromEventValue(log.OriginalValue, definition);
                var newHtml = ReportDataGridHtml.BuildFromEventValue(log.NewValue, definition);
                parts.Add(
                    $"<div class=\"report-value-change\">{originalHtml}<div class=\"muted\">→</div>{newHtml}</div>");
            }
            else
            {
                var original = WebUtility.HtmlEncode(log.OriginalValue ?? string.Empty);
                var updated = WebUtility.HtmlEncode(log.NewValue ?? string.Empty);
                parts.Add($"{original} → {updated}");
            }
        }

        if (!string.IsNullOrWhiteSpace(log.Hostname))
            parts.Add(WebUtility.HtmlEncode(log.Hostname));

        return string.Join(" · ", parts);
    }

    private static bool IsDataGridSetting(SettingBusinessEntity setting)
        => setting.Value is DataGridSettingBusinessEntity ||
           setting.ValueType.FigPropertyType() == FigPropertyType.DataGrid;

    private static DateTime EnsureUtc(DateTime value)
        => value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value.ToUniversalTime(), DateTimeKind.Utc);
}
