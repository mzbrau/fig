using Fig.Api.Reports.Rendering.Components;
using Newtonsoft.Json.Linq;

namespace Fig.Api.Reports;

public static class AiReportDocumentValidator
{
    public const int MaxSections = 30;
    public const int MaxTableRows = 100;
    public const int MaxChartPoints = 24;
    public const int MaxSummaryCards = 12;
    public const int MaxTimelineItems = 50;

    private static readonly HashSet<string> AllowedChartTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "pie", "doughnut", "bar"
    };

    public static AiReportDocument ParseAndValidate(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new AiReportValidationException(
                "AI report document JSON is empty. Expected e.g. " +
                "{\"title\":\"Report\",\"sections\":[{\"type\":\"markdown\",\"content\":\"...\"}]}.");

        JObject root;
        try
        {
            root = JObject.Parse(json);
        }
        catch (Exception ex)
        {
            throw new AiReportValidationException(
                "AI report document JSON is invalid. Expected an object with title and sections array.",
                ex);
        }

        var title = root.Value<string>("title")?.Trim();
        if (string.IsNullOrWhiteSpace(title))
            throw new AiReportValidationException(
                "AI report document requires a non-empty title. Example: {\"title\":\"Report\",\"sections\":[...]}.");

        var description = root.Value<string>("description")?.Trim();
        if (root["sections"] is not JArray sectionsToken)
            throw new AiReportValidationException(
                "AI report document requires a sections array. Example: " +
                "\"sections\":[{\"type\":\"summary\",\"items\":[{\"label\":\"Users\",\"value\":\"3\"}]},{\"type\":\"markdown\",\"content\":\"...\"}].");

        if (sectionsToken.Count == 0)
            throw new AiReportValidationException(
                "AI report document requires at least one section (summary, markdown, table, chart, or timeline).");

        if (sectionsToken.Count > MaxSections)
            throw new AiReportValidationException($"AI report document may have at most {MaxSections} sections.");

        var sections = new List<AiReportSection>();
        foreach (var sectionToken in sectionsToken)
        {
            if (sectionToken is not JObject sectionObj)
                throw new AiReportValidationException("Each section must be a JSON object with a type field.");

            var type = sectionObj.Value<string>("type")?.Trim().ToLowerInvariant();
            sections.Add(type switch
            {
                "summary" => ParseSummary(sectionObj),
                "markdown" => ParseMarkdown(sectionObj),
                "table" => ParseTable(sectionObj),
                "chart" => ParseChart(sectionObj),
                "timeline" => ParseTimeline(sectionObj),
                _ => throw new AiReportValidationException(
                    $"Unknown section type '{type}'. Allowed: summary, markdown, table, chart, timeline.")
            });
        }

        return new AiReportDocument
        {
            Title = title,
            Description = string.IsNullOrWhiteSpace(description) ? null : description,
            Sections = sections
        };
    }

    private static AiReportSummarySection ParseSummary(JObject section)
    {
        var itemsToken = section["items"] as JArray
                         ?? throw new AiReportValidationException(
                             "summary sections require items: [{\"label\":\"...\",\"value\":\"...\",\"subText\":\"optional\"}].");
        if (itemsToken.Count > MaxSummaryCards)
            throw new AiReportValidationException($"summary sections may have at most {MaxSummaryCards} items.");

        var items = new List<SummaryCardItem>();
        foreach (var item in itemsToken.OfType<JObject>())
        {
            var label = item.Value<string>("label")?.Trim() ?? string.Empty;
            var value = item.Value<string>("value")?.Trim() ?? item["value"]?.ToString() ?? string.Empty;
            var subText = item.Value<string>("subText")?.Trim();
            if (string.IsNullOrWhiteSpace(label) && string.IsNullOrWhiteSpace(value))
                continue;
            items.Add(new SummaryCardItem(label, value, string.IsNullOrWhiteSpace(subText) ? null : subText));
        }

        return new AiReportSummarySection { Items = items };
    }

    private static AiReportMarkdownSection ParseMarkdown(JObject section)
    {
        var content = section.Value<string>("content")?.Trim() ?? string.Empty;
        return new AiReportMarkdownSection { Content = content };
    }

    private static AiReportTableSection ParseTable(JObject section)
    {
        var title = section.Value<string>("title")?.Trim();
        var columnsToken = section["columns"] as JArray
                           ?? throw new AiReportValidationException(
                               "table sections require columns: {\"type\":\"table\",\"columns\":[\"A\",\"B\"],\"rows\":[[\"1\",\"2\"]]}.");
        var columns = columnsToken
            .Select(c => c.Type == JTokenType.String ? c.Value<string>()?.Trim() : c.ToString())
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Cast<string>()
            .ToList();
        if (columns.Count == 0)
            throw new AiReportValidationException(
                "table sections require at least one column. Example columns: [\"User\",\"Count\"].");

        var rowsToken = section["rows"] as JArray ?? new JArray();
        if (rowsToken.Count > MaxTableRows)
            throw new AiReportValidationException($"table sections may have at most {MaxTableRows} rows.");

        var rows = new List<IReadOnlyDictionary<string, object?>>();
        foreach (var rowToken in rowsToken)
        {
            var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            if (rowToken is JArray cells)
            {
                for (var i = 0; i < columns.Count; i++)
                {
                    dict[columns[i]] = i < cells.Count ? CellValue(cells[i]) : null;
                }
            }
            else if (rowToken is JObject obj)
            {
                foreach (var column in columns)
                {
                    var match = obj.Properties()
                        .FirstOrDefault(p => string.Equals(p.Name, column, StringComparison.OrdinalIgnoreCase));
                    dict[column] = match is null ? null : CellValue(match.Value);
                }
            }
            else
            {
                continue;
            }

            rows.Add(dict);
        }

        return new AiReportTableSection
        {
            Title = string.IsNullOrWhiteSpace(title) ? null : title,
            Columns = columns,
            Rows = rows
        };
    }

    private static AiReportChartSection ParseChart(JObject section)
    {
        var title = section.Value<string>("title")?.Trim();
        var chartType = section.Value<string>("chartType")?.Trim() ?? "pie";
        if (!AllowedChartTypes.Contains(chartType))
            throw new AiReportValidationException(
                $"chartType '{chartType}' is not supported. Use pie, doughnut, or bar.");

        var datasetLabel = section.Value<string>("datasetLabel")?.Trim();
        if (string.IsNullOrWhiteSpace(datasetLabel))
            datasetLabel = "Value";

        var dataToken = section["data"] as JArray
                        ?? throw new AiReportValidationException(
                            "chart sections require data: [{\"label\":\"Alice\",\"value\":3},{\"label\":\"Bob\",\"value\":1}].");
        if (dataToken.Count > MaxChartPoints)
            throw new AiReportValidationException($"chart sections may have at most {MaxChartPoints} points.");

        var slices = new List<ChartSlice>();
        foreach (var point in dataToken.OfType<JObject>())
        {
            var label = point.Value<string>("label")?.Trim() ?? string.Empty;
            var valueToken = point["value"];
            var value = valueToken?.Type switch
            {
                JTokenType.Integer or JTokenType.Float => valueToken.Value<double>(),
                JTokenType.String when double.TryParse(valueToken.Value<string>(), out var parsed) => parsed,
                _ => 0d
            };
            var color = point.Value<string>("color")?.Trim();
            if (string.IsNullOrWhiteSpace(label))
                continue;
            slices.Add(new ChartSlice(label, value, string.IsNullOrWhiteSpace(color) ? null : color));
        }

        return new AiReportChartSection
        {
            Title = string.IsNullOrWhiteSpace(title) ? null : title,
            ChartType = chartType.ToLowerInvariant(),
            DatasetLabel = datasetLabel,
            Data = slices
        };
    }

    private static AiReportTimelineSection ParseTimeline(JObject section)
    {
        var title = section.Value<string>("title")?.Trim();
        var itemsToken = section["items"] as JArray
                         ?? throw new AiReportValidationException(
                             "timeline sections require items: [{\"title\":\"...\",\"detail\":\"optional\",\"timestampUtc\":\"2026-01-01T00:00:00Z\"}].");
        if (itemsToken.Count > MaxTimelineItems)
            throw new AiReportValidationException($"timeline sections may have at most {MaxTimelineItems} items.");

        var items = new List<TimelineItem>();
        foreach (var item in itemsToken.OfType<JObject>())
        {
            var titleText = item.Value<string>("title")?.Trim() ?? string.Empty;
            var detail = item.Value<string>("detail")?.Trim();
            DateTime timestamp;
            var timestampRaw = item["timestampUtc"]?.ToString() ?? item["timestamp"]?.ToString();
            if (!DateTime.TryParse(timestampRaw, out var parsed))
                timestamp = DateTime.UtcNow;
            else
                timestamp = parsed.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
                    : parsed.ToUniversalTime();

            if (string.IsNullOrWhiteSpace(titleText))
                continue;
            items.Add(new TimelineItem(
                timestamp,
                titleText,
                string.IsNullOrWhiteSpace(detail) ? null : detail));
        }

        return new AiReportTimelineSection
        {
            Title = string.IsNullOrWhiteSpace(title) ? null : title,
            Items = items
        };
    }

    private static object? CellValue(JToken token) =>
        token.Type switch
        {
            JTokenType.Null => null,
            JTokenType.Integer => token.Value<long>(),
            JTokenType.Float => token.Value<double>(),
            JTokenType.Boolean => token.Value<bool>(),
            JTokenType.Date => token.Value<DateTime>(),
            _ => token.ToString()
        };
}

public sealed class AiReportValidationException : Exception
{
    public AiReportValidationException(string message)
        : base(message)
    {
    }

    public AiReportValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
