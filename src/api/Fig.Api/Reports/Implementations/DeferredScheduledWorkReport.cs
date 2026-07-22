using Fig.Api.Datalayer.Repositories;
using Fig.Api.Reports.Rendering.Components;
using Fig.Api.Reports.Rendering.Views;
using Fig.Common.Constants;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Reports.Implementations;

public class DeferredScheduledWorkParameters
{
    [ReportParameter("From")]
    public DateTime From { get; set; }

    [ReportParameter("To")]
    public DateTime To { get; set; }
}

public class DeferredScheduledWorkReportModel
{
    public IReadOnlyList<SummaryCardItem> Summary { get; set; } = [];
    public IReadOnlyList<PendingDeferredChangeRow> PendingChanges { get; set; } = [];
    public IReadOnlyList<PendingDeferredChangeRow> OverdueChanges { get; set; } = [];
    public IReadOnlyList<ScheduleEventRow> ScheduleEvents { get; set; } = [];
    public IReadOnlyList<DeferredImportRow> DeferredImports { get; set; } = [];
}

public class PendingDeferredChangeRow
{
    public DateTime ExecuteAtUtc { get; set; }
    public string? RequestingUser { get; set; }
    public string ClientDisplay { get; set; } = string.Empty;
    public int SettingCount { get; set; }
    public string Overdue { get; set; } = string.Empty;
}

public class ScheduleEventRow
{
    public DateTime Timestamp { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? User { get; set; }
    public string? ClientDisplay { get; set; }
    public string? Message { get; set; }
}

public class DeferredImportRow
{
    public DateTime ImportTime { get; set; }
    public string ClientDisplay { get; set; } = string.Empty;
    public int SettingCount { get; set; }
    public string AuthenticatedUser { get; set; } = string.Empty;
}

public class DeferredScheduledWorkReport : ReportBase<DeferredScheduledWorkParameters, DeferredScheduledWorkReportModel>
{
    private static readonly string[] ScheduleEventTypes =
    [
        EventMessage.ChangesScheduled,
        EventMessage.ScheduledChangesDeleted
    ];

    private readonly IDeferredChangeRepository _deferredChangeRepository;
    private readonly IDeferredClientImportRepository _deferredClientImportRepository;
    private readonly IEventLogRepository _eventLogRepository;

    public DeferredScheduledWorkReport(
        IDeferredChangeRepository deferredChangeRepository,
        IDeferredClientImportRepository deferredClientImportRepository,
        IEventLogRepository eventLogRepository)
    {
        _deferredChangeRepository = deferredChangeRepository;
        _deferredClientImportRepository = deferredClientImportRepository;
        _eventLogRepository = eventLogRepository;
    }

    public override string Id => "deferred-scheduled-work";
    public override string Name => "Deferred / Scheduled Work Report";
    public override string Category => "Operations";
    public override string Description =>
        "Shows pending and overdue deferred setting changes, schedule-related events, and deferred client imports waiting to apply.";
    public override Type BodyComponentType => typeof(DeferredScheduledWorkReportView);
    public override ReportPageOrientation PageOrientation => ReportPageOrientation.Landscape;

    public override async Task<object> ExecuteAsync(DeferredScheduledWorkParameters parameters, CancellationToken cancellationToken = default)
    {
        var (from, to) = ReportDateRange.Validate(parameters.From, parameters.To);
        var now = DateTime.UtcNow;

        var pending = (await _deferredChangeRepository.GetAllChanges())
            .OrderBy(c => c.ExecuteAtUtc)
            .Select(c => ToPendingRow(c, now))
            .ToList();

        var overdue = pending.Where(p => p.Overdue == "Yes").ToList();

        var scheduleEvents = (await _eventLogRepository.GetEventsByTypes(from, to, ScheduleEventTypes, RequireAuthenticatedUser()))
            .OrderByDescending(e => e.Timestamp)
            .Select(ToEventRow)
            .ToList();

        var deferredImports = (await _deferredClientImportRepository.GetAllClients(RequireAuthenticatedUser()))
            .OrderByDescending(i => i.ImportTime)
            .Select(i => new DeferredImportRow
            {
                ImportTime = i.ImportTime,
                ClientDisplay = ReportValueFormatter.FormatClientDisplay(i.Name, i.Instance),
                SettingCount = i.SettingCount,
                AuthenticatedUser = i.AuthenticatedUser
            })
            .ToList();

        var scheduledCount = scheduleEvents.Count(e => e.EventType == EventMessage.ChangesScheduled);
        var deletedCount = scheduleEvents.Count(e => e.EventType == EventMessage.ScheduledChangesDeleted);

        return new DeferredScheduledWorkReportModel
        {
            Summary =
            [
                new SummaryCardItem("Pending Changes", pending.Count.ToString()),
                new SummaryCardItem("Overdue", overdue.Count.ToString()),
                new SummaryCardItem("Scheduled In Range", scheduledCount.ToString()),
                new SummaryCardItem("Deleted In Range", deletedCount.ToString()),
                new SummaryCardItem("Deferred Imports Waiting", deferredImports.Count.ToString())
            ],
            PendingChanges = pending,
            OverdueChanges = overdue,
            ScheduleEvents = scheduleEvents,
            DeferredImports = deferredImports
        };
    }

    private static PendingDeferredChangeRow ToPendingRow(DeferredChangeBusinessEntity change, DateTime nowUtc)
    {
        var settingCount = change.ChangeSet?.ValueUpdates?.Count() ?? 0;
        return new PendingDeferredChangeRow
        {
            ExecuteAtUtc = change.ExecuteAtUtc,
            RequestingUser = change.RequestingUser,
            ClientDisplay = ReportValueFormatter.FormatClientDisplay(change.ClientName, change.Instance),
            SettingCount = settingCount,
            Overdue = change.ExecuteAtUtc < nowUtc ? "Yes" : "No"
        };
    }

    private static ScheduleEventRow ToEventRow(EventLogBusinessEntity log)
        => new()
        {
            Timestamp = log.Timestamp,
            EventType = log.EventType,
            User = log.AuthenticatedUser,
            ClientDisplay = string.IsNullOrWhiteSpace(log.ClientName)
                ? null
                : ReportValueFormatter.FormatClientDisplay(log.ClientName, log.Instance),
            Message = log.Message
        };
}
