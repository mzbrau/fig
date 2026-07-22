using Fig.Api.Datalayer.Repositories;
using Fig.Api.Reports.Rendering.Components;
using Fig.Api.Reports.Rendering.Views;
using Fig.Common.Constants;
using Fig.Contracts.Reports;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Reports.Implementations;

public class ChangeAnalyticsParameters
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

public class ChangeAnalyticsReportModel
{
    public string ScopeDisplay { get; set; } = string.Empty;
    public IReadOnlyList<SummaryCardItem> Summary { get; set; } = [];
    public IReadOnlyList<ChartSlice> DailyVolume { get; set; } = [];
    public IReadOnlyList<TopCountRow> TopClients { get; set; } = [];
    public IReadOnlyList<TopCountRow> TopSettings { get; set; } = [];
    public IReadOnlyList<TopCountRow> TopUsers { get; set; } = [];
    public IReadOnlyList<UnchangedSettingRow> UnchangedSettings { get; set; } = [];
}

public class TopCountRow
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class UnchangedSettingRow
{
    public string SettingName { get; set; } = string.Empty;
}

public class ChangeAnalyticsReport : ReportBase<ChangeAnalyticsParameters, ChangeAnalyticsReportModel>
{
    private static readonly string[] ChangeEventTypes =
    [
        EventMessage.SettingValueUpdated,
        EventMessage.ExternallyManagedSettingUpdatedByUser
    ];

    private readonly IEventLogRepository _eventLogRepository;
    private readonly ISettingClientRepository _settingClientRepository;

    public ChangeAnalyticsReport(
        IEventLogRepository eventLogRepository,
        ISettingClientRepository settingClientRepository)
    {
        _eventLogRepository = eventLogRepository;
        _settingClientRepository = settingClientRepository;
    }

    public override string Id => "change-analytics";
    public override string Name => "Change Analytics Report";
    public override string Category => "Analytics";
    public override string Description =>
        "Analyzes setting change volume over a date range, including top clients, settings, and users, plus externally managed share.";
    public override Type BodyComponentType => typeof(ChangeAnalyticsReportView);
    public override ReportPageOrientation PageOrientation => ReportPageOrientation.Landscape;

    public override async Task<object> ExecuteAsync(ChangeAnalyticsParameters parameters, CancellationToken cancellationToken = default)
    {
        var (from, to) = ReportDateRange.Validate(parameters.From, parameters.To);
        var clientName = ReportDateRange.NormalizeOptionalClient(parameters.ClientName);
        var instance = string.IsNullOrWhiteSpace(parameters.Instance) ? null : parameters.Instance.Trim();

        if (clientName is not null)
            ThrowIfNoAccess(clientName);

        var events = (await _eventLogRepository.GetEventsByTypes(from, to, ChangeEventTypes, RequireAuthenticatedUser(), clientName, instance))
            .OrderByDescending(e => e.Timestamp)
            .ToList();

        var externalCount = EventAnalytics.CountOfType(events, EventMessage.ExternallyManagedSettingUpdatedByUser);
        var externalShare = events.Count == 0
            ? "0%"
            : $"{(100.0 * externalCount / events.Count):0.#}%";

        var distinctSettings = events
            .Select(e => e.SettingName)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var distinctUsers = events
            .Select(e => e.AuthenticatedUser)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        var dailyVolume = EventAnalytics.DailySeries(events, from, to)
            .Select(d => new ChartSlice(d.Day.ToString("yyyy-MM-dd"), d.Count))
            .ToList();

        var topClients = EventAnalytics.TopBy(
                events,
                e => string.IsNullOrWhiteSpace(e.ClientName)
                    ? null
                    : ReportValueFormatter.FormatClientDisplay(e.ClientName, e.Instance))
            .Select(t => new TopCountRow { Name = t.Key, Count = t.Count })
            .ToList();

        var topSettings = EventAnalytics.TopBy(events, e => e.SettingName)
            .Select(t => new TopCountRow { Name = t.Key, Count = t.Count })
            .ToList();

        var topUsers = EventAnalytics.TopBy(events, e => e.AuthenticatedUser)
            .Select(t => new TopCountRow { Name = t.Key, Count = t.Count })
            .ToList();

        IReadOnlyList<UnchangedSettingRow> unchanged = [];
        if (clientName is not null)
        {
            var client = await _settingClientRepository.GetClient(clientName, instance)
                         ?? throw new KeyNotFoundException($"Client '{clientName}' was not found.");

            var changedNames = events
                .Select(e => e.SettingName)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            unchanged = client.Settings
                .Where(s => !changedNames.Contains(s.Name))
                .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                .Select(s => new UnchangedSettingRow { SettingName = s.Name })
                .ToList();
        }

        return new ChangeAnalyticsReportModel
        {
            ScopeDisplay = clientName is null
                ? "All clients"
                : ReportValueFormatter.FormatClientDisplay(clientName, instance),
            Summary =
            [
                new SummaryCardItem("Total Changes", events.Count.ToString()),
                new SummaryCardItem("Regular Updates", (events.Count - externalCount).ToString()),
                new SummaryCardItem("Externally Managed Updates", externalCount.ToString()),
                new SummaryCardItem("Externally Managed Share", externalShare),
                new SummaryCardItem("Distinct Settings", distinctSettings.ToString()),
                new SummaryCardItem("Distinct Users", distinctUsers.ToString())
            ],
            DailyVolume = dailyVolume,
            TopClients = topClients,
            TopSettings = topSettings,
            TopUsers = topUsers,
            UnchangedSettings = unchanged
        };
    }
}
