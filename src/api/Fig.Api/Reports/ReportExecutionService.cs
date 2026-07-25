using System.Reflection;
using Fig.Api.Assistant;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.ExtensionMethods;
using Fig.Api.Reports.Implementations;
using Fig.Api.Services;
using Fig.Contracts.Reports;
using Microsoft.Extensions.Logging;

namespace Fig.Api.Reports;

public class ReportExecutionService : AuthenticatedService, IReportExecutionService
{
    private readonly IReportRegistry _registry;
    private readonly IReportParameterBinder _parameterBinder;
    private readonly IEnumerable<IReportRenderer> _renderers;
    private readonly IReportAiAnalysisService _aiAnalysisService;
    private readonly IConfigurationRepository _configurationRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<ReportExecutionService> _logger;

    public ReportExecutionService(
        IReportRegistry registry,
        IReportParameterBinder parameterBinder,
        IEnumerable<IReportRenderer> renderers,
        IReportAiAnalysisService aiAnalysisService,
        IConfigurationRepository configurationRepository,
        IEncryptionService encryptionService,
        ILogger<ReportExecutionService> logger)
    {
        _registry = registry;
        _parameterBinder = parameterBinder;
        _renderers = renderers;
        _aiAnalysisService = aiAnalysisService;
        _configurationRepository = configurationRepository;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<IList<ReportDefinitionDataContract>> GetAvailableReports()
    {
        var assistantReady = await IsAssistantReadyAsync();
        return _registry.GetAll()
            .Where(r => assistantReady ||
                        !string.Equals(r.Id, AiComposedReport.ReportId, StringComparison.OrdinalIgnoreCase))
            .Select(r => new ReportDefinitionDataContract(
                r.Id,
                r.Name,
                r.Category,
                r.Description,
                r.GetParameterDefinitions(),
                supportsAiAnalysis: assistantReady &&
                                    !string.Equals(r.Id, AiComposedReport.ReportId, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    public async Task<(string Html, string ContentType)> ExecuteAsync(
        string reportId,
        ReportExecutionRequestDataContract request,
        CancellationToken cancellationToken = default)
    {
        var report = _registry.Get(reportId)
                     ?? throw new ReportNotFoundException(reportId);

        var isAiComposedReport = string.Equals(
            report.Id, AiComposedReport.ReportId, StringComparison.OrdinalIgnoreCase);
        var enableAiAnalysis = request.EnableAiAnalysis && !isAiComposedReport;
        var assistantReady = isAiComposedReport || enableAiAnalysis
            ? await IsAssistantReadyAsync()
            : false;

        if (isAiComposedReport && !assistantReady)
        {
            throw new InvalidOperationException(
                "The AI Report requires Fig Assistant to be enabled and fully configured.");
        }

        var format = request.Format;
        var renderer = _renderers.FirstOrDefault(r => r.CanRender(format))
                       ?? throw new NotSupportedException($"Report format '{format}' is not supported.");

        var parameters = _parameterBinder.Bind(report.ParametersType, request.Parameters);
        var model = await report.ExecuteAsync(parameters, cancellationToken);

        var parameterSummary = BuildParameterSummary(report, parameters);
        string? aiAnalysis = null;

        if (enableAiAnalysis)
        {
            if (assistantReady)
            {
                aiAnalysis = await _aiAnalysisService.AnalyzeAsync(
                    report.Name,
                    model,
                    request.AiPrompt,
                    cancellationToken);
                parameterSummary["AI Analysis"] = aiAnalysis is null ? "Requested (omitted)" : "Yes";
                if (!string.IsNullOrWhiteSpace(request.AiPrompt))
                {
                    var truncated = request.AiPrompt.Trim();
                    if (truncated.Length > 120)
                        truncated = truncated.Substring(0, 117) + "...";
                    parameterSummary["AI Prompt"] = truncated;
                }
            }
            else
            {
                _logger.LogInformation(
                    "AI analysis requested for {ReportId} but Fig Assistant is not ready; omitting.",
                    reportId.Sanitize());
            }
        }

        var title = report.Name;
        var description = report.Description;
        if (model is IReportDocumentMetadata metadata)
        {
            if (!string.IsNullOrWhiteSpace(metadata.Title))
                title = metadata.Title;
            if (!string.IsNullOrWhiteSpace(metadata.Description))
                description = metadata.Description!;
        }

        var context = new ReportRenderContext
        {
            Title = title,
            Description = description,
            GeneratedAtUtc = DateTime.UtcNow,
            GeneratedBy = RequireAuthenticatedUser().Username,
            ParameterSummary = parameterSummary,
            PageOrientation = report.PageOrientation,
            BodyComponentType = report.BodyComponentType,
            Model = model,
            AiAnalysisMarkdown = aiAnalysis
        };

        var html = await renderer.RenderAsync(context, cancellationToken);
        return (html, "text/html; charset=utf-8");
    }

    private async Task<bool> IsAssistantReadyAsync()
    {
        try
        {
            var configuration = await _configurationRepository.GetConfiguration();
            return FigAssistantAvailability.IsReady(configuration, _encryptionService);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to evaluate Fig Assistant availability.");
            return false;
        }
    }

    private static Dictionary<string, string> BuildParameterSummary(IReport report, object parameters)
    {
        var summary = new Dictionary<string, string>();
        var definitions = report.GetParameterDefinitions()
            .ToDictionary(d => d.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var property in report.ParametersType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead)
                continue;

            var displayName = definitions.TryGetValue(property.Name, out var def)
                ? def.DisplayName
                : property.Name;
            var value = property.GetValue(parameters);
            summary[displayName] = FormatValue(value);
        }

        return summary;
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => "(none)",
            DateTime dt => dt.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss") + " UTC",
            _ => Convert.ToString(value) ?? "(none)"
        };
    }
}

public class ReportNotFoundException : Exception
{
    public ReportNotFoundException(string reportId)
        : base($"Report '{reportId}' was not found.")
    {
        ReportId = reportId;
    }

    public string ReportId { get; }
}
