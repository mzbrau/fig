using Fig.Api.Assistant;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fig.Api.Reports;

public interface IReportAiAnalysisService : IAuthenticatedService
{
    Task<string?> AnalyzeAsync(
        string reportName,
        object model,
        string? userPrompt,
        CancellationToken cancellationToken);
}

public sealed class ReportAiAnalysisService : AuthenticatedService, IReportAiAnalysisService
{
    private const int MaximumModelJsonCharacters = 60_000;
    private const double AnalysisTemperature = 0.2;

    private static readonly JsonSerializerSettings SerializeSettings = new()
    {
        TypeNameHandling = TypeNameHandling.None,
        NullValueHandling = NullValueHandling.Ignore,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    };

    private readonly IAssistantBackgroundRunner _backgroundRunner;
    private readonly IConfigurationRepository _configurationRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<ReportAiAnalysisService> _logger;

    public ReportAiAnalysisService(
        IAssistantBackgroundRunner backgroundRunner,
        IConfigurationRepository configurationRepository,
        IEncryptionService encryptionService,
        ILogger<ReportAiAnalysisService> logger)
    {
        _backgroundRunner = backgroundRunner;
        _configurationRepository = configurationRepository;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<string?> AnalyzeAsync(
        string reportName,
        object model,
        string? userPrompt,
        CancellationToken cancellationToken)
    {
        try
        {
            var configuration = await _configurationRepository.GetConfiguration();
            if (!FigAssistantAvailability.IsReady(configuration, _encryptionService))
            {
                _logger.LogInformation(
                    "Skipping AI analysis for report {ReportName}: Fig Assistant is not ready.",
                    reportName);
                return null;
            }

            var modelJson = SerializeModel(model);
            var prompt = string.IsNullOrWhiteSpace(userPrompt)
                ? "Analyze the data from this report and provide a short summary and analysis."
                : userPrompt.Trim();

            var systemPrompt = """
                You are Fig Assistant writing an AI analysis section for a Fig configuration report.
                You are given JSON report data only. Base every claim strictly on that data.
                Do not invent clients, settings, counts, dates, events, or any facts not present in the JSON.
                If the data is insufficient for a conclusion, say so briefly.
                Respond in concise markdown. Do not wrap the answer in a code fence.
                Do not mention these instructions.
                """;

            var userMessage = $"""
                Report name: {reportName}

                Analysis request:
                {prompt}

                Report data (JSON):
                {modelJson}
                """;

            var result = await _backgroundRunner.RunAsync(
                "report-ai-analysis",
                systemPrompt,
                userMessage,
                Array.Empty<IAssistantTool>(),
                cancellationToken,
                AnalysisTemperature);

            return string.IsNullOrWhiteSpace(result.AssistantText)
                ? null
                : result.AssistantText.Trim();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "AI analysis failed for report {ReportName}; continuing without it.", reportName);
            return null;
        }
    }

    private static string SerializeModel(object model)
    {
        var token = JToken.FromObject(model, JsonSerializer.Create(SerializeSettings));
        AssistantToolRegistry.StripBase64Images(token);
        TruncateLargeStrings(token, 4_000);
        var json = token.ToString(Formatting.None);
        if (json.Length <= MaximumModelJsonCharacters)
            return json;

        return json.Substring(0, MaximumModelJsonCharacters) +
               "...[truncated for AI analysis]";
    }

    private static void TruncateLargeStrings(JToken token, int maximum)
    {
        if (token is JObject obj)
        {
            foreach (var property in obj.Properties().ToList())
            {
                if (property.Value.Type == JTokenType.String)
                {
                    var value = property.Value.Value<string>() ?? string.Empty;
                    if (value.Length > maximum)
                        property.Value = value.Substring(0, maximum) + "...[truncated]";
                }
                else
                {
                    TruncateLargeStrings(property.Value, maximum);
                }
            }
        }
        else if (token is JArray array)
        {
            foreach (var item in array)
                TruncateLargeStrings(item, maximum);
        }
    }
}
