using System.Text;
using Fig.Web.Models.Setting;
using Fig.Web.Models.Setting.ConfigurationModels.DataGrid;

namespace Fig.Web.Utils;

public static class DataGridCsvHandler
{
    public static string? ConvertToCsv(DataGridSettingConfigurationModel setting)
    {
        if (setting.Value == null || !setting.Value.Any() || setting.DataGridConfiguration?.Columns == null ||
            !setting.DataGridConfiguration.Columns.Any())
        {
            return null;
        }

        var sb = new StringBuilder();
        var columns = setting.DataGridConfiguration.Columns;

        // Headers
        sb.AppendLine(string.Join(",", columns.Select(c => FormatCsvField(c.Name))));

        // Data Rows
        foreach (var row in setting.Value)
        {
            var rowValues = new List<string>();
            foreach (var column in columns)
            {
                // Ensure the column exists in the row before trying to access it.
                // If a column from configuration doesn't exist in a particular row dictionary, treat as null.
                var cellValue = row.TryGetValue(column.Name, out var valueModel) ? valueModel.ReadOnlyValue : null;
                rowValues.Add(FormatCsvField(cellValue));
            }

            sb.AppendLine(string.Join(",", rowValues));
        }

        return sb.ToString();
    }

    private static string FormatCsvField(object? field)
    {
        if (field == null) return "\"\"";

        string valueString;
        if (field is IEnumerable<string> list)
        {
            valueString = string.Join(",", list);
        }
        else
        {
            valueString = field.ToString() ?? "";
        }

        valueString = valueString.Replace("\"", "\"\"");
        return $"\"{valueString}\"";
    }

    // Returns list of strings, where quoted fields have quotes removed and escaped quotes unescaped.
    // Unquoted fields are returned as-is. No trimming is done by this function.
    public static List<string> ParseCsvLine(string? line)
    {
        var fields = new List<string>();
        if (string.IsNullOrEmpty(line)) 
            return fields;
        
        var currentField = new StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
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

    public class CsvImportResult
    {
        public List<Dictionary<string, IDataGridValueModel>> Rows { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }

    public static CsvImportResult ParseCsvToRows(
        string csvContent,
        List<DataGridColumn> configuredColumns,
        Func<Type, object?, DataGridColumn, ISetting, IDataGridValueModel> createValueModel,
        ISetting parentSetting)
    {
        var result = new CsvImportResult();
        var lines = csvContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        if (!lines.Any() || string.IsNullOrWhiteSpace(lines.First()))
        {
            result.Errors.Add("CSV file is empty or header row is missing.");
            return result;
        }

        var headerLine = lines.First();
        var headerValidationError = ValidateCsvHeader(headerLine, configuredColumns, out var csvHeaders);
        if (headerValidationError != null)
        {
            result.Errors.Add(headerValidationError);
            return result;
        }

        var dataLines = lines.Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
        for (int i = 0; i < dataLines.Count; i++)
        {
            var rowString = dataLines[i];
            var rowResult = ParseCsvRow(rowString, i, configuredColumns, createValueModel, parentSetting);
            if (rowResult.Errors.Any())
                result.Errors.AddRange(rowResult.Errors);
            else
                result.Rows.Add(rowResult.Row);
        }

        return result;
    }

    private static string? ValidateCsvHeader(string headerLine, List<DataGridColumn> configuredColumns,
        out List<string> csvHeaders)
    {
        csvHeaders = ParseCsvLine(headerLine).Select(h => h.Trim()).ToList();
        if (csvHeaders.Count != configuredColumns.Count)
            return
                $"Header column count ({csvHeaders.Count}) does not match grid configuration ({configuredColumns.Count}).";
        for (int i = 0; i < configuredColumns.Count; i++)
        {
            if (!string.Equals(csvHeaders[i], configuredColumns[i].Name, StringComparison.OrdinalIgnoreCase))
                return
                    $"Header mismatch: Expected '{configuredColumns[i].Name}' but found '{csvHeaders[i]}' at column index {i}.";
        }

        return null;
    }

    private static RowParseResult ParseCsvRow(
        string rowString,
        int rowIndex,
        List<DataGridColumn> configuredColumns,
        Func<Type, object?, DataGridColumn, ISetting, IDataGridValueModel> createValueModel,
        ISetting parentSetting)
    {
        var result = new RowParseResult();
        var cellValues = ParseCsvLine(rowString).Select(cv => cv.Trim()).ToList();
        if (cellValues.Count != configuredColumns.Count)
        {
            result.Errors.Add(
                $"Row {rowIndex + 1}: Expected {configuredColumns.Count} columns, found {cellValues.Count}.");
            return result;
        }

        for (int j = 0; j < configuredColumns.Count; j++)
        {
            var columnConfig = configuredColumns[j];
            var cellValueString = cellValues[j];
            var (parsedValue, parseSuccess, error) = TryParseCellValue(cellValueString, columnConfig, rowIndex);
            if (parseSuccess)
            {
                var modelValue = createValueModel(columnConfig.Type, parsedValue, columnConfig, parentSetting);
                result.Row[columnConfig.Name] = modelValue;
            }
            else
            {
                if (!string.IsNullOrEmpty(error))
                    result.Errors.Add(error);
            }
        }

        // Only add row if no errors for any cell
        if (result.Errors.Any())
            result.Row.Clear();
        return result;
    }

    private static (object? parsedValue, bool parseSuccess, string? error) TryParseCellValue(string cellValueString,
        DataGridColumn columnConfig, int rowIndex)
    {
        try
        {
            if (columnConfig.IsReadOnly)
                return (null, true, null);
            
            if (string.IsNullOrWhiteSpace(cellValueString) && 
                columnConfig.Type != typeof(string) &&
                columnConfig.Type != typeof(List<string>))
                return (null, true, null);
            
            if (columnConfig.Type == typeof(string))
                return (cellValueString, true, null);
            
            if (columnConfig.Type == typeof(int) || columnConfig.Type == typeof(int?))
            {
                if (int.TryParse(cellValueString, out var intVal)) 
                    return (intVal, true, null);
                
                return (null, false,
                    $"Row {rowIndex + 1}, Col '{columnConfig.Name}': Invalid integer '{cellValueString}'.");
            }

            if (columnConfig.Type == typeof(long) || columnConfig.Type == typeof(long?))
            {
                if (long.TryParse(cellValueString, out var longVal)) 
                    return (longVal, true, null);
                
                return (null, false,
                    $"Row {rowIndex + 1}, Col '{columnConfig.Name}': Invalid long '{cellValueString}'.");
            }

            if (columnConfig.Type == typeof(double) || columnConfig.Type == typeof(double?))
            {
                if (double.TryParse(cellValueString, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var doubleVal))
                    return (doubleVal, true, null);
                
                return (null, false,
                    $"Row {rowIndex + 1}, Col '{columnConfig.Name}': Invalid double '{cellValueString}'.");
            }

            if (columnConfig.Type == typeof(bool) || columnConfig.Type == typeof(bool?))
            {
                if (bool.TryParse(cellValueString, out var boolVal)) 
                    return (boolVal, true, null);
                
                if (cellValueString.Trim() == "1") 
                    return (true, true, null);
                
                if (cellValueString.Trim() == "0") 
                    return (false, true, null);
                
                return (null, false,
                    $"Row {rowIndex + 1}, Col '{columnConfig.Name}': Invalid boolean '{cellValueString}'.");
            }

            if (columnConfig.Type == typeof(DateTime) || columnConfig.Type == typeof(DateTime?))
            {
                if (DateTime.TryParse(cellValueString, out var dateVal)) 
                    return (dateVal, true, null);
                
                return (null, false,
                    $"Row {rowIndex + 1}, Col '{columnConfig.Name}': Invalid DateTime '{cellValueString}'.");
            }

            if (columnConfig.Type == typeof(TimeSpan) || columnConfig.Type == typeof(TimeSpan?))
            {
                if (TimeSpan.TryParse(cellValueString, out var tsVal)) 
                    return (tsVal, true, null);
                
                return (null, false,
                    $"Row {rowIndex + 1}, Col '{columnConfig.Name}': Invalid TimeSpan '{cellValueString}'.");
            }

            if (columnConfig.Type == typeof(List<string>))
            {
                if (!string.IsNullOrEmpty(cellValueString))
                    return (cellValueString.Split(',').Select(s => s.Trim()).ToList(), true, null);
                
                return (new List<string>(), true, null);
            }

            return (null, false,
                $"Row {rowIndex + 1}, Col '{columnConfig.Name}': Unsupported data type '{columnConfig.Type.FullName}'.");
        }
        catch (Exception ex)
        {
            return (null, false,
                $"Row {rowIndex + 1}, Col '{columnConfig.Name}': Error parsing '{cellValueString}'. Details: {ex.Message}");
        }
    }
    
    private class RowParseResult
    {
        public Dictionary<string, IDataGridValueModel> Row { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }
}