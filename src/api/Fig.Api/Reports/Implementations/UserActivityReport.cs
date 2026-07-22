using Fig.Api.Datalayer.Repositories;
using Fig.Api.Reports.Rendering.Components;
using Fig.Api.Reports.Rendering.Views;
using Fig.Common.Constants;
using Fig.Contracts.Reports;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Reports.Implementations;

public class UserActivityParameters
{
    [ReportParameter("User", LookupKind = ReportParameterLookupKind.Users)]
    public string Username { get; set; } = string.Empty;

    [ReportParameter("From")]
    public DateTime From { get; set; }

    [ReportParameter("To")]
    public DateTime To { get; set; }
}

public class UserActivityReportModel
{
    public string Username { get; set; } = string.Empty;
    public IReadOnlyList<SummaryCardItem> Summary { get; set; } = [];
    public IReadOnlyList<ChartSlice> ActionBreakdown { get; set; } = [];
    public IReadOnlyList<TimelineItem> Timeline { get; set; } = [];
    public IReadOnlyList<UserActivityRow> Rows { get; set; } = [];
}

public class UserActivityRow
{
    public DateTime Timestamp { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? ClientName { get; set; }
    public string? SettingName { get; set; }
    public string? Message { get; set; }
    public string? Details { get; set; }
}

public class UserActivityReport : ReportBase<UserActivityParameters, UserActivityReportModel>
{
    private readonly IEventLogRepository _eventLogRepository;

    public UserActivityReport(IEventLogRepository eventLogRepository)
    {
        _eventLogRepository = eventLogRepository;
    }

    public override string Id => "user-activity";
    public override string Name => "User Activity Report";
    public override string Category => "Users";
    public override string Description =>
        "Displays logins, setting changes, restarts, and other audited actions performed by a user over a date range.";
    public override Type BodyComponentType => typeof(UserActivityReportView);

    public override async Task<object> ExecuteAsync(UserActivityParameters parameters, CancellationToken cancellationToken = default)
    {
        if (parameters.From > parameters.To)
            throw new ReportParameterValidationException("From must be before To.");

        var from = EnsureUtc(parameters.From);
        var to = EnsureUtc(parameters.To);
        var userLogs = (await _eventLogRepository.GetLogsForAuthenticatedUser(from, to, parameters.Username))
            .OrderByDescending(l => l.Timestamp)
            .ToList();

        var loginCount = userLogs.Count(l => l.EventType == EventMessage.Login);
        var failedLoginCount = userLogs.Count(l => l.EventType == EventMessage.LoginFailed);
        var settingChangeCount = userLogs.Count(l =>
            l.EventType == EventMessage.SettingValueUpdated ||
            l.EventType == EventMessage.ExternallyManagedSettingUpdatedByUser);
        var restartCount = userLogs.Count(l => l.EventType == EventMessage.RestartRequested);
        var otherCount = userLogs.Count - loginCount - failedLoginCount - settingChangeCount - restartCount;

        var breakdown = userLogs
            .GroupBy(l => l.EventType)
            .OrderByDescending(g => g.Count())
            .Select(g => new ChartSlice(g.Key, g.Count()))
            .ToList();

        return new UserActivityReportModel
        {
            Username = parameters.Username,
            Summary =
            [
                new SummaryCardItem("Total Events", userLogs.Count.ToString()),
                new SummaryCardItem("Logins", loginCount.ToString()),
                new SummaryCardItem("Failed Logins", failedLoginCount.ToString()),
                new SummaryCardItem("Setting Changes", settingChangeCount.ToString()),
                new SummaryCardItem("Restarts", restartCount.ToString()),
                new SummaryCardItem("Other Actions", Math.Max(0, otherCount).ToString())
            ],
            ActionBreakdown = breakdown,
            Timeline = userLogs.Take(50).Select(ToTimeline).ToList(),
            Rows = userLogs.Select(ToRow).ToList()
        };
    }

    private static TimelineItem ToTimeline(EventLogBusinessEntity log)
        => new(log.Timestamp, log.EventType, BuildTimelineDetail(log));

    private static UserActivityRow ToRow(EventLogBusinessEntity log)
        => new()
        {
            Timestamp = log.Timestamp,
            EventType = log.EventType,
            ClientName = string.IsNullOrWhiteSpace(log.ClientName)
                ? null
                : ReportValueFormatter.FormatClientDisplay(log.ClientName, log.Instance),
            SettingName = log.SettingName,
            Message = log.Message,
            Details = BuildTableDetail(log)
        };

    private static string? BuildTimelineDetail(EventLogBusinessEntity log)
    {
        if (!string.IsNullOrWhiteSpace(log.Message))
            return log.Message;
        if (!string.IsNullOrWhiteSpace(log.OriginalValue) || !string.IsNullOrWhiteSpace(log.NewValue))
            return "Value changed";
        if (!string.IsNullOrWhiteSpace(log.ClientName))
            return ReportValueFormatter.FormatClientDisplay(log.ClientName, log.Instance);
        return null;
    }

    private static string? BuildTableDetail(EventLogBusinessEntity log)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(log.Message))
            parts.Add(log.Message);
        if (!string.IsNullOrWhiteSpace(log.OriginalValue) || !string.IsNullOrWhiteSpace(log.NewValue))
            parts.Add($"{log.OriginalValue} → {log.NewValue}");
        return parts.Count == 0 ? null : string.Join(" · ", parts);
    }

    private static DateTime EnsureUtc(DateTime value)
        => value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value.ToUniversalTime(), DateTimeKind.Utc);
}
