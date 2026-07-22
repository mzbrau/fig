using Fig.Contracts.Reports;
using Fig.Web.Models.Reports;

namespace Fig.Web.Facades;

public interface IReportsFacade
{
    IReadOnlyList<ReportDefinitionModel> Reports { get; }

    Task LoadReports();

    Task<string?> GenerateReport(string reportId, Dictionary<string, object?> parameters);
}
