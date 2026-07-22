using Fig.Api.Attributes;
using Fig.Api.Reports;
using Fig.Contracts.Authentication;
using Fig.Contracts.Reports;
using Microsoft.AspNetCore.Mvc;

namespace Fig.Api.Controllers;

[ApiController]
[Route("reports")]
public class ReportsController : ControllerBase
{
    private readonly IReportExecutionService _reportExecutionService;

    public ReportsController(IReportExecutionService reportExecutionService)
    {
        _reportExecutionService = reportExecutionService;
    }

    [Authorize(Role.Administrator)]
    [HttpGet]
    public IActionResult GetReports()
    {
        var reports = _reportExecutionService.GetAvailableReports();
        return Ok(reports);
    }

    [Authorize(Role.Administrator)]
    [HttpPost("{reportId}")]
    public async Task<IActionResult> ExecuteReport(
        [FromRoute] string reportId,
        [FromBody] ReportExecutionRequestDataContract request,
        CancellationToken cancellationToken)
    {
        var (html, contentType) = await _reportExecutionService.ExecuteAsync(reportId, request, cancellationToken);
        return Content(html, contentType);
    }
}
