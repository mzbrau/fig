using System.Collections;
using System.Text;
using Fig.Common.Constants;
using Fig.Contracts.SettingDefinitions;
using Fig.Datalayer.BusinessEntities.SettingValues;

namespace Fig.Api.Utils;

public class ChangedSetting
{
    public ChangedSetting(string name,
        SettingValueBaseBusinessEntity? originalValue,
        SettingValueBaseBusinessEntity? newValue,
        bool isSecret, 
        DataGridDefinitionDataContract? dataGridDefinition, 
        bool settingIsExternallyManaged)
    {
        Name = name;
        IsSecret = isSecret;
        SettingIsExternallyManaged = settingIsExternallyManaged;
        if (isSecret)
        {
            OriginalValue = new StringSettingBusinessEntity(SecretConstants.SecretPlaceholder);
            NewValue = new StringSettingBusinessEntity(SecretConstants.SecretPlaceholder);
        }
        else if (newValue is DataGridSettingBusinessEntity newDataGridVal && 
                 originalValue is DataGridSettingBusinessEntity originalDataGridVal)
        {
            OriginalValue = GetDataGridValue(originalDataGridVal, dataGridDefinition);
            NewValue = GetDataGridValue(newDataGridVal, dataGridDefinition);
        }
        else
        {
            OriginalValue = originalValue;
            NewValue = newValue;
        }
    }

    public string Name { get; }
    
    public SettingValueBaseBusinessEntity? OriginalValue { get; }
    
    public SettingValueBaseBusinessEntity? NewValue { get; }
    
    public bool IsSecret { get; }
    
    public bool SettingIsExternallyManaged { get; }

    public static StringSettingBusinessEntity GetDataGridValue(DataGridSettingBusinessEntity? value,
        DataGridDefinitionDataContract? dataGridDefinition)
    {
        var list = value?.Value;

        if (list == null || dataGridDefinition == null)
            return new StringSettingBusinessEntity(string.Empty);

        var builder = new StringBuilder();
        foreach (var row in list)
            builder.AppendLine(string.Join(",", GetRowValuesWithSecretRedaction(row, dataGridDefinition)));

        return new StringSettingBusinessEntity(builder.ToString());
    }

    private static IEnumerable<string?> GetRowValuesWithSecretRedaction(Dictionary<string,object?> row, DataGridDefinitionDataContract dataGridDefinition)
    {
        foreach (var column in dataGridDefinition.Columns)
        {
            if (column.IsSecret)
            {
                yield return FormatCsvField(SecretConstants.SecretPlaceholder);
            }
            else if (row.TryGetValue(column.Name, out var value))
            {
                yield return FormatCsvField(value);
            }
            else
            {
                yield return FormatCsvField(null);
            }
        }
    }

    /// <summary>
    /// Formats a data-grid cell for flattened CSV storage.
    /// Collections (including JSON JArray multi-select values) are joined to a single line
    /// and fields are CSV-quoted when they contain commas, quotes, or newlines.
    /// </summary>
    internal static string FormatCsvField(object? field)
    {
        if (field is null)
            return "\"\"";

        string valueString;
        if (field is string s)
        {
            valueString = s;
        }
        else if (field is IEnumerable enumerable)
        {
            var parts = new List<string>();
            foreach (var item in enumerable)
                parts.Add(item?.ToString() ?? string.Empty);
            valueString = string.Join(", ", parts);
        }
        else
        {
            valueString = Convert.ToString(field) ?? string.Empty;
        }

        var needsQuotes = valueString.Contains(',') ||
                          valueString.Contains('"') ||
                          valueString.Contains('\n') ||
                          valueString.Contains('\r') ||
                          valueString.Length == 0;

        if (!needsQuotes)
            return valueString;

        return $"\"{valueString.Replace("\"", "\"\"")}\"";
    }
}
