using Fig.Api.Datalayer.Repositories;
using Fig.Api.Reports.Rendering.Components;
using Fig.Api.Reports.Rendering.Views;
using Fig.Common.Constants;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Reports.Implementations;

public class SecurityAuditParameters
{
    [ReportParameter("From")]
    public DateTime From { get; set; }

    [ReportParameter("To")]
    public DateTime To { get; set; }
}

public class SecurityAuditReportModel
{
    public IReadOnlyList<SummaryCardItem> Summary { get; set; } = [];
    public IReadOnlyList<ChartSlice> Breakdown { get; set; } = [];
    public IReadOnlyList<SecurityEventRow> FailedLogins { get; set; } = [];
    public IReadOnlyList<SecurityEventRow> InvalidSecrets { get; set; } = [];
    public IReadOnlyList<SecurityEventRow> UserLifecycle { get; set; } = [];
    public IReadOnlyList<SecurityEventRow> ConfigChanges { get; set; } = [];
}

public class SecurityEventRow
{
    public DateTime Timestamp { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? User { get; set; }
    public string? ClientName { get; set; }
    public string? Message { get; set; }
    public string? Details { get; set; }
}

public class SecurityAuditReport : ReportBase<SecurityAuditParameters, SecurityAuditReportModel>
{
    private static readonly string[] SecurityEventTypes =
    [
        EventMessage.LoginFailed,
        EventMessage.InvalidClientSecretAttempt,
        EventMessage.ClientSecretChanged,
        EventMessage.PasswordUpdated,
        EventMessage.UserCreated,
        EventMessage.UserUpdated,
        EventMessage.UserDeleted,
        EventMessage.ConfigurationChanged
    ];

    private static readonly HashSet<string> UserLifecycleTypes = new(StringComparer.Ordinal)
    {
        EventMessage.PasswordUpdated,
        EventMessage.UserCreated,
        EventMessage.UserUpdated,
        EventMessage.UserDeleted
    };

    private readonly IEventLogRepository _eventLogRepository;

    public SecurityAuditReport(IEventLogRepository eventLogRepository)
    {
        _eventLogRepository = eventLogRepository;
    }

    public override string Id => "security-audit";
    public override string Name => "Security Audit Report";
    public override string Category => "Security";
    public override string Description =>
        "Summarizes security-relevant activity including failed logins, invalid client secrets, user lifecycle changes, and Fig configuration updates.";
    public override Type BodyComponentType => typeof(SecurityAuditReportView);

    public override async Task<object> ExecuteAsync(SecurityAuditParameters parameters, CancellationToken cancellationToken = default)
    {
        var (from, to) = ReportDateRange.Validate(parameters.From, parameters.To);
        var events = (await _eventLogRepository.GetEventsByTypes(from, to, SecurityEventTypes, RequireAuthenticatedUser()))
            .OrderByDescending(e => e.Timestamp)
            .ToList();

        var failedLogins = events.Where(e => e.EventType == EventMessage.LoginFailed).Select(ToRow).ToList();
        var invalidSecrets = events.Where(e => e.EventType == EventMessage.InvalidClientSecretAttempt).Select(ToRow).ToList();
        var userLifecycle = events.Where(e => UserLifecycleTypes.Contains(e.EventType)).Select(ToRow).ToList();
        var configChanges = events.Where(e => e.EventType == EventMessage.ConfigurationChanged).Select(ToRow).ToList();
        var secretChanges = EventAnalytics.CountOfType(events, EventMessage.ClientSecretChanged);

        return new SecurityAuditReportModel
        {
            Summary =
            [
                new SummaryCardItem("Total Security Events", events.Count.ToString()),
                new SummaryCardItem("Failed Logins", failedLogins.Count.ToString()),
                new SummaryCardItem("Invalid Client Secrets", invalidSecrets.Count.ToString()),
                new SummaryCardItem("Client Secret Changes", secretChanges.ToString()),
                new SummaryCardItem("User Lifecycle", userLifecycle.Count.ToString()),
                new SummaryCardItem("Configuration Changes", configChanges.Count.ToString())
            ],
            Breakdown = EventAnalytics.CountByEventType(events),
            FailedLogins = failedLogins,
            InvalidSecrets = invalidSecrets,
            UserLifecycle = userLifecycle,
            ConfigChanges = configChanges
        };
    }

    private static SecurityEventRow ToRow(EventLogBusinessEntity log)
        => new()
        {
            Timestamp = log.Timestamp,
            EventType = log.EventType,
            User = log.AuthenticatedUser,
            ClientName = string.IsNullOrWhiteSpace(log.ClientName)
                ? null
                : ReportValueFormatter.FormatClientDisplay(log.ClientName, log.Instance),
            Message = log.Message,
            Details = BuildDetails(log)
        };

    private static string? BuildDetails(EventLogBusinessEntity log)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(log.Message))
            parts.Add(log.Message!);
        if (!string.IsNullOrWhiteSpace(log.OriginalValue) || !string.IsNullOrWhiteSpace(log.NewValue))
            parts.Add($"{log.OriginalValue} → {log.NewValue}");
        return parts.Count == 0 ? null : string.Join(" · ", parts);
    }
}
