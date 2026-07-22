using Fig.Api.Services;
using Fig.Contracts.Reports;

namespace Fig.Api.Reports;

public interface IReportExecutionService : IAuthenticatedService
{
    IList<ReportDefinitionDataContract> GetAvailableReports();

    Task<(string Html, string ContentType)> ExecuteAsync(
        string reportId,
        ReportExecutionRequestDataContract request,
        CancellationToken cancellationToken = default);
}
