using Fig.Api.Datalayer.Repositories;
using Fig.Api.Reports.Rendering.Components;
using Fig.Api.Reports.Rendering.Views;
using Fig.Common.Constants;
using Fig.Contracts.WebHook;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Reports.Implementations;

public class WebhookDeliveryParameters
{
    [ReportParameter("From")]
    public DateTime From { get; set; }

    [ReportParameter("To")]
    public DateTime To { get; set; }
}

public class WebhookDeliveryReportModel
{
    public IReadOnlyList<SummaryCardItem> Summary { get; set; } = [];
    public IReadOnlyList<ChartSlice> SendsByType { get; set; } = [];
    public IReadOnlyList<WebhookDefinitionRow> Definitions { get; set; } = [];
    public IReadOnlyList<WebhookZeroSendRow> ZeroSends { get; set; } = [];
    public IReadOnlyList<SessionFlapRow> SessionFlaps { get; set; } = [];
}

public class WebhookDefinitionRow
{
    public string ClientName { get; set; } = string.Empty;
    public string BaseUri { get; set; } = string.Empty;
    public string WebHookType { get; set; } = string.Empty;
    public string ClientNameRegex { get; set; } = string.Empty;
    public string? SettingNameRegex { get; set; }
    public int MinSessions { get; set; }
    public int SendCount { get; set; }
}

public class WebhookZeroSendRow
{
    public string ClientName { get; set; } = string.Empty;
    public string BaseUri { get; set; } = string.Empty;
    public string WebHookType { get; set; } = string.Empty;
    public string ClientNameRegex { get; set; } = string.Empty;
}

public class SessionFlapRow
{
    public string ClientDisplay { get; set; } = string.Empty;
    public int NewSessions { get; set; }
    public int ExpiredSessions { get; set; }
    public int FlapCount { get; set; }
}

public class WebhookDeliveryReport : ReportBase<WebhookDeliveryParameters, WebhookDeliveryReportModel>
{
    private static readonly string[] WebHookEventTypes = [EventMessage.WebHookSent];

    private static readonly string[] SessionEventTypes =
    [
        EventMessage.NewSession,
        EventMessage.ExpiredSession
    ];

    private readonly IWebHookRepository _webHookRepository;
    private readonly IWebHookClientRepository _webHookClientRepository;
    private readonly IEventLogRepository _eventLogRepository;

    public WebhookDeliveryReport(
        IWebHookRepository webHookRepository,
        IWebHookClientRepository webHookClientRepository,
        IEventLogRepository eventLogRepository)
    {
        _webHookRepository = webHookRepository;
        _webHookClientRepository = webHookClientRepository;
        _eventLogRepository = eventLogRepository;
    }

    public override string Id => "webhook-delivery";
    public override string Name => "Webhook Delivery Report";
    public override string Category => "Integrations";
    public override string Description =>
        "Shows webhook send activity by type, configured hooks with zero sends, and session-flap noise context.";
    public override Type BodyComponentType => typeof(WebhookDeliveryReportView);
    public override ReportPageOrientation PageOrientation => ReportPageOrientation.Landscape;

    public override async Task<object> ExecuteAsync(WebhookDeliveryParameters parameters, CancellationToken cancellationToken = default)
    {
        var (from, to) = ReportDateRange.Validate(parameters.From, parameters.To);

        var hooks = await _webHookRepository.GetWebHooks();
        var clients = (await _webHookClientRepository.GetClients(false))
            .ToDictionary(c => c.Id);

        var sendEvents = await _eventLogRepository.GetEventsByTypes(from, to, WebHookEventTypes, RequireAuthenticatedUser());
        var sessionEvents = await _eventLogRepository.GetEventsByTypes(from, to, SessionEventTypes, RequireAuthenticatedUser());

        var definitions = hooks
            .Select(hook =>
            {
                clients.TryGetValue(hook.ClientId, out var client);
                var clientName = client?.Name ?? hook.ClientId.ToString();
                var baseUri = client?.BaseUri ?? string.Empty;
                var typeName = hook.WebHookType.ToString();
                var sendCount = sendEvents.Count(e => MatchesSend(e, typeName, baseUri));

                return new WebhookDefinitionRow
                {
                    ClientName = clientName,
                    BaseUri = baseUri,
                    WebHookType = typeName,
                    ClientNameRegex = hook.ClientNameRegex,
                    SettingNameRegex = hook.SettingNameRegex,
                    MinSessions = hook.MinSessions,
                    SendCount = sendCount
                };
            })
            .OrderBy(r => r.ClientName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.WebHookType, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var sendsByType = sendEvents
            .Select(e => ParseWebHookType(e.Message) ?? "Unknown")
            .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
            .Select(g => new ChartSlice(g.Key, g.Count()))
            .OrderByDescending(s => s.Value)
            .ThenBy(s => s.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var zeroSends = definitions
            .Where(d => d.SendCount == 0)
            .Select(d => new WebhookZeroSendRow
            {
                ClientName = d.ClientName,
                BaseUri = d.BaseUri,
                WebHookType = d.WebHookType,
                ClientNameRegex = d.ClientNameRegex
            })
            .ToList();

        var sessionFlaps = sessionEvents
            .Where(e => !string.IsNullOrWhiteSpace(e.ClientName))
            .GroupBy(
                e => ReportValueFormatter.FormatClientDisplay(e.ClientName!, e.Instance),
                StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var newCount = g.Count(e => e.EventType == EventMessage.NewSession);
                var expiredCount = g.Count(e => e.EventType == EventMessage.ExpiredSession);
                return new SessionFlapRow
                {
                    ClientDisplay = g.Key,
                    NewSessions = newCount,
                    ExpiredSessions = expiredCount,
                    FlapCount = newCount + expiredCount
                };
            })
            .OrderByDescending(r => r.FlapCount)
            .ThenBy(r => r.ClientDisplay, StringComparer.OrdinalIgnoreCase)
            .Take(15)
            .ToList();

        return new WebhookDeliveryReportModel
        {
            Summary =
            [
                new SummaryCardItem("Configured Hooks", definitions.Count.ToString()),
                new SummaryCardItem("Webhook Sends", sendEvents.Count.ToString()),
                new SummaryCardItem("Hooks With Zero Sends", zeroSends.Count.ToString()),
                new SummaryCardItem("Distinct Hook Types Sent", sendsByType.Count.ToString())
            ],
            SendsByType = sendsByType,
            Definitions = definitions,
            ZeroSends = zeroSends,
            SessionFlaps = sessionFlaps
        };
    }

    private static bool MatchesSend(EventLogBusinessEntity log, string webHookType, string baseUri)
        => MatchesSend(log.Message, webHookType, baseUri);

    internal static bool MatchesSend(string? message, string webHookType, string baseUri)
    {
        if (string.IsNullOrWhiteSpace(message))
            return false;

        if (!message.StartsWith(webHookType, StringComparison.OrdinalIgnoreCase))
            return false;

        if (string.IsNullOrWhiteSpace(baseUri))
            return true;

        return message.Contains(baseUri, StringComparison.OrdinalIgnoreCase);
    }

    internal static string? ParseWebHookType(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return null;

        foreach (var type in Enum.GetValues<WebHookType>())
        {
            var name = type.ToString();
            if (message.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                return name;
        }

        return null;
    }
}
