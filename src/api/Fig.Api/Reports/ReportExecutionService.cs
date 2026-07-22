using System.Reflection;
using Fig.Api.Services;
using Fig.Contracts.Reports;

namespace Fig.Api.Reports;

public class ReportExecutionService : AuthenticatedService, IReportExecutionService
{
    private readonly IReportRegistry _registry;
    private readonly IReportParameterBinder _parameterBinder;
    private readonly IEnumerable<IReportRenderer> _renderers;

    public ReportExecutionService(
        IReportRegistry registry,
        IReportParameterBinder parameterBinder,
        IEnumerable<IReportRenderer> renderers)
    {
        _registry = registry;
        _parameterBinder = parameterBinder;
        _renderers = renderers;
    }

    public IList<ReportDefinitionDataContract> GetAvailableReports()
    {
        return _registry.GetAll()
            .Select(r => new ReportDefinitionDataContract(
                r.Id,
                r.Name,
                r.Category,
                r.Description,
                r.GetParameterDefinitions()))
            .ToList();
    }

    public async Task<(string Html, string ContentType)> ExecuteAsync(
        string reportId,
        ReportExecutionRequestDataContract request,
        CancellationToken cancellationToken = default)
    {
        var report = _registry.Get(reportId)
                     ?? throw new ReportNotFoundException(reportId);

        var format = request.Format;
        var renderer = _renderers.FirstOrDefault(r => r.CanRender(format))
                       ?? throw new NotSupportedException($"Report format '{format}' is not supported.");

        var parameters = _parameterBinder.Bind(report.ParametersType, request.Parameters);
        var model = await report.ExecuteAsync(parameters, cancellationToken);

        var context = new ReportRenderContext
        {
            Title = report.Name,
            Description = report.Description,
            GeneratedAtUtc = DateTime.UtcNow,
            GeneratedBy = RequireAuthenticatedUser().Username,
            ParameterSummary = BuildParameterSummary(report, parameters),
            PageOrientation = report.PageOrientation,
            BodyComponentType = report.BodyComponentType,
            Model = model
        };

        var html = await renderer.RenderAsync(context, cancellationToken);
        return (html, "text/html; charset=utf-8");
    }

    private static IReadOnlyDictionary<string, string> BuildParameterSummary(IReport report, object parameters)
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
