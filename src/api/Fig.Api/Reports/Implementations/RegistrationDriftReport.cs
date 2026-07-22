using Fig.Api.Datalayer.Repositories;
using Fig.Api.Reports.Rendering.Components;
using Fig.Api.Reports.Rendering.Views;
using Fig.Common.Constants;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Reports.Implementations;

public class RegistrationDriftParameters
{
    [ReportParameter("From")]
    public DateTime From { get; set; }

    [ReportParameter("To")]
    public DateTime To { get; set; }
}

public class RegistrationDriftReportModel
{
    public IReadOnlyList<SummaryCardItem> Summary { get; set; } = [];
    public IReadOnlyList<ChartSlice> EventBreakdown { get; set; } = [];
    public IReadOnlyList<RegistrationEventRow> DefinitionChanges { get; set; } = [];
    public IReadOnlyList<RegistrationEventRow> NewInstances { get; set; } = [];
    public IReadOnlyList<StaleRegistrationRow> NoRegistrationInRange { get; set; } = [];
}

public class RegistrationEventRow
{
    public DateTime Timestamp { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string ClientDisplay { get; set; } = string.Empty;
    public string? Message { get; set; }
}

public class StaleRegistrationRow
{
    public string ClientDisplay { get; set; } = string.Empty;
    public DateTime? LastRegistration { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class RegistrationDriftReport : ReportBase<RegistrationDriftParameters, RegistrationDriftReportModel>
{
    private static readonly string[] RegistrationEventTypes =
    [
        EventMessage.InitialRegistration,
        EventMessage.RegistrationWithChange,
        EventMessage.RegistrationNoChange,
        EventMessage.ClientInstanceCreated,
        EventMessage.ClientDeleted
    ];

    private readonly IEventLogRepository _eventLogRepository;
    private readonly ISettingClientRepository _settingClientRepository;

    public RegistrationDriftReport(
        IEventLogRepository eventLogRepository,
        ISettingClientRepository settingClientRepository)
    {
        _eventLogRepository = eventLogRepository;
        _settingClientRepository = settingClientRepository;
    }

    public override string Id => "registration-drift";
    public override string Name => "Registration Drift";
    public override string Category => "Operations";
    public override string Description =>
        "Tracks registration definition changes, new instances, and clients that did not register in the selected range.";
    public override Type BodyComponentType => typeof(RegistrationDriftReportView);

    public override async Task<object> ExecuteAsync(RegistrationDriftParameters parameters, CancellationToken cancellationToken = default)
    {
        var (from, to) = ReportDateRange.Validate(parameters.From, parameters.To);

        var events = (await _eventLogRepository.GetEventsByTypes(from, to, RegistrationEventTypes, RequireAuthenticatedUser()))
            .OrderByDescending(e => e.Timestamp)
            .ToList();

        var definitionChanges = events
            .Where(e => e.EventType == EventMessage.RegistrationWithChange)
            .Select(ToEventRow)
            .ToList();

        var newInstances = events
            .Where(e => e.EventType is EventMessage.ClientInstanceCreated or EventMessage.InitialRegistration)
            .Select(ToEventRow)
            .ToList();

        var clients = await _settingClientRepository.GetAllClients(RequireAuthenticatedUser());
        var noRegistrationInRange = clients
            .Where(c => c.LastRegistration is null || c.LastRegistration < from || c.LastRegistration > to)
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => c.Instance ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .Select(c => new StaleRegistrationRow
            {
                ClientDisplay = ReportValueFormatter.FormatClientDisplay(c.Name, c.Instance),
                LastRegistration = c.LastRegistration,
                Status = c.LastRegistration is null
                    ? "Never registered"
                    : c.LastRegistration < from
                        ? "Stale (before range)"
                        : "After range"
            })
            .ToList();

        return new RegistrationDriftReportModel
        {
            Summary =
            [
                new SummaryCardItem("Registration Events", events.Count.ToString()),
                new SummaryCardItem("Definition Changes", definitionChanges.Count.ToString()),
                new SummaryCardItem("New Instances / Clients", newInstances.Count.ToString()),
                new SummaryCardItem("No Registration In Range", noRegistrationInRange.Count.ToString()),
                new SummaryCardItem("No-Change Registrations",
                    EventAnalytics.CountOfType(events, EventMessage.RegistrationNoChange).ToString()),
                new SummaryCardItem("Client Deletes",
                    EventAnalytics.CountOfType(events, EventMessage.ClientDeleted).ToString())
            ],
            EventBreakdown = EventAnalytics.CountByEventType(events),
            DefinitionChanges = definitionChanges,
            NewInstances = newInstances,
            NoRegistrationInRange = noRegistrationInRange
        };
    }

    private static RegistrationEventRow ToEventRow(EventLogBusinessEntity log)
        => new()
        {
            Timestamp = log.Timestamp,
            EventType = log.EventType,
            ClientDisplay = string.IsNullOrWhiteSpace(log.ClientName)
                ? string.Empty
                : ReportValueFormatter.FormatClientDisplay(log.ClientName, log.Instance),
            Message = log.Message
        };
}
