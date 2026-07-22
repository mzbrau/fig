using System.Collections;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Fig.Api.ExtensionMethods;
using Fig.Contracts.SettingDefinitions;
using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.BusinessEntities.SettingValues;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fig.Api.Reports.Rendering;

public static class ReportDataGridHtml
{
    public const string SecretMask = "******";

    private static readonly JsonSerializerSettings EventValueJsonSettings = new()
    {
        TypeNameHandling = TypeNameHandling.None
    };

    /// <summary>
    /// Matches pretty-printed or compact JSON string arrays embedded in flattened CSV
    /// (e.g. multi-select enum cells written via JArray.ToString()).
    /// </summary>
    private static readonly Regex JsonStringArrayPattern = new(
        @"\[\s*(?:""(?:[^""\\]|\\.)*""\s*,\s*)*""(?:[^""\\]|\\.)*""\s*\]|\[\s*\]",
        RegexOptions.Compiled | RegexOptions.Singleline);

    public static string Build(SettingBusinessEntity setting)
        => Build((setting.Value as DataGridSettingBusinessEntity)?.Value, setting.GetDataGridDefinition());

    public static string Build(
        List<Dictionary<string, object?>>? rows,
        DataGridDefinitionDataContract? definition)
    {
        var columns = ResolveColumns(rows, definition);

        if (columns.Count == 0)
            return "<span class=\"muted\">No rows</span>";

        var sb = new StringBuilder();
        sb.Append("<table class=\"report-table report-table-nested\">");
        sb.Append("<thead><tr>");
        foreach (var column in columns)
            sb.Append("<th>").Append(WebUtility.HtmlEncode(column)).Append("</th>");
        sb.Append("</tr></thead><tbody>");

        if (rows is null || rows.Count == 0)
        {
            sb.Append("<tr><td colspan=\"")
                .Append(columns.Count)
                .Append("\" class=\"muted\">No rows</td></tr>");
        }
        else
        {
            foreach (var row in rows)
            {
                sb.Append("<tr>");
                foreach (var column in columns)
                {
                    sb.Append("<td>")
                        .Append(WebUtility.HtmlEncode(FormatCell(column, row, definition)))
                        .Append("</td>");
                }

                sb.Append("</tr>");
            }
        }

        sb.Append("</tbody></table>");
        return sb.ToString();
    }

    /// <summary>
    /// Renders an event-log OriginalValue/NewValue string as a nested table when possible.
    /// Supports JSON arrays of objects and the flattened CSV format produced by ChangedSetting.GetDataGridValue.
    /// Falls back to HTML-encoded plain text when the value cannot be interpreted as a grid.
    /// </summary>
    public static string BuildFromEventValue(string? value, DataGridDefinitionDataContract? definition)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "<span class=\"muted\">(empty)</span>";

        var trimmed = value.Trim();
        if (TryParseJsonRows(trimmed, out var jsonRows))
            return Build(jsonRows, definition);

        if (definition?.Columns is { Count: > 0 } && TryParseFlattenedRows(trimmed, definition, out var csvRows))
            return Build(csvRows, definition);

        return WebUtility.HtmlEncode(value);
    }

    public static bool TryParseJsonRows(string value, out List<Dictionary<string, object?>>? rows)
    {
        rows = null;
        if (!value.StartsWith('[') || !value.EndsWith(']'))
            return false;

        try
        {
            rows = JsonConvert.DeserializeObject<List<Dictionary<string, object?>>>(value, EventValueJsonSettings);
            return rows is not null;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static bool TryParseFlattenedRows(
        string value,
        DataGridDefinitionDataContract definition,
        out List<Dictionary<string, object?>> rows)
    {
        rows = [];
        var columns = definition.Columns;
        if (columns.Count == 0)
            return false;

        var normalized = NormalizeEmbeddedJsonArrays(value);
        var lines = SplitCsvLines(normalized);

        if (lines.Count == 0)
            return true;

        foreach (var line in lines)
        {
            var cells = ParseCsvLine(line);
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < columns.Count; i++)
            {
                var cell = i < cells.Count ? cells[i] : string.Empty;
                row[columns[i].Name] = cell;
            }

            rows.Add(row);
        }

        return true;
    }

    /// <summary>
    /// Collapses pretty-printed JSON string arrays (historical JArray.ToString() cells)
    /// into a single CSV-quoted joined value so newline splitting does not create phantom rows.
    /// </summary>
    internal static string NormalizeEmbeddedJsonArrays(string value)
    {
        return JsonStringArrayPattern.Replace(value, match =>
        {
            try
            {
                var array = JArray.Parse(match.Value);
                var parts = array.Select(t => t.Type == JTokenType.Null ? string.Empty : t.ToString())
                    .ToList();
                var joined = string.Join(", ", parts);
                return $"\"{joined.Replace("\"", "\"\"")}\"";
            }
            catch (JsonException)
            {
                return match.Value;
            }
        });
    }

    /// <summary>
    /// Splits CSV text into logical rows, ignoring newlines inside quoted fields.
    /// </summary>
    internal static List<string> SplitCsvLines(string value)
    {
        var lines = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (c == '"')
            {
                current.Append(c);
                if (inQuotes && i + 1 < value.Length && value[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (!inQuotes && (c == '\n' || c == '\r'))
            {
                if (c == '\r' && i + 1 < value.Length && value[i + 1] == '\n')
                    i++;

                var line = current.ToString().Trim();
                if (line.Length > 0)
                    lines.Add(line);
                current.Clear();
                continue;
            }

            current.Append(c);
        }

        var last = current.ToString().Trim();
        if (last.Length > 0)
            lines.Add(last);

        return lines;
    }

    /// <summary>
    /// Quote-aware CSV field splitter (compatible with ChangedSetting.FormatCsvField output).
    /// </summary>
    internal static List<string> ParseCsvLine(string? line)
    {
        var fields = new List<string>();
        if (string.IsNullOrEmpty(line))
            return fields;

        var currentField = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        currentField.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    currentField.Append(c);
                }
            }
            else
            {
                if (c == '"' && currentField.Length == 0)
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    fields.Add(currentField.ToString());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }
        }

        fields.Add(currentField.ToString());
        return fields;
    }

    private static IReadOnlyList<string> ResolveColumns(
        List<Dictionary<string, object?>>? rows,
        DataGridDefinitionDataContract? definition)
    {
        if (definition?.Columns is { Count: > 0 })
            return definition.Columns.Select(c => c.Name).ToList();

        if (rows is { Count: > 0 })
            return rows[0].Keys.ToList();

        return [];
    }

    private static string FormatCell(
        string columnName,
        Dictionary<string, object?> row,
        DataGridDefinitionDataContract? definition)
    {
        var column = definition?.Columns.FirstOrDefault(c =>
            string.Equals(c.Name, columnName, StringComparison.OrdinalIgnoreCase));

        if (column?.IsSecret == true)
            return SecretMask;

        if (!row.TryGetValue(columnName, out var value) || value is null)
            return string.Empty;

        if (value is IEnumerable<string> stringList)
            return string.Join(", ", stringList);

        if (value is IEnumerable enumerable and not string)
        {
            var parts = new List<string>();
            foreach (var item in enumerable)
                parts.Add(item?.ToString() ?? string.Empty);
            return string.Join(", ", parts);
        }

        return Convert.ToString(value) ?? string.Empty;
    }
}
