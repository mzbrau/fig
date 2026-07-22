using Fig.Common.Events;
using Fig.Contracts.Reports;
using Fig.Web.Events;
using Fig.Web.Models.Reports;
using Fig.Web.Services;

namespace Fig.Web.Facades;

public class ReportsFacade : IReportsFacade
{
    private readonly IHttpService _httpService;
    private readonly List<ReportDefinitionModel> _reports = new();

    public ReportsFacade(IHttpService httpService, IEventDistributor eventDistributor)
    {
        _httpService = httpService;
        eventDistributor.Subscribe(EventConstants.LogoutEvent, () => _reports.Clear());
    }

    public IReadOnlyList<ReportDefinitionModel> Reports => _reports;

    public async Task LoadReports()
    {
        var result = await _httpService.Get<List<ReportDefinitionDataContract>>("reports");
        _reports.Clear();
        if (result is null)
            return;

        foreach (var report in result)
        {
            _reports.Add(new ReportDefinitionModel
            {
                Id = report.Id,
                Name = report.Name,
                Category = report.Category,
                Description = report.Description,
                Parameters = report.Parameters.Select(p => new ReportParameterModel
                {
                    Name = p.Name,
                    DisplayName = p.DisplayName,
                    Type = p.Type,
                    Required = p.Required,
                    LookupKind = p.LookupKind,
                    Value = CreateDefaultValue(p)
                }).ToList()
            });
        }
    }

    public async Task<string?> GenerateReport(string reportId, Dictionary<string, object?> parameters)
    {
        var request = new ReportExecutionRequestDataContract(parameters);
        return await _httpService.PostForString($"reports/{Uri.EscapeDataString(reportId)}", request);
    }

    private static object? CreateDefaultValue(ReportParameterDataContract parameter)
    {
        if (parameter.Type == ReportParameterType.DateTime)
        {
            if (string.Equals(parameter.Name, "From", StringComparison.OrdinalIgnoreCase))
                return DateTime.UtcNow.Date.AddDays(-7);
            if (string.Equals(parameter.Name, "To", StringComparison.OrdinalIgnoreCase))
                return DateTime.UtcNow;
            return DateTime.UtcNow;
        }

        if (parameter.DefaultValue is not null)
            return parameter.DefaultValue;

        return parameter.Type switch
        {
            ReportParameterType.Bool => false,
            ReportParameterType.Int => 0,
            _ => null
        };
    }
}
