using System;
using System.Collections.Generic;
using System.Text;
using Fig.Client.Validation;

namespace Fig.Client.Attributes;

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

    public Type[] ApplyToTypes => [typeof(string)];

    public (bool, string) IsValid(object? value)
    {
        if (!_includeInHealthCheck)
            return (true, "Not validated");

        var message = $"{value} is not a valid SQL Server connection string";
        
        if (value == null || value is not string stringValue)
            return (false, message);

        if (string.IsNullOrWhiteSpace(stringValue))
            return (false, message);

        try
        {
            var connectionParams = ParseConnectionString(stringValue);
            
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
        var script = $@"
var connectionString = {propertyName}.Value;
if (!connectionString || connectionString.trim() === '') {{
    {propertyName}.IsValid = false;
    {propertyName}.ValidationExplanation = '{propertyName} cannot be empty';
}} else {{
    var hasDataSource = /(?:server|data source|addr|address|network address)\s*=/i.test(connectionString);
    var hasDatabase = /(?:initial catalog|database)\s*=/i.test(connectionString) || 
                     /attachdbfilename\s*=/i.test(connectionString);
    
    if (!hasDataSource) {{
        {propertyName}.IsValid = false;
        {propertyName}.ValidationExplanation = '{propertyName} must contain a valid Data Source (Server)';
    }} else if (!hasDatabase) {{
        {propertyName}.IsValid = false;
        {propertyName}.ValidationExplanation = '{propertyName} must contain either Initial Catalog (Database) or AttachDBFilename';
    }} else {{
        {propertyName}.IsValid = true;
        {propertyName}.ValidationExplanation = '';
    }}
}}";

        return script;
    }
}
