using Fig.Api.Datalayer.Repositories;
using Fig.Api.Reports.Rendering.Components;
using Fig.Api.Reports.Rendering.Views;
using Fig.Common.Constants;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Reports.Implementations;

public class TimeMachineActivityParameters
{
    [ReportParameter("From")]
    public DateTime From { get; set; }

    [ReportParameter("To")]
    public DateTime To { get; set; }
}

public class TimeMachineActivityReportModel
{
    public IReadOnlyList<SummaryCardItem> Summary { get; set; } = [];
    public IReadOnlyList<CheckPointMetadataRow> CheckPoints { get; set; } = [];
    public IReadOnlyList<TimeMachineEventRow> CreatedEvents { get; set; } = [];
    public IReadOnlyList<TimeMachineEventRow> AppliedEvents { get; set; } = [];
    public IReadOnlyList<TimeMachineEventRow> NoteEvents { get; set; } = [];
}

public class CheckPointMetadataRow
{
    public DateTime Timestamp { get; set; }
    public string? User { get; set; }
    public string AfterEvent { get; set; } = string.Empty;
    public int NumberOfClients { get; set; }
    public int NumberOfSettings { get; set; }
    public string? Note { get; set; }
}

public class TimeMachineEventRow
{
    public DateTime Timestamp { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? User { get; set; }
    public string? Message { get; set; }
}

public class TimeMachineActivityReport : ReportBase<TimeMachineActivityParameters, TimeMachineActivityReportModel>
{
    private static readonly string[] TimeMachineEventTypes =
    [
        EventMessage.CheckPointCreated,
        EventMessage.CheckPointApplied,
        EventMessage.NoteAddedToCheckPoint
    ];

    private readonly IEventLogRepository _eventLogRepository;
    private readonly ICheckPointRepository _checkPointRepository;

    public TimeMachineActivityReport(
        IEventLogRepository eventLogRepository,
        ICheckPointRepository checkPointRepository)
    {
        _eventLogRepository = eventLogRepository;
        _checkPointRepository = checkPointRepository;
    }

    public override string Id => "time-machine-activity";
    public override string Name => "Time Machine Activity Report";
    public override string Category => "Operations";
    public override string Description =>
        "Summarizes Time Machine checkpoint creation, application, and notes over a date range.";
    public override Type BodyComponentType => typeof(TimeMachineActivityReportView);

    public override async Task<object> ExecuteAsync(TimeMachineActivityParameters parameters, CancellationToken cancellationToken = default)
    {
        var (from, to) = ReportDateRange.Validate(parameters.From, parameters.To);

        var events = (await _eventLogRepository.GetEventsByTypes(from, to, TimeMachineEventTypes, RequireAuthenticatedUser()))
            .OrderByDescending(e => e.Timestamp)
            .ToList();

        var created = events.Where(e => e.EventType == EventMessage.CheckPointCreated).Select(ToEventRow).ToList();
        var applied = events.Where(e => e.EventType == EventMessage.CheckPointApplied).Select(ToEventRow).ToList();
        var notes = events.Where(e => e.EventType == EventMessage.NoteAddedToCheckPoint).Select(ToEventRow).ToList();

        // Metadata only — do not load checkpoint binary payloads.
        var checkPoints = (await _checkPointRepository.GetCheckPoints(from, to))
            .OrderByDescending(c => c.Timestamp)
            .Select(c => new CheckPointMetadataRow
            {
                Timestamp = c.Timestamp,
                User = c.User,
                AfterEvent = c.AfterEvent,
                NumberOfClients = c.NumberOfClients,
                NumberOfSettings = c.NumberOfSettings,
                Note = c.Note
            })
            .ToList();

        return new TimeMachineActivityReportModel
        {
            Summary =
            [
                new SummaryCardItem("Checkpoints (Metadata)", checkPoints.Count.ToString()),
                new SummaryCardItem("Created Events", created.Count.ToString()),
                new SummaryCardItem("Applied Events", applied.Count.ToString()),
                new SummaryCardItem("Notes Added", notes.Count.ToString()),
                new SummaryCardItem("Total Events", events.Count.ToString())
            ],
            CheckPoints = checkPoints,
            CreatedEvents = created,
            AppliedEvents = applied,
            NoteEvents = notes
        };
    }

    private static TimeMachineEventRow ToEventRow(EventLogBusinessEntity log)
        => new()
        {
            Timestamp = log.Timestamp,
            EventType = log.EventType,
            User = log.AuthenticatedUser,
            Message = log.Message
        };
}
