using Fig.Api.Datalayer.Repositories;
using Fig.Api.Reports.Rendering.Components;
using Fig.Api.Reports.Rendering.Views;
using Fig.Common.Constants;
using Fig.Datalayer.BusinessEntities;
using Microsoft.Extensions.Options;

namespace Fig.Api.Reports.Implementations;

public class AccessPrivilegeParameters
{
    [ReportParameter("From")]
    public DateTime From { get; set; }

    [ReportParameter("To")]
    public DateTime To { get; set; }
}

public class AccessPrivilegeReportModel
{
    public IReadOnlyList<SummaryCardItem> Summary { get; set; } = [];
    public IReadOnlyList<AccessPrivilegeRow> Rows { get; set; } = [];
}

public class AccessPrivilegeRow
{
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string ClientFilter { get; set; } = string.Empty;
    public string Classifications { get; set; } = string.Empty;
    public string PasswordChangeRequired { get; set; } = string.Empty;
    public int LoginCount { get; set; }
    public int FailCount { get; set; }
    public DateTime? LastLogin { get; set; }
}

public class AccessPrivilegeReport : ReportBase<AccessPrivilegeParameters, AccessPrivilegeReportModel>
{
    private const string KeycloakNotApplicable = "N/A (Keycloak)";

    private static readonly string[] LoginEventTypes =
    [
        EventMessage.Login,
        EventMessage.LoginFailed
    ];

    private readonly IUserRepository _userRepository;
    private readonly IEventLogRepository _eventLogRepository;
    private readonly AuthMode _authMode;

    public AccessPrivilegeReport(
        IUserRepository userRepository,
        IEventLogRepository eventLogRepository,
        IOptions<ApiSettings> apiSettings)
    {
        _userRepository = userRepository;
        _eventLogRepository = eventLogRepository;
        _authMode = apiSettings.Value.Authentication.Mode;
    }

    public override string Id => "access-privilege";
    public override string Name => "Access & Privilege Report";
    public override string Category => "Security";
    public override string Description =>
        "Lists users with roles, client filters, classifications, and login activity over a date range.";
    public override Type BodyComponentType => typeof(AccessPrivilegeReportView);
    public override ReportPageOrientation PageOrientation => ReportPageOrientation.Landscape;

    public override async Task<object> ExecuteAsync(AccessPrivilegeParameters parameters, CancellationToken cancellationToken = default)
    {
        var (from, to) = ReportDateRange.Validate(parameters.From, parameters.To);
        var loginEvents = await _eventLogRepository.GetEventsByTypes(from, to, LoginEventTypes, RequireAuthenticatedUser());

        List<AccessPrivilegeRow> rows;
        if (_authMode == AuthMode.Keycloak)
        {
            rows = loginEvents
                .Where(e => !string.IsNullOrWhiteSpace(e.AuthenticatedUser))
                .GroupBy(e => e.AuthenticatedUser!, StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var logins = g.Where(e => e.EventType == EventMessage.Login).ToList();
                    var fails = g.Where(e => e.EventType == EventMessage.LoginFailed).ToList();
                    return new AccessPrivilegeRow
                    {
                        Username = g.Key,
                        Role = KeycloakNotApplicable,
                        ClientFilter = KeycloakNotApplicable,
                        Classifications = KeycloakNotApplicable,
                        PasswordChangeRequired = KeycloakNotApplicable,
                        LoginCount = logins.Count,
                        FailCount = fails.Count,
                        LastLogin = logins.Count == 0 ? null : logins.Max(l => l.Timestamp)
                    };
                })
                .ToList();
        }
        else
        {
            var users = (await _userRepository.GetAllUsers())
                .OrderBy(u => u.Username, StringComparer.OrdinalIgnoreCase)
                .ToList();

            rows = users.Select(user =>
            {
                var userEvents = loginEvents
                    .Where(e => string.Equals(e.AuthenticatedUser, user.Username, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                var logins = userEvents.Where(e => e.EventType == EventMessage.Login).ToList();
                var fails = userEvents.Where(e => e.EventType == EventMessage.LoginFailed).ToList();

                return new AccessPrivilegeRow
                {
                    Username = user.Username,
                    Role = user.Role.ToString(),
                    ClientFilter = user.ClientFilter,
                    Classifications = FormatClassifications(user),
                    PasswordChangeRequired = user.PasswordChangeRequired ? "Yes" : "No",
                    LoginCount = logins.Count,
                    FailCount = fails.Count,
                    LastLogin = logins.Count == 0 ? null : logins.Max(l => l.Timestamp)
                };
            }).ToList();
        }

        return new AccessPrivilegeReportModel
        {
            Summary =
            [
                new SummaryCardItem("Users", rows.Count.ToString()),
                new SummaryCardItem(
                    "Password Change Required",
                    _authMode == AuthMode.Keycloak
                        ? KeycloakNotApplicable
                        : rows.Count(r => r.PasswordChangeRequired == "Yes").ToString()),
                new SummaryCardItem("Logins In Range", rows.Sum(r => r.LoginCount).ToString()),
                new SummaryCardItem("Failed Logins In Range", rows.Sum(r => r.FailCount).ToString())
            ],
            Rows = rows
        };
    }

    private static string FormatClassifications(UserBusinessEntity user)
    {
        var classifications = user.AllowedClassifications;
        if (classifications is null || classifications.Count == 0)
            return string.Empty;

        return string.Join(", ", classifications.OrderBy(c => c.ToString()).Select(c => c.ToString()));
    }
}
