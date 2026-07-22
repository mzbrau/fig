using Fig.Api.Datalayer.Repositories;
using Fig.Api.Reports.Rendering.Components;
using Fig.Api.Reports.Rendering.Views;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Reports.Implementations;

public class FigPlatformParameters
{
}

public class FigPlatformReportModel
{
    public IReadOnlyList<SummaryCardItem> Summary { get; set; } = [];
    public IReadOnlyList<PlatformConfigFlagRow> ConfigFlags { get; set; } = [];
    public IReadOnlyList<PlatformApiNodeRow> ApiNodes { get; set; } = [];
}

public class PlatformConfigFlagRow
{
    public string Flag { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class PlatformApiNodeRow
{
    public string Hostname { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string Version { get; set; } = string.Empty;
    public DateTime StartTimeUtc { get; set; }
    public DateTime LastSeen { get; set; }
    public string MemoryMb { get; set; } = string.Empty;
    public long TotalRequests { get; set; }
    public double RequestsPerMinute { get; set; }
    public string RunningUser { get; set; } = string.Empty;
    public string ConfigurationError { get; set; } = string.Empty;
}

public class FigPlatformReport : ReportBase<FigPlatformParameters, FigPlatformReportModel>
{
    private readonly IApiStatusRepository _apiStatusRepository;
    private readonly IConfigurationRepository _configurationRepository;
    private readonly IEventLogRepository _eventLogRepository;
    private readonly ISettingClientRepository _settingClientRepository;
    private readonly IUserRepository _userRepository;
    private readonly IWebHookRepository _webHookRepository;
    private readonly ISettingGroupRepository _settingGroupRepository;

    public FigPlatformReport(
        IApiStatusRepository apiStatusRepository,
        IConfigurationRepository configurationRepository,
        IEventLogRepository eventLogRepository,
        ISettingClientRepository settingClientRepository,
        IUserRepository userRepository,
        IWebHookRepository webHookRepository,
        ISettingGroupRepository settingGroupRepository)
    {
        _apiStatusRepository = apiStatusRepository;
        _configurationRepository = configurationRepository;
        _eventLogRepository = eventLogRepository;
        _settingClientRepository = settingClientRepository;
        _userRepository = userRepository;
        _webHookRepository = webHookRepository;
        _settingGroupRepository = settingGroupRepository;
    }

    public override string Id => "fig-platform";
    public override string Name => "Fig Platform Self-Report";
    public override string Category => "Platform";
    public override string Description =>
        "Snapshot of Fig API nodes, feature configuration gates, and platform inventory counts.";
    public override Type BodyComponentType => typeof(FigPlatformReportView);

    public override async Task<object> ExecuteAsync(FigPlatformParameters parameters, CancellationToken cancellationToken = default)
    {
        var apiNodes = await _apiStatusRepository.GetAllActive();
        var configuration = await _configurationRepository.GetConfiguration();
        var eventLogCount = await _eventLogRepository.GetEventLogCount();
        var clients = await _settingClientRepository.GetAllClients(RequireAuthenticatedUser());
        var users = await _userRepository.GetAllUsers();
        var webhooks = await _webHookRepository.GetWebHooks();
        var groups = await _settingGroupRepository.GetAllGroups();

        return new FigPlatformReportModel
        {
            Summary =
            [
                new SummaryCardItem("Active API Nodes", apiNodes.Count.ToString()),
                new SummaryCardItem("Clients", clients.Count.ToString()),
                new SummaryCardItem("Users", users.Count.ToString()),
                new SummaryCardItem("Webhooks", webhooks.Count.ToString()),
                new SummaryCardItem("Setting Groups", groups.Count.ToString()),
                new SummaryCardItem("Event Log Rows", eventLogCount.ToString("N0"))
            ],
            ConfigFlags = BuildConfigFlags(configuration),
            ApiNodes = apiNodes
                .OrderBy(n => n.Hostname, StringComparer.OrdinalIgnoreCase)
                .Select(ToApiNodeRow)
                .ToList()
        };
    }

    private static IReadOnlyList<PlatformConfigFlagRow> BuildConfigFlags(FigConfigurationBusinessEntity config)
        =>
        [
            new() { Flag = "Allow New Registrations", Value = Flag(config.AllowNewRegistrations) },
            new() { Flag = "Allow Updated Registrations", Value = Flag(config.AllowUpdatedRegistrations) },
            new() { Flag = "Allow File Imports", Value = Flag(config.AllowFileImports) },
            new() { Flag = "Allow Offline Settings", Value = Flag(config.AllowOfflineSettings) },
            new() { Flag = "Allow Client Overrides", Value = Flag(config.AllowClientOverrides) },
            new() { Flag = "Client Overrides Regex", Value = config.ClientOverridesRegex ?? string.Empty },
            new() { Flag = "Use Azure Key Vault", Value = Flag(config.UseAzureKeyVault) },
            new() { Flag = "Allow Display Scripts", Value = Flag(config.AllowDisplayScripts) },
            new() { Flag = "Enable Time Machine", Value = Flag(config.EnableTimeMachine) },
            new() { Flag = "Allow Migrate From Migrations", Value = Flag(config.AllowMigrateFromMigrations) },
            new() { Flag = "Timeline Duration Days", Value = config.TimelineDurationDays.ToString() },
            new() { Flag = "Time Machine Cleanup Days", Value = FormatOptional(config.TimeMachineCleanupDays) },
            new() { Flag = "Event Logs Cleanup Days", Value = FormatOptional(config.EventLogsCleanupDays) },
            new() { Flag = "API Status Cleanup Days", Value = FormatOptional(config.ApiStatusCleanupDays) },
            new() { Flag = "Setting History Cleanup Days", Value = FormatOptional(config.SettingHistoryCleanupDays) },
            new() { Flag = "Poll Interval Override", Value = config.PollIntervalOverride?.ToString() ?? "None" },
            new() { Flag = "Web Application Base Address", Value = config.WebApplicationBaseAddress ?? string.Empty }
        ];

    private static PlatformApiNodeRow ToApiNodeRow(ApiStatusBusinessEntity node)
        => new()
        {
            Hostname = node.Hostname ?? string.Empty,
            IpAddress = node.IpAddress,
            Version = node.Version,
            StartTimeUtc = node.StartTimeUtc,
            LastSeen = node.LastSeen,
            MemoryMb = (node.MemoryUsageBytes / (1024d * 1024d)).ToString("0.#"),
            TotalRequests = node.TotalRequests,
            RequestsPerMinute = node.RequestsPerMinute,
            RunningUser = node.RunningUser,
            ConfigurationError = node.ConfigurationErrorDetected ? "Yes" : "No"
        };

    private static string Flag(bool value) => value ? "Enabled" : "Disabled";

    private static string FormatOptional(int? value) => value?.ToString() ?? "None";
}
