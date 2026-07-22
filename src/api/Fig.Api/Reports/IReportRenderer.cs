using Fig.Contracts.Reports;

namespace Fig.Api.Reports;

public interface IReportRenderer
{
    bool CanRender(ReportFormat format);

    Task<string> RenderAsync(ReportRenderContext context, CancellationToken cancellationToken = default);
}
