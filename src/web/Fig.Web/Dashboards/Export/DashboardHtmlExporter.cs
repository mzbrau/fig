using System.Net;
using System.Text;
using Fig.Contracts.Dashboards;
using Fig.Web.Dashboards.Components;
using Fig.Web.Dashboards.Components.Contracts;
using Fig.Web.Dashboards.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fig.Web.Dashboards.Export;

public class DashboardHtmlExporter
{
    public string Export(
        DashboardDataContract dashboard,
        IReadOnlyDictionary<string, DashboardComponentResult> results)
    {
        ArgumentNullException.ThrowIfNull(dashboard);
        ArgumentNullException.ThrowIfNull(results);

        var definition = dashboard.Definition ?? new DashboardDefinitionDataContract();
        var componentsById = definition.Components.ToDictionary(c => c.Id, StringComparer.OrdinalIgnoreCase);
        var layout = definition.Layout.OrderBy(c => c.Y).ThenBy(c => c.X).ToList();
        var chartScripts = new StringBuilder();
        var chartIndex = 0;

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"utf-8\" />");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
        sb.AppendLine($"<title>{Html(dashboard.Name)} — Fig Dashboard</title>");
        sb.AppendLine("<script src=\"https://cdn.jsdelivr.net/npm/chart.js@4.4.1/dist/chart.umd.min.js\"></script>");
        sb.AppendLine("<style>");
        sb.AppendLine(Css);
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<header class=\"header\">");
        sb.AppendLine($"<h1>{Html(dashboard.Name)}</h1>");
        if (!string.IsNullOrWhiteSpace(dashboard.Description))
            sb.AppendLine($"<p class=\"subtitle\">{Html(dashboard.Description)}</p>");
        sb.AppendLine($"<p class=\"meta\">Exported {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
        sb.AppendLine("</header>");
        sb.AppendLine("<div class=\"grid\">");

        foreach (var cell in layout)
        {
            componentsById.TryGetValue(cell.Id, out var component);
            results.TryGetValue(cell.Id, out var result);
            var width = Math.Clamp(cell.Width <= 0 ? 4 : cell.Width, 1, 12);
            var height = Math.Max(cell.Height <= 0 ? 2 : cell.Height, 1);
            var col = Math.Clamp(cell.X, 0, 11) + 1;
            var row = Math.Max(cell.Y, 0) + 1;

            sb.AppendLine(
                $"<section class=\"cell\" style=\"grid-column:{col} / span {width}; grid-row:{row} / span {height};\">");

            if (component is null)
            {
                sb.AppendLine("<div class=\"error\">Missing component</div>");
            }
            else
            {
                var title = component.Config?["title"]?.ToString()
                            ?? component.Config?["Title"]?.ToString();
                if (!string.IsNullOrWhiteSpace(title))
                    sb.AppendLine($"<div class=\"title\">{Html(title)}</div>");

                if (result is null)
                    sb.AppendLine("<div class=\"empty\">No data</div>");
                else if (!result.Success)
                    sb.AppendLine($"<div class=\"error\">{Html(result.Error ?? "Component error")}</div>");
                else
                    RenderComponent(sb, chartScripts, ref chartIndex, component, result.Data);
            }

            sb.AppendLine("</section>");
        }

        sb.AppendLine("</div>");
        if (chartScripts.Length > 0)
        {
            sb.AppendLine("<script>");
            sb.Append(chartScripts);
            sb.AppendLine("</script>");
        }

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }

    private static void RenderComponent(
        StringBuilder sb,
        StringBuilder chartScripts,
        ref int chartIndex,
        DashboardComponentInstanceDataContract component,
        object? data)
    {
        switch ((component.Type ?? string.Empty).ToLowerInvariant())
        {
            case "kpi":
                RenderKpi(sb, DashboardComponentDataBinder.ToKpi(data));
                break;
            case "text":
                RenderText(sb, DashboardComponentDataBinder.ToText(data));
                break;
            case "badge":
                RenderBadge(sb, DashboardComponentDataBinder.ToBadge(data));
                break;
            case "table":
                RenderTable(sb, DashboardComponentDataBinder.ToTable(data, component.Config));
                break;
            case "list":
                RenderList(sb, DashboardComponentDataBinder.ToList(data));
                break;
            case "keyvalue":
                RenderKeyValue(sb, DashboardComponentDataBinder.ToKeyValue(data, component.Config));
                break;
            case "bar":
                RenderChart(sb, chartScripts, ref chartIndex, "bar",
                    DashboardComponentDataBinder.ToChartPoints(data),
                    DashboardComponentDataBinder.ReadLegendPositionCss(component.Config));
                break;
            case "donut":
                RenderChart(sb, chartScripts, ref chartIndex, "doughnut",
                    DashboardComponentDataBinder.ToChartPoints(data),
                    DashboardComponentDataBinder.ReadLegendPositionCss(component.Config));
                break;
            default:
                sb.AppendLine($"<div class=\"error\">Unknown type '{Html(component.Type)}'</div>");
                break;
        }
    }

    private static void RenderKpi(StringBuilder sb, DashboardKpiInput input)
    {
        var variant = string.IsNullOrWhiteSpace(input.Variant) ? "" : $" kpi--{Html(input.Variant)}";
        sb.AppendLine($"<div class=\"kpi{variant}\">");
        if (!string.IsNullOrWhiteSpace(input.Label))
            sb.AppendLine($"<div class=\"kpi__label\">{Html(input.Label)}</div>");
        sb.AppendLine($"<div class=\"kpi__value\">{Html(FormatValue(input.Value))}</div>");
        if (input.Trend is not null)
            sb.AppendLine($"<div class=\"kpi__trend\">{Html(FormatValue(input.Trend))}</div>");
        sb.AppendLine("</div>");
    }

    private static void RenderText(StringBuilder sb, DashboardTextInput input)
    {
        var variant = string.IsNullOrWhiteSpace(input.Variant) ? "body" : input.Variant!;
        sb.AppendLine($"<p class=\"text text--{Html(variant)}\">{Html(input.Text)}</p>");
    }

    private static void RenderBadge(StringBuilder sb, DashboardBadgeInput input)
    {
        var variant = string.IsNullOrWhiteSpace(input.Variant) ? "normal" : input.Variant!;
        sb.AppendLine($"<span class=\"badge badge--{Html(variant)}\">{Html(input.Text)}</span>");
    }

    private static void RenderTable(StringBuilder sb, DashboardTableInput input)
    {
        if (input.Columns.Count == 0 || input.Rows.Count == 0)
        {
            sb.AppendLine("<div class=\"empty\">No rows</div>");
            return;
        }

        sb.AppendLine("<table class=\"table\"><thead><tr>");
        foreach (var col in input.Columns)
            sb.AppendLine($"<th>{Html(col.Header ?? col.Property)}</th>");
        sb.AppendLine("</tr></thead><tbody>");
        foreach (var row in input.Rows)
        {
            sb.AppendLine("<tr>");
            foreach (var col in input.Columns)
            {
                row.TryGetValue(col.Property, out var value);
                sb.AppendLine($"<td>{Html(FormatValue(value))}</td>");
            }

            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody></table>");
    }

    private static void RenderList(StringBuilder sb, DashboardListInput input)
    {
        if (input.Items.Count == 0)
        {
            sb.AppendLine("<div class=\"empty\">No items</div>");
            return;
        }

        sb.AppendLine("<ul class=\"list\">");
        foreach (var item in input.Items)
        {
            sb.AppendLine("<li>");
            sb.AppendLine($"<div>{Html(item.Text)}</div>");
            if (!string.IsNullOrWhiteSpace(item.Secondary))
                sb.AppendLine($"<div class=\"secondary\">{Html(item.Secondary)}</div>");
            sb.AppendLine("</li>");
        }

        sb.AppendLine("</ul>");
    }

    private static void RenderKeyValue(StringBuilder sb, DashboardKeyValueInput input)
    {
        if (input.Items.Count == 0)
        {
            sb.AppendLine("<div class=\"empty\">No items</div>");
            return;
        }

        sb.AppendLine("<div class=\"kv-card\">");
        if (!string.IsNullOrWhiteSpace(input.StatusIcon))
        {
            var color = string.IsNullOrWhiteSpace(input.StatusColor) ? "#6c757d" : input.StatusColor!;
            sb.AppendLine(
                $"<div class=\"kv__status\" style=\"background-color:{Html(color)};\" title=\"{Html(input.StatusIcon)}\">{Html(input.StatusIcon)}</div>");
        }

        sb.AppendLine("<dl class=\"kv\">");
        foreach (var item in input.Items)
        {
            sb.AppendLine("<div class=\"kv__row\">");
            sb.AppendLine($"<dt>{Html(item.Key)}</dt>");
            sb.AppendLine($"<dd>{Html(FormatValue(item.Value))}</dd>");
            sb.AppendLine("</div>");
        }

        sb.AppendLine("</dl>");
        sb.AppendLine("</div>");
    }

    private static void RenderChart(
        StringBuilder sb,
        StringBuilder chartScripts,
        ref int chartIndex,
        string chartType,
        IReadOnlyList<DashboardChartPoint> points,
        string legendPosition = "right")
    {
        chartIndex++;
        var canvasId = $"chart_{chartIndex}";
        sb.AppendLine($"<canvas id=\"{canvasId}\" height=\"180\"></canvas>");

        var labels = JsonConvert.SerializeObject(points.Select(p => p.Label ?? string.Empty).ToList());
        var values = JsonConvert.SerializeObject(points.Select(p => p.Value).ToList());
        var hidden = string.Equals(legendPosition, "hidden", StringComparison.OrdinalIgnoreCase);
        var position = string.Equals(legendPosition, "bottom", StringComparison.OrdinalIgnoreCase)
            ? "bottom"
            : "right";
        var legendJson = hidden
            ? "{ display: false }"
            : $"{{ position: '{position}', labels: {{ color: '#ddd' }} }}";
        chartScripts.AppendLine($@"
(() => {{
  const ctx = document.getElementById('{canvasId}');
  if (!ctx || typeof Chart === 'undefined') return;
  new Chart(ctx, {{
    type: '{chartType}',
    data: {{
      labels: {labels},
      datasets: [{{
        data: {values},
        backgroundColor: ['#5bc0de','#5cb85c','#f0ad4e','#d9534f','#9b59b6','#3498db','#1abc9c','#e67e22']
      }}]
    }},
    options: {{
      responsive: true,
      plugins: {{ legend: {legendJson} }},
      scales: {(chartType == "bar" ? "{ x: { ticks: { color: '#bbb' }, grid: { color: 'rgba(255,255,255,0.08)' } }, y: { ticks: { color: '#bbb' }, grid: { color: 'rgba(255,255,255,0.08)' } } }" : "undefined")}
    }}
  }});
}})();");
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            string s => s,
            JValue jv => jv.ToString(Formatting.None),
            JToken jt => jt.ToString(Formatting.None),
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
            DateTimeOffset dto => dto.ToString("yyyy-MM-dd HH:mm:ss"),
            _ => Convert.ToString(value) ?? string.Empty
        };
    }

    private static string Html(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    private const string Css = """
        :root { color-scheme: dark; }
        * { box-sizing: border-box; }
        body {
          margin: 0;
          font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
          background: #121212;
          color: #e8e8e8;
          padding: 1.25rem;
        }
        .header { margin-bottom: 1.25rem; }
        .header h1 { margin: 0 0 0.35rem; font-size: 1.6rem; }
        .subtitle { margin: 0; opacity: 0.8; }
        .meta { margin: 0.4rem 0 0; font-size: 0.8rem; opacity: 0.55; }
        .grid {
          display: grid;
          grid-template-columns: repeat(12, minmax(0, 1fr));
          grid-auto-rows: minmax(4.5rem, auto);
          gap: 0.75rem;
        }
        .cell {
          background: rgba(255,255,255,0.03);
          border: 1px solid rgba(255,255,255,0.08);
          border-radius: 0.5rem;
          padding: 0.75rem;
          overflow: auto;
          min-width: 0;
        }
        .title { font-size: 0.85rem; font-weight: 600; opacity: 0.85; margin-bottom: 0.5rem; }
        .empty, .error { opacity: 0.75; }
        .error { color: #f0ad4e; }
        .kpi { display: flex; flex-direction: column; gap: 0.25rem; }
        .kpi__label { opacity: 0.75; font-size: 0.85rem; }
        .kpi__value { font-size: 2rem; font-weight: 700; }
        .kpi__trend { font-size: 0.85rem; opacity: 0.8; }
        .kpi--success .kpi__value { color: #5cb85c; }
        .kpi--warning .kpi__value { color: #f0ad4e; }
        .kpi--danger .kpi__value { color: #d9534f; }
        .kpi--info .kpi__value { color: #5bc0de; }
        .text--heading { font-size: 1.25rem; font-weight: 600; margin: 0; }
        .text--body { margin: 0; }
        .text--muted { margin: 0; opacity: 0.65; }
        .badge {
          display: inline-block;
          padding: 0.2rem 0.55rem;
          border-radius: 0.35rem;
          font-size: 0.8rem;
          font-weight: 600;
          background: rgba(255,255,255,0.08);
        }
        .badge--info { background: rgba(91,192,222,0.25); color: #5bc0de; }
        .badge--success { background: rgba(92,184,92,0.25); color: #5cb85c; }
        .badge--warning { background: rgba(240,173,78,0.25); color: #f0ad4e; }
        .badge--danger { background: rgba(217,83,79,0.25); color: #d9534f; }
        .badge--muted { opacity: 0.7; }
        .table { width: 100%; border-collapse: collapse; font-size: 0.85rem; }
        .table th, .table td {
          text-align: left;
          padding: 0.35rem 0.4rem;
          border-bottom: 1px solid rgba(255,255,255,0.08);
          overflow: hidden;
          text-overflow: ellipsis;
          white-space: nowrap;
          max-width: 12rem;
        }
        .table th { opacity: 0.8; }
        .list { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 0.35rem; }
        .list li { padding: 0.35rem 0.5rem; border-radius: 0.35rem; background: rgba(255,255,255,0.03); }
        .secondary { font-size: 0.8rem; opacity: 0.7; }
        .kv-card { position: relative; padding-right: 2.75rem; }
        .kv__status {
          position: absolute; top: 0; right: 0;
          min-width: 2.25rem; height: 2.25rem; padding: 0 0.4rem;
          border-radius: 999px; display: flex; align-items: center; justify-content: center;
          font-size: 0.75rem; font-weight: 700; color: #fff;
        }
        .kv { margin: 0; display: flex; flex-direction: column; gap: 0.35rem; }
        .kv__row { display: grid; grid-template-columns: minmax(0,1fr) minmax(0,1.4fr); gap: 0.75rem; }
        .kv__row dt { margin: 0; opacity: 0.9; font-weight: 700; }
        .kv__row dd { margin: 0; overflow: hidden; text-overflow: ellipsis; }
        @media (max-width: 900px) {
          .grid { grid-template-columns: 1fr; }
          .cell { grid-column: 1 / -1 !important; }
        }
        """;
}
