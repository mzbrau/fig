using System.ComponentModel;
using Fig.Mcp.ApiClient;
using ModelContextProtocol.Server;
using Newtonsoft.Json;

namespace Fig.Mcp.Tools;

[McpServerToolType]
public class LookupTableReadTools
{
    [McpServerTool, Description("List all lookup tables with their key-value data. " +
        "Lookup tables are shared reference datasets used across multiple clients for dropdown selections, " +
        "validation lists, and mapping values. Each table contains a name, description, and a set of key-value pairs. " +
        "Settings on any client can reference a lookup table to constrain their allowed values. " +
        "Use this to understand what shared reference data exists and which values are available for configuration.")]
    public static async Task<string> ListLookupTables(
        IFigApiClient apiClient,
        CancellationToken cancellationToken)
    {
        var tables = await apiClient.GetLookupTablesAsync(cancellationToken);
        return JsonConvert.SerializeObject(tables, Formatting.Indented);
    }
}
