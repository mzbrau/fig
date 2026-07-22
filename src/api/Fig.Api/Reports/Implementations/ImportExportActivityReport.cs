using Fig.Api.Datalayer.Repositories;
using Fig.Api.Reports.Rendering.Components;
using Fig.Api.Reports.Rendering.Views;
using Fig.Common.Constants;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Reports.Implementations;

public class ImportExportActivityParameters
{
    [ReportParameter("From")]
    public DateTime From { get; set; }

    [ReportParameter("To")]
    public DateTime To { get; set; }
}

public class ImportExportActivityReportModel
{
    public IReadOnlyList<SummaryCardItem> Summary { get; set; } = [];
    public IReadOnlyList<ChartSlice> SuccessFailBreakdown { get; set; } = [];
    public IReadOnlyList<ImportExportEventRow> Rows { get; set; } = [];
}

public class ImportExportEventRow
{
    public DateTime Timestamp { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? User { get; set; }
    public string? ClientDisplay { get; set; }
    public string? Message { get; set; }
}

public class ImportExportActivityReport : ReportBase<ImportExportActivityParameters, ImportExportActivityReportModel>
{
    private static readonly string[] ImportExportEventTypes =
    [
        EventMessage.DataExported,
        EventMessage.DataImported,
        EventMessage.DataImportFailed,
        EventMessage.DataImportStarted,
        EventMessage.ClientImported,
        EventMessage.DeferredImportRegistered,
        EventMessage.DeferredImportApplied
    ];

    private readonly IEventLogRepository _eventLogRepository;

    public ImportExportActivityReport(IEventLogRepository eventLogRepository)
    {
        _eventLogRepository = eventLogRepository;
    }

    public override string Id => "import-export-activity";
    public override string Name => "Import / Export Activity Report";
    public override string Category => "Operations";
    public override string Description =>
        "Tracks data import and export activity, including failures, client imports, and deferred import lifecycle events.";
    public override Type BodyComponentType => typeof(ImportExportActivityReportView);

    public override async Task<object> ExecuteAsync(ImportExportActivityParameters parameters, CancellationToken cancellationToken = default)
    {
        var (from, to) = ReportDateRange.Validate(parameters.From, parameters.To);
        var events = (await _eventLogRepository.GetEventsByTypes(from, to, ImportExportEventTypes, RequireAuthenticatedUser()))
            .OrderByDescending(e => e.Timestamp)
            .ToList();

        var exported = EventAnalytics.CountOfType(events, EventMessage.DataExported);
        var imported = EventAnalytics.CountOfType(events, EventMessage.DataImported);
        var failed = EventAnalytics.CountOfType(events, EventMessage.DataImportFailed);
        var started = EventAnalytics.CountOfType(events, EventMessage.DataImportStarted);
        var clientImported = EventAnalytics.CountOfType(events, EventMessage.ClientImported);
        var deferredRegistered = EventAnalytics.CountOfType(events, EventMessage.DeferredImportRegistered);
        var deferredApplied = EventAnalytics.CountOfType(events, EventMessage.DeferredImportApplied);

        var successCount = imported + clientImported + deferredApplied + exported;
        var failCount = failed;

        return new ImportExportActivityReportModel
        {
            Summary =
            [
                new SummaryCardItem("Total Events", events.Count.ToString()),
                new SummaryCardItem("Exports", exported.ToString()),
                new SummaryCardItem("Imports", imported.ToString()),
                new SummaryCardItem("Import Failures", failed.ToString()),
                new SummaryCardItem("Import Started", started.ToString()),
                new SummaryCardItem("Client Imports", clientImported.ToString()),
                new SummaryCardItem("Deferred Registered", deferredRegistered.ToString()),
                new SummaryCardItem("Deferred Applied", deferredApplied.ToString())
            ],
            SuccessFailBreakdown =
            [
                new ChartSlice("Success", successCount),
                new ChartSlice("Failed", failCount)
            ],
            Rows = events.Select(ToRow).ToList()
        };
    }

    private static ImportExportEventRow ToRow(EventLogBusinessEntity log)
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
