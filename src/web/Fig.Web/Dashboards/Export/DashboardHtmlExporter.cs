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
            case "cards":
                RenderCards(sb, DashboardComponentDataBinder.ToCards(data),
                    DashboardComponentDataBinder.ReadCardStyle(component.Config));
                break;
            case "bar":
                RenderChart(sb, chartScripts, ref chartIndex, "bar",
                    DashboardComponentDataBinder.ToChartPoints(data),
                    DashboardComponentDataBinder.ReadLegendPositionCss(component.Config));
                break;
            case "donut":
                RenderChart(sb, chartScripts, ref chartIndex, "doughnut",
                    DashboardComponentDataBinder.ToChartPoints(data),
                    DashboardComponentDataBinder.ReadLegendPositionCss(component.Config),
                    DashboardComponentDataBinder.ReadChartSize(component.Config));
                break;
            default:
                sb.AppendLine($"<div class=\"error\">Unknown type '{Html(component.Type)}'</div>");
                break;
        }
    }

    private static void RenderKpi(StringBuilder sb, DashboardKpiInput input)
    {
        var variant = string.IsNullOrWhiteSpace(input.Variant) ? "" : $" kpi--{Html(input.Variant)}";
        var hasIcon = !string.IsNullOrWhiteSpace(input.Icon);
        var iconClass = hasIcon ? " kpi--has-icon" : "";
        sb.AppendLine($"<div class=\"kpi{variant}{iconClass}\">");
        if (hasIcon)
            sb.AppendLine($"<div class=\"kpi__icon\">{Html(input.Icon)}</div>");
        if (!string.IsNullOrWhiteSpace(input.Label))
            sb.AppendLine($"<div class=\"kpi__label\">{Html(input.Label)}</div>");
        sb.AppendLine($"<div class=\"kpi__value\">{Html(FormatKpiValue(input))}</div>");
        if (!string.IsNullOrWhiteSpace(input.Subtitle))
            sb.AppendLine($"<div class=\"kpi__subtitle\">{Html(input.Subtitle)}</div>");
        else if (input.Trend is not null)
            sb.AppendLine($"<div class=\"kpi__trend\">{Html(FormatValue(input.Trend))}</div>");
        sb.AppendLine("</div>");
    }

    private static string FormatKpiValue(DashboardKpiInput input)
    {
        if (input.Numerator is not null && input.Denominator is not null)
            return $"{FormatValue(input.Numerator)}/{FormatValue(input.Denominator)}";
        return FormatValue(input.Value);
    }

    private static void RenderCards(StringBuilder sb, DashboardCardsInput input, string style)
    {
        if (input.Cards.Count == 0)
        {
            sb.AppendLine("<div class=\"empty\">No cards</div>");
            return;
        }

        var styleClass = string.Equals(style, "extraWide", StringComparison.OrdinalIgnoreCase)
            ? " cards--extraWide"
            : string.Equals(style, "wide", StringComparison.OrdinalIgnoreCase)
                ? " cards--wide"
                : " cards--compact";
        sb.AppendLine($"<div class=\"cards{styleClass}\">");
        foreach (var card in input.Cards)
        {
            var variant = string.IsNullOrWhiteSpace(card.Variant) ? "" : $" card--{Html(card.Variant)}";
            var hasIcon = !string.IsNullOrWhiteSpace(card.Icon);
            var iconClass = hasIcon ? " card--has-icon" : "";
            sb.AppendLine($"<div class=\"card{variant}{iconClass}\">");
            if (hasIcon)
                sb.AppendLine($"<div class=\"card__icon\">{Html(card.Icon)}</div>");
            if (!string.IsNullOrWhiteSpace(card.Title))
                sb.AppendLine($"<div class=\"card__title\">{Html(card.Title)}</div>");
            sb.AppendLine($"<div class=\"card__value\">{Html(FormatValue(card.Value))}</div>");
            if (card.Rows.Count > 0)
            {
                sb.AppendLine("<dl class=\"card__rows\">");
                foreach (var row in card.Rows)
                {
                    sb.AppendLine("<div class=\"card__row\">");
                    sb.AppendLine($"<dt>{Html(row.Key)}</dt>");
                    var rowValue = FormatValue(row.Value);
                    sb.AppendLine($"<dd title=\"{Html(rowValue)}\">{Html(rowValue)}</dd>");
                    sb.AppendLine("</div>");
                }

                sb.AppendLine("</dl>");
            }

            sb.AppendLine("</div>");
        }

        sb.AppendLine("</div>");
    }

    private static void RenderText(StringBuilder sb, DashboardTextInput input)
    {
        var lines = input.Lines;
        if (lines.Count == 0 && !string.IsNullOrWhiteSpace(input.Text))
        {
            var size = (input.Variant ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "heading" => "xl",
                "muted" => "sm",
                _ => "md"
            };
            lines = [new DashboardTextLine { Text = input.Text, Size = size }];
        }

        if (lines.Count == 0)
        {
            sb.AppendLine("<div class=\"text\"></div>");
            return;
        }

        sb.AppendLine("<div class=\"text\">");
        foreach (var line in lines)
        {
            var size = NormalizeTextSize(line.Size);
            var align = NormalizeTextAlign(line.Align);
            var weight = string.Equals(line.Weight, "bold", StringComparison.OrdinalIgnoreCase) ? "bold" : "normal";
            var style = string.IsNullOrWhiteSpace(line.Color) ? string.Empty : $" style=\"color:{Html(line.Color)}\"";
            sb.AppendLine(
                $"<div class=\"text__line text__line--{size} text__line--align-{align} text__line--weight-{weight}\"{style}>{Html(line.Text)}</div>");
        }

        sb.AppendLine("</div>");
    }

    private static string NormalizeTextSize(string? size) =>
        (size ?? "md").Trim().ToLowerInvariant() switch
        {
            "xs" => "xs",
            "sm" => "sm",
            "lg" => "lg",
            "xl" => "xl",
            "xxl" => "xxl",
            _ => "md"
        };

    private static string NormalizeTextAlign(string? align) =>
        (align ?? "left").Trim().ToLowerInvariant() switch
        {
            "center" => "center",
            "right" => "right",
            _ => "left"
        };

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
            var variant = string.IsNullOrWhiteSpace(item.Variant)
                ? "normal"
                : item.Variant.Trim().ToLowerInvariant();
            if (variant is "success" or "warning" or "danger" or "info")
                sb.AppendLine($"<li class=\"list--{Html(variant)}\">");
            else
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

        var hasStatus = !string.IsNullOrWhiteSpace(input.StatusIcon);
        sb.AppendLine(hasStatus ? "<div class=\"kv-card kv-card--has-status\">" : "<div class=\"kv-card\">");
        if (hasStatus)
        {
            var color = string.IsNullOrWhiteSpace(input.StatusColor) ? "#9aa0a6" : input.StatusColor!;
            sb.AppendLine(
                $"<div class=\"kv__status\" style=\"--dash-status-accent:{Html(color)};\" title=\"{Html(input.StatusIcon)}\">{Html(input.StatusIcon)}</div>");
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
        string legendPosition = "right",
        string chartSize = "large")
    {
        chartIndex++;
        var canvasId = $"chart_{chartIndex}";
        var canvasHeight = string.Equals(chartSize, "small", StringComparison.OrdinalIgnoreCase) ? 120 : 180;
        sb.AppendLine($"<canvas id=\"{canvasId}\" height=\"{canvasHeight}\"></canvas>");

        var labels = JsonConvert.SerializeObject(
            points.Select(p => p.Label ?? string.Empty).ToList(),
            ScriptJsonSettings);
        var values = JsonConvert.SerializeObject(
            points.Select(p => p.Value).ToList(),
            ScriptJsonSettings);
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

    private static readonly JsonSerializerSettings ScriptJsonSettings = new()
    {
        StringEscapeHandling = StringEscapeHandling.EscapeHtml
    };

    private const string Css = """
        :root {
          color-scheme: dark;
          --dash-success: #8fd18f;
          --dash-warning: #f5c57a;
          --dash-danger: #e89996;
          --dash-info: #8ed3e8;
          --dash-success-bg: rgba(92, 184, 92, 0.2);
          --dash-warning-bg: rgba(240, 173, 78, 0.2);
          --dash-danger-bg: rgba(217, 83, 79, 0.2);
          --dash-info-bg: rgba(91, 192, 222, 0.2);
          --dash-status-size: 2rem;
          --dash-status-icon: 1.15rem;
          --dash-status-inset: 0.35rem;
          --dash-item-bg: rgba(255, 255, 255, 0.03);
          --dash-item-border: rgba(255, 255, 255, 0.06);
        }
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
        .error { color: var(--dash-warning); }
        .kpi__icon, .card__icon, .kv__status {
          position: absolute;
          top: var(--dash-status-inset); right: var(--dash-status-inset);
          width: var(--dash-status-size); height: var(--dash-status-size);
          border-radius: 50%;
          display: flex; align-items: center; justify-content: center;
          background: rgba(255,255,255,0.12);
          font-size: var(--dash-status-icon);
          line-height: 0;
          overflow: hidden;
          padding: 0; margin: 0;
          box-sizing: border-box;
        }
        .kpi {
          position: relative;
          display: flex;
          flex-direction: column;
          justify-content: center;
          gap: 0.25rem;
        }
        .kpi--has-icon { padding-right: calc(var(--dash-status-size) + var(--dash-status-inset)); }
        .kpi__label { opacity: 0.8; font-size: 0.85rem; }
        .kpi__value { font-size: 2.35rem; font-weight: 700; line-height: 1.1; }
        .kpi__subtitle, .kpi__trend { font-size: 0.85rem; opacity: 0.85; }
        .kpi--success .kpi__value { color: var(--dash-success); }
        .kpi--warning .kpi__value { color: var(--dash-warning); }
        .kpi--danger .kpi__value { color: var(--dash-danger); }
        .kpi--info .kpi__value { color: var(--dash-info); }
        .kpi--success .kpi__icon { color: var(--dash-success); background: var(--dash-success-bg); }
        .kpi--warning .kpi__icon { color: var(--dash-warning); background: var(--dash-warning-bg); }
        .kpi--danger .kpi__icon { color: var(--dash-danger); background: var(--dash-danger-bg); }
        .kpi--info .kpi__icon { color: var(--dash-info); background: var(--dash-info-bg); }
        .cards {
          display: grid;
          grid-template-columns: repeat(auto-fill, minmax(12rem, 1fr));
          gap: 0.5rem;
        }
        .cards--wide {
          grid-template-columns: repeat(auto-fill, minmax(18rem, 1fr));
          gap: 0.75rem;
        }
        .cards--extraWide {
          grid-template-columns: repeat(auto-fill, minmax(24rem, 1fr));
          gap: 0.9rem;
        }
        .card {
          position: relative;
          display: flex;
          flex-direction: column;
          gap: 0.2rem;
          padding: 0.5rem 0.65rem;
          border-radius: 0.35rem;
          background: var(--dash-item-bg);
          border: 1px solid var(--dash-item-border);
          min-width: 0;
        }
        .cards--wide .card {
          gap: 0.35rem;
          padding: 0.75rem 0.9rem;
        }
        .cards--extraWide .card {
          gap: 0.4rem;
          padding: 0.85rem 1rem;
        }
        .card--has-icon { padding-right: calc(var(--dash-status-size) + var(--dash-status-inset)); }
        .cards--wide .card--has-icon { padding-right: calc(var(--dash-status-size) + var(--dash-status-inset) + 0.25rem); }
        .cards--extraWide .card--has-icon { padding-right: calc(var(--dash-status-size) + var(--dash-status-inset) + 0.35rem); }
        .card__title { font-size: 0.85rem; font-weight: 600; opacity: 0.8; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
        .cards--wide .card__title { font-size: 1rem; }
        .cards--extraWide .card__title { font-size: 1.05rem; }
        .card__value { font-size: 1.65rem; font-weight: 700; line-height: 1.15; }
        .cards--wide .card__value { font-size: 2rem; }
        .cards--extraWide .card__value { font-size: 2.15rem; }
        .card__rows { margin: 0.35rem 0 0; display: flex; flex-direction: column; gap: 0.2rem; }
        .cards--wide .card__rows { margin-top: 0.5rem; gap: 0.3rem; }
        .cards--extraWide .card__rows { margin-top: 0.55rem; gap: 0.35rem; }
        .card__row { display: grid; grid-template-columns: minmax(0,0.9fr) minmax(0,1.6fr); gap: 0.4rem; align-items: start; font-size: 0.75rem; }
        .cards--wide .card__row { gap: 0.55rem; font-size: 0.85rem; }
        .cards--extraWide .card__row { grid-template-columns: minmax(0,0.8fr) minmax(0,2fr); gap: 0.65rem; font-size: 0.9rem; }
        .card__row dt { margin: 0; font-weight: 700; opacity: 0.85; }
        .card__row dd {
          margin: 0;
          text-align: right;
          overflow: hidden;
          display: -webkit-box;
          -webkit-box-orient: vertical;
          -webkit-line-clamp: 2;
          line-clamp: 2;
          white-space: normal;
          overflow-wrap: anywhere;
          word-break: break-word;
          opacity: 0.9;
        }
        .card--success .card__value { color: var(--dash-success); }
        .card--warning .card__value { color: var(--dash-warning); }
        .card--danger .card__value { color: var(--dash-danger); }
        .card--info .card__value { color: var(--dash-info); }
        .card--success .card__icon { color: var(--dash-success); background: var(--dash-success-bg); }
        .card--warning .card__icon { color: var(--dash-warning); background: var(--dash-warning-bg); }
        .card--danger .card__icon { color: var(--dash-danger); background: var(--dash-danger-bg); }
        .card--info .card__icon { color: var(--dash-info); background: var(--dash-info-bg); }
        .text { display: flex; flex-direction: column; gap: 0.15rem; }
        .text__line { margin: 0; line-height: 1.25; word-break: break-word; }
        .text__line--xs { font-size: 0.75rem; }
        .text__line--sm { font-size: 0.85rem; opacity: 0.8; }
        .text__line--md { font-size: 1rem; }
        .text__line--lg { font-size: 1.25rem; }
        .text__line--xl { font-size: 1.6rem; font-weight: 600; }
        .text__line--xxl { font-size: 2.4rem; font-weight: 700; letter-spacing: -0.02em; }
        .text__line--align-left { text-align: left; }
        .text__line--align-center { text-align: center; }
        .text__line--align-right { text-align: right; }
        .text__line--weight-normal { font-weight: 400; }
        .text__line--weight-bold { font-weight: 700; }
        .badge {
          display: inline-block;
          padding: 0.2rem 0.55rem;
          border-radius: 0.35rem;
          font-size: 0.8rem;
          font-weight: 600;
          background: rgba(255,255,255,0.08);
        }
        .badge--info { background: var(--dash-info-bg); color: var(--dash-info); }
        .badge--success { background: var(--dash-success-bg); color: var(--dash-success); }
        .badge--warning { background: var(--dash-warning-bg); color: var(--dash-warning); }
        .badge--danger { background: var(--dash-danger-bg); color: var(--dash-danger); }
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
        .list li {
          padding: 0.35rem 0.5rem;
          border-radius: 0.35rem;
          background: var(--dash-item-bg);
          border-left: 3px solid transparent;
        }
        .list li.list--success { border-left-color: var(--dash-success); }
        .list li.list--warning { border-left-color: var(--dash-warning); }
        .list li.list--danger { border-left-color: var(--dash-danger); }
        .list li.list--info { border-left-color: var(--dash-info); }
        .secondary { font-size: 0.8rem; opacity: 0.7; }
        .kv-card { position: relative; }
        .kv-card--has-status { padding-right: calc(var(--dash-status-size) + var(--dash-status-inset)); }
        .kv__status {
          color: var(--dash-status-accent, #9aa0a6);
          background: color-mix(in srgb, var(--dash-status-accent, #9aa0a6) 25%, transparent);
          font-weight: 700;
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
