using Fig.Api.Assistant;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Reports.Rendering.Views;
using Fig.Api.Services;
using Fig.Contracts.Reports;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Fig.Api.Reports.Implementations;

public class AiComposedReportParameters
{
    [ReportParameter("Prompt")]
    public string Prompt { get; set; } = string.Empty;
}

public class AiComposedReport : ReportBase<AiComposedReportParameters, AiReportDocument>
{
    public const string ReportId = "ai-report";
    public const string SubmitToolName = "submit_ai_report";

    /// <summary>
    /// Focused read tools for AI reports. Heavy dumps (list_clients, get_client_settings, docs, lookups) are excluded.
    /// </summary>
    internal static readonly string[] CuratedToolNames =
    [
        "get_event_count",
        "get_events",
        "get_last_changed",
        "get_client_timeline",
        "get_run_sessions",
        "get_client_descriptions",
        "list_reports",
        "get_api_status",
        "get_api_version",
        "list_setting_groups",
        "list_deferred_changes",
        "list_checkpoints"
    ];

    private readonly IAssistantBackgroundRunner _backgroundRunner;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfigurationRepository _configurationRepository;
    private readonly IEncryptionService _encryptionService;

    public AiComposedReport(
        IAssistantBackgroundRunner backgroundRunner,
        IServiceProvider serviceProvider,
        IConfigurationRepository configurationRepository,
        IEncryptionService encryptionService)
    {
        _backgroundRunner = backgroundRunner;
        _serviceProvider = serviceProvider;
        _configurationRepository = configurationRepository;
        _encryptionService = encryptionService;
    }

    public override string Id => ReportId;

    public override string Name => "AI Report";

    public override string Category => "AI";

    public override string Description =>
        "Describe what you want to learn; Fig Assistant gathers data with read tools and builds a structured HTML report.";

    public override Type BodyComponentType => typeof(AiReportView);

    public override async Task<object> ExecuteAsync(
        AiComposedReportParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var configuration = await _configurationRepository.GetConfiguration();
        if (!FigAssistantAvailability.IsReady(configuration, _encryptionService))
            throw new InvalidOperationException(
                "The AI Report requires Fig Assistant to be enabled and fully configured.");

        if (string.IsNullOrWhiteSpace(parameters.Prompt))
            throw new ReportParameterValidationException("Prompt is required.");

        // Resolve tools lazily to avoid DI cycle:
        // AiComposedReport -> ToolRegistry -> ReportExecutionService -> ReportRegistry -> AiComposedReport
        var toolRegistry = _serviceProvider.GetRequiredService<IAssistantToolRegistry>();

        AiReportDocument? submitted = null;
        var submitTool = CreateSubmitTool(doc => submitted = doc);
        var tools = ResolveCuratedTools(toolRegistry).Append(submitTool).ToArray();

        var systemPrompt = """
            You are Fig Assistant composing an HTML report for Fig administrators.
            Use read tools to gather facts. Never invent clients, settings, counts, dates, or events.
            The only successful completion is calling submit_ai_report exactly once. Never end with assistant text.
            Never ask clarifying questions — always submit the best report you can from available tool results.
            Prefer existing reports via list_reports when they match the ask (for example user activity or similar canned reports).
            For activity or most-active-user questions: call get_events with an explicit UTC startTime/endTime range
            relative to the current UTC time provided in the user message (typically the last 7 or 30 days).
            Do not invent historical years such as 2023.
            Prefer filtered get_events over broad dumps: use authenticatedUser, clientName, eventTypes, and/or searchText
            when the ask names a user, client, or event kind. Aggregate AuthenticatedUser (and related fields) from the results,
            and do not pull full client settings dumps.
            Cap exploration: after enough grounded facts (aim for at most 4–6 tool rounds), call submit_ai_report
            with a structured document built only from tool results.
            If data is sparse or the range seems mismatched, say so clearly in markdown and still submit.
            Do not emit HTML, CSS, Chart.js, or markdown tables as the final answer — only submit_ai_report.
            Prefer summary cards, charts (with numeric data points), tables, timelines, and short markdown narrative.
            Chart and table data must come from tool results. Keep markdown concise and grounded.
            Never reveal secrets, credentials, tokens, or passwords.
            """;

        var userMessage = $"""
            Current UTC time: {DateTime.UtcNow:O}

            Build a Fig report for this request:

            {parameters.Prompt.Trim()}
            """;

        try
        {
            await _backgroundRunner.RunAsync(
                "ai-composed-report",
                systemPrompt,
                userMessage,
                tools,
                cancellationToken);
        }
        catch (InvalidOperationException ex) when (
            ex.Message.Contains("tool iteration limit", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The AI did not submit a valid report document. Try refining the prompt or try again.",
                ex);
        }

        if (submitted is null)
            throw new InvalidOperationException(
                "The AI did not submit a valid report document. Try refining the prompt or try again.");

        return submitted;
    }

    internal static IReadOnlyCollection<IAssistantTool> ResolveCuratedTools(IAssistantToolRegistry toolRegistry)
    {
        var tools = new List<IAssistantTool>();
        foreach (var name in CuratedToolNames)
        {
            if (toolRegistry.TryGet(name, out var tool) && tool is not null)
                tools.Add(tool);
        }

        return tools;
    }

    private static IAssistantTool CreateSubmitTool(Action<AiReportDocument> onSubmitted)
    {
        const string schema = """
            {
              "type": "object",
              "properties": {
                "title": { "type": "string" },
                "description": { "type": "string" },
                "sections": {
                  "type": "array",
                  "items": {
                    "type": "object",
                    "properties": {
                      "type": { "type": "string", "enum": ["summary", "markdown", "table", "chart", "timeline"] },
                      "items": { "type": "array" },
                      "content": { "type": "string" },
                      "title": { "type": "string" },
                      "columns": { "type": "array", "items": { "type": "string" } },
                      "rows": { "type": "array" },
                      "chartType": { "type": "string", "enum": ["pie", "doughnut", "bar"] },
                      "datasetLabel": { "type": "string" },
                      "data": { "type": "array" }
                    },
                    "required": ["type"],
                    "additionalProperties": true
                  }
                }
              },
              "required": ["title", "sections"],
              "additionalProperties": false
            }
            """;

        return new SubmitAiReportTool(schema, onSubmitted);
    }

    private sealed class SubmitAiReportTool : IAssistantTool
    {
        private readonly Action<AiReportDocument> _onSubmitted;

        public SubmitAiReportTool(string parameterJsonSchema, Action<AiReportDocument> onSubmitted)
        {
            ParameterJsonSchema = parameterJsonSchema;
            _onSubmitted = onSubmitted;
        }

        public string Name => SubmitToolName;

        public string Description =>
            "Submit the final structured report document. Fig.Api validates the JSON and renders summary cards, " +
            "markdown, tables, charts, and timelines. Call exactly once when ready. Minimal valid example: " +
            "{\"title\":\"Report\",\"sections\":[{\"type\":\"summary\",\"items\":[{\"label\":\"Total\",\"value\":\"10\"}]}," +
            "{\"type\":\"markdown\",\"content\":\"Short grounded narrative.\"}]}.";

        public string ParameterJsonSchema { get; }

        public Task<string> ExecuteAsync(string argumentsJson, CancellationToken cancellationToken)
        {
            try
            {
                var document = AiReportDocumentValidator.ParseAndValidate(argumentsJson);
                _onSubmitted(document);
                return Task.FromResult(JsonConvert.SerializeObject(new
                {
                    ok = true,
                    title = document.Title,
                    sectionCount = document.Sections.Count
                }));
            }
            catch (AiReportValidationException ex)
            {
                return Task.FromResult(JsonConvert.SerializeObject(new { error = ex.Message }));
            }
        }
    }
}
