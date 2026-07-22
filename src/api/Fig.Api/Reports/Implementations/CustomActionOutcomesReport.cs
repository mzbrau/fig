using Fig.Api.Datalayer.Repositories;
using Fig.Api.Reports.Rendering.Components;
using Fig.Api.Reports.Rendering.Views;
using Fig.Contracts.Reports;

namespace Fig.Api.Reports.Implementations;

public class CustomActionOutcomesParameters
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

public class CustomActionOutcomesReportModel
{
    public string ScopeDisplay { get; set; } = string.Empty;
    public IReadOnlyList<SummaryCardItem> Summary { get; set; } = [];
    public IReadOnlyList<CustomActionFailureRow> Failures { get; set; } = [];
    public IReadOnlyList<CustomActionSlowRow> Slowest { get; set; } = [];
}

public class CustomActionFailureRow
{
    public DateTime RequestedAt { get; set; }
    public DateTime? ExecutedAt { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    public string? HandlingInstance { get; set; }
}

public class CustomActionSlowRow
{
    public DateTime RequestedAt { get; set; }
    public DateTime ExecutedAt { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
}

public class CustomActionOutcomesReport : ReportBase<CustomActionOutcomesParameters, CustomActionOutcomesReportModel>
{
    private readonly ICustomActionExecutionRepository _customActionExecutionRepository;

    public CustomActionOutcomesReport(ICustomActionExecutionRepository customActionExecutionRepository)
    {
        _customActionExecutionRepository = customActionExecutionRepository;
    }

    public override string Id => "custom-action-outcomes";
    public override string Name => "Custom Action Outcomes Report";
    public override string Category => "Integrations";
    public override string Description =>
        "Summarizes custom action executions with success rate, failures, and slowest runs over a date range.";
    public override Type BodyComponentType => typeof(CustomActionOutcomesReportView);
    public override ReportPageOrientation PageOrientation => ReportPageOrientation.Landscape;

    public override async Task<object> ExecuteAsync(CustomActionOutcomesParameters parameters, CancellationToken cancellationToken = default)
    {
        var (from, to) = ReportDateRange.Validate(parameters.From, parameters.To);
        // Instance is ignored for actions — filter by client name only.
        var clientName = ReportDateRange.NormalizeOptionalClient(parameters.ClientName);
        if (clientName is not null)
            ThrowIfNoAccess(clientName);

        var history = (await _customActionExecutionRepository.GetHistory(from, to, clientName))
            .OrderByDescending(e => e.RequestedAt)
            .ToList();

        var total = history.Count;
        var succeeded = history.Count(e => e.Succeeded);
        var failed = total - succeeded;
        var successRate = total == 0
            ? "n/a"
            : $"{succeeded * 100.0 / total:F1}%";

        var failures = history
            .Where(e => !e.Succeeded)
            .Select(e => new CustomActionFailureRow
            {
                RequestedAt = e.RequestedAt,
                ExecutedAt = e.ExecutedAt,
                ClientName = e.ClientName,
                ActionName = e.CustomActionName,
                HandlingInstance = e.HandlingInstance
            })
            .ToList();

        var slowest = history
            .Where(e => e.ExecutedAt.HasValue)
            .Select(e => new
            {
                Entity = e,
                Duration = e.ExecutedAt!.Value - e.RequestedAt
            })
            .OrderByDescending(x => x.Duration)
            .Take(20)
            .Select(x => new CustomActionSlowRow
            {
                RequestedAt = x.Entity.RequestedAt,
                ExecutedAt = x.Entity.ExecutedAt!.Value,
                ClientName = x.Entity.ClientName,
                ActionName = x.Entity.CustomActionName,
                Duration = FormatDuration(x.Duration),
                Outcome = x.Entity.Succeeded ? "Succeeded" : "Failed"
            })
            .ToList();

        return new CustomActionOutcomesReportModel
        {
            ScopeDisplay = clientName ?? "All clients",
            Summary =
            [
                new SummaryCardItem("Total Executions", total.ToString()),
                new SummaryCardItem("Succeeded", succeeded.ToString()),
                new SummaryCardItem("Failed", failed.ToString()),
                new SummaryCardItem("Success Rate", successRate)
            ],
            Failures = failures,
            Slowest = slowest
        };
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalSeconds < 1)
            return $"{duration.TotalMilliseconds:F0} ms";
        if (duration.TotalMinutes < 1)
            return $"{duration.TotalSeconds:F1} s";
        if (duration.TotalHours < 1)
            return $"{duration.TotalMinutes:F1} min";
        return $"{duration.TotalHours:F1} h";
    }
}
