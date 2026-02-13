using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Fig.Client.Abstractions.Validation;

namespace Fig.Client.Abstractions.Attributes;

/// <summary>
/// This attribute can be used to apply validation sql server connection strings.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ValidateSqlServerConnectionStringAttribute : Attribute, IValidatableAttribute, IDisplayScriptProvider
{
    private readonly bool _includeInHealthCheck;

    public ValidateSqlServerConnectionStringAttribute(bool includeInHealthCheck = true)
    {
        _includeInHealthCheck = includeInHealthCheck;
    }

    /// <summary>
    /// Set to true when this attribute is applied to a data grid setting.
    /// </summary>
    public bool UsedInDataGrid { get; set; }

    /// <summary>
    /// The data grid column name that contains the SQL Server connection string.
    /// Defaults to "Values" for single-column data grids.
    /// </summary>
    public string DataGridFieldName { get; set; } = "Values";

    public Type[] ApplyToTypes => [typeof(string)];

    public (bool, string) IsValid(object? value)
    {
        if (!_includeInHealthCheck)
            return (true, "Not validated");

        if (UsedInDataGrid)
            return ValidateDataGrid(value);

        var message = $"{value} is not a valid SQL Server connection string";
        
        if (value == null || value is not string stringValue)
            return (false, message);

        if (string.IsNullOrWhiteSpace(stringValue))
            return (false, message);

        return ValidateSingleConnectionString(stringValue);
    }

    private (bool, string) ValidateSingleConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return (false, $"{connectionString} is not a valid SQL Server connection string");

        var normalizedConnectionString = connectionString!;

        try
        {
            var connectionParams = ParseConnectionString(normalizedConnectionString);
            
            // Check for required components
            if (!HasDataSource(connectionParams))
                return (false, "Connection string must contain a valid Data Source (Server)");

            if (!HasDatabase(connectionParams))
                return (false, "Connection string must contain either Initial Catalog (Database) or AttachDBFilename");

            return (true, "Valid");
        }
        catch (Exception ex)
        {
            return (false, $"Invalid SQL Server connection string: {ex.Message}");
        }
    }

    private (bool, string) ValidateDataGrid(object? value)
    {
        if (value is null)
            return (false, "Data grid value cannot be null");

        if (value is string || value is not IEnumerable rows)
            return (false, "Data grid value must be an enumerable collection of rows");

        var fieldName = string.IsNullOrWhiteSpace(DataGridFieldName) ? "Values" : DataGridFieldName;
        var rowIndex = 0;
        foreach (var row in rows)
        {
            object? fieldValue;
            if (IsDirectRowValue(row))
            {
                fieldValue = row;
            }
            else
            {
                if (!TryGetFieldValue(row, fieldName, out fieldValue))
                    return (false, $"Row {rowIndex} does not contain field '{fieldName}'");
            }

            var connectionString = ExtractStringValue(fieldValue);
            var (isValid, message) = ValidateSingleConnectionString(connectionString);
            if (!isValid)
                return (false, $"Row {rowIndex} ({fieldName}): {message}");

            rowIndex++;
        }

        return (true, "Valid");
    }

    private static bool IsDirectRowValue(object? row)
    {
        if (row is null)
            return true;

        var rowType = row.GetType();
        if (rowType == typeof(string))
            return true;

        return rowType.IsPrimitive || rowType.IsValueType;
    }

    private static bool TryGetFieldValue(object? row, string fieldName, out object? fieldValue)
    {
        fieldValue = null;

        if (row is null)
            return false;

        if (row is IDictionary dictionary)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                if (entry.Key is string key && string.Equals(key, fieldName, StringComparison.OrdinalIgnoreCase))
                {
                    fieldValue = entry.Value;
                    return true;
                }
            }

            return false;
        }

        var property = row.GetType().GetProperty(
            fieldName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        if (property == null)
            return false;

        fieldValue = property.GetValue(row);
        return true;
    }

    private static string? ExtractStringValue(object? value)
    {
        if (value is null)
            return null;

        if (value is string stringValue)
            return stringValue;

        var readOnlyValueProperty = value.GetType().GetProperty("ReadOnlyValue");
        if (readOnlyValueProperty != null)
            return Convert.ToString(readOnlyValueProperty.GetValue(value));

        return Convert.ToString(value);
    }

    private static Dictionary<string, string> ParseConnectionString(string connectionString)
    {
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        // Split by semicolon, but handle quoted values that might contain semicolons
        var parts = SplitConnectionString(connectionString);
        
        foreach (var part in parts)
        {
            var trimmedPart = part.Trim();
            if (string.IsNullOrEmpty(trimmedPart)) continue;
            
            var equalIndex = trimmedPart.IndexOf('=');
            if (equalIndex <= 0) continue;
            
            var key = trimmedPart.Substring(0, equalIndex).Trim();
            var value = trimmedPart.Substring(equalIndex + 1).Trim();
            
            // Remove quotes if present
            if (value.StartsWith("\"") && value.EndsWith("\"") && value.Length > 1)
                value = value.Substring(1, value.Length - 2);
            else if (value.StartsWith("'") && value.EndsWith("'") && value.Length > 1)
                value = value.Substring(1, value.Length - 2);
            
            parameters[key] = value;
        }
        
        return parameters;
    }

    private static string[] SplitConnectionString(string connectionString)
    {
        var parts = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;
        var quoteChar = '\0';
        
        for (int i = 0; i < connectionString.Length; i++)
        {
            var c = connectionString[i];
            
            if (!inQuotes && (c == '"' || c == '\''))
            {
                inQuotes = true;
                quoteChar = c;
                current.Append(c);
            }
            else if (inQuotes && c == quoteChar)
            {
                inQuotes = false;
                current.Append(c);
            }
            else if (!inQuotes && c == ';')
            {
                parts.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        
        if (current.Length > 0)
            parts.Add(current.ToString());
        
        return parts.ToArray();
    }

    private static bool HasDataSource(Dictionary<string, string> parameters)
    {
        var dataSourceKeys = new[] { "server", "data source", "addr", "address", "network address" };
        
        foreach (var key in dataSourceKeys)
        {
            if (parameters.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                return true;
        }
        
        return false;
    }

    private static bool HasDatabase(Dictionary<string, string> parameters)
    {
        var databaseKeys = new[] { "initial catalog", "database" };
        var attachDbKeys = new[] { "attachdbfilename" };
        
        foreach (var key in databaseKeys)
        {
            if (parameters.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                return true;
        }
        
        foreach (var key in attachDbKeys)
        {
            if (parameters.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                return true;
        }
        
        return false;
    }

    public string GetScript(string propertyName)
    {
        var scriptPropertyName = NormalizeScriptPropertyName(propertyName);
        var escapedPropertyName = propertyName.Replace("\\", "\\\\").Replace("'", "\\'");

        if (UsedInDataGrid)
            return GetDataGridScript(scriptPropertyName, propertyName);

        var script = $@"
var connectionString = {scriptPropertyName}.Value;
if (!connectionString || connectionString.trim() === '') {{
    {scriptPropertyName}.IsValid = false;
    {scriptPropertyName}.ValidationExplanation = '{escapedPropertyName} cannot be empty';
}} else {{
    var hasDataSource = /(?:server|data source|addr|address|network address)\s*=/i.test(connectionString);
    var hasDatabase = /(?:initial catalog|database)\s*=/i.test(connectionString) || 
                     /attachdbfilename\s*=/i.test(connectionString);
    
    if (!hasDataSource) {{
        {scriptPropertyName}.IsValid = false;
        {scriptPropertyName}.ValidationExplanation = '{escapedPropertyName} must contain a valid Data Source (Server)';
    }} else if (!hasDatabase) {{
        {scriptPropertyName}.IsValid = false;
        {scriptPropertyName}.ValidationExplanation = '{escapedPropertyName} must contain either Initial Catalog (Database) or AttachDBFilename';
    }} else {{
        {scriptPropertyName}.IsValid = true;
        {scriptPropertyName}.ValidationExplanation = '';
    }}
}}";

        return script;
    }

    private string GetDataGridScript(string scriptPropertyName, string propertyName)
    {
        var fieldName = string.IsNullOrWhiteSpace(DataGridFieldName) ? "Values" : DataGridFieldName;
        var escapedFieldName = fieldName.Replace("\\", "\\\\").Replace("'", "\\'");

        var script = $@"
var rows = {scriptPropertyName}.Value;
var validationErrors = {scriptPropertyName}.ValidationErrors;
var hasValidationError = false;

if (!rows || !Array.isArray(rows)) {{
    {scriptPropertyName}.IsValid = false;
    {scriptPropertyName}.ValidationExplanation = '{propertyName} must be a valid data grid value';
}} else {{
    for (var rowIndex = 0; rowIndex < rows.length; rowIndex++) {{
        var row = rows[rowIndex] || {{}};
        var rawValue = row['{escapedFieldName}'];
        var connectionString = rawValue === undefined || rawValue === null ? '' : String(rawValue);

        var rowMessage = null;
        if (!connectionString || connectionString.trim() === '') {{
            rowMessage = '{escapedFieldName} cannot be empty';
        }} else {{
            var hasDataSource = /(?:server|data source|addr|address|network address)\s*=/i.test(connectionString);
            var hasDatabase = /(?:initial catalog|database)\s*=/i.test(connectionString) ||
                             /attachdbfilename\s*=/i.test(connectionString);

            if (!hasDataSource) {{
                rowMessage = '{escapedFieldName} must contain a valid Data Source (Server)';
            }} else if (!hasDatabase) {{
                rowMessage = '{escapedFieldName} must contain either Initial Catalog (Database) or AttachDBFilename';
            }}
        }}

        if (validationErrors && validationErrors[rowIndex]) {{
            validationErrors[rowIndex]['{escapedFieldName}'] = rowMessage;
        }}

        if (rowMessage) {{
            hasValidationError = true;
        }}
    }}

    {scriptPropertyName}.IsValid = !hasValidationError;
    {scriptPropertyName}.ValidationExplanation = hasValidationError
        ? 'One or more rows contain invalid SQL Server connection strings'
        : '';
}}";

        return script;
    }

    private static string NormalizeScriptPropertyName(string propertyName)
    {
        return propertyName.Replace("->", ".");
    }
}
