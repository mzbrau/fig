using Fig.Contracts.Reports;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Fig.Api.Reports.Rendering;

public class HtmlReportRenderer : IReportRenderer
{
    private readonly HtmlRenderer _htmlRenderer;

    public HtmlReportRenderer(HtmlRenderer htmlRenderer)
    {
        _htmlRenderer = htmlRenderer;
    }

    public bool CanRender(ReportFormat format) => format == ReportFormat.Html;

    public async Task<string> RenderAsync(ReportRenderContext context, CancellationToken cancellationToken = default)
    {
        return await _htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(ReportDocument.Context)] = context
            });

            var output = await _htmlRenderer.RenderComponentAsync<ReportDocument>(parameters);
            return output.ToHtmlString();
        });
    }
}
