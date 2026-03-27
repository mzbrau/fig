using System.ComponentModel;
using Fig.Contracts.LookupTable;
using Fig.Mcp.ApiClient;
using ModelContextProtocol.Server;
using Newtonsoft.Json;

namespace Fig.Mcp.Tools;

[McpServerToolType]
public class LookupTableWriteTools
{
    [McpServerTool, Description("Create a new lookup table with key-value pairs. " +
        "Lookup tables provide shared reference datasets that settings can reference for dropdown selections or validation. " +
        "Once created, clients can bind settings to this table to constrain their allowed values.")]
    public static async Task<string> CreateLookupTable(
        IFigApiClient apiClient,
        [Description("A descriptive name for the lookup table.")] string name,
        [Description("A JSON object of key-value pairs representing the lookup data. Example: {\"key1\":\"value1\",\"key2\":\"value2\"}")] string lookupDataJson,
        CancellationToken cancellationToken)
    {
        var lookupData = JsonConvert.DeserializeObject<Dictionary<string, string?>>(lookupDataJson)
            ?? throw new ArgumentException("Failed to deserialize lookup data JSON. Ensure the format is a valid JSON object of key-value pairs.");

        var table = new LookupTableDataContract(null, name, lookupData, false);
        await apiClient.CreateLookupTableAsync(table, cancellationToken);
        return $"Lookup table '{name}' created successfully.";
    }

    [McpServerTool, Description("Update an existing lookup table's name and/or key-value data. " +
        "This replaces the entire lookup table content with the provided data.")]
    public static async Task<string> UpdateLookupTable(
        IFigApiClient apiClient,
        [Description("The GUID identifier of the lookup table to update. Use list_lookup_tables to find table IDs.")] string id,
        [Description("The new name for the lookup table.")] string name,
        [Description("A JSON object of key-value pairs representing the updated lookup data. Example: {\"key1\":\"value1\",\"key2\":\"value2\"}")] string lookupDataJson,
        CancellationToken cancellationToken)
    {
        var lookupData = JsonConvert.DeserializeObject<Dictionary<string, string?>>(lookupDataJson)
            ?? throw new ArgumentException("Failed to deserialize lookup data JSON. Ensure the format is a valid JSON object of key-value pairs.");

        var tableId = Guid.Parse(id);
        var table = new LookupTableDataContract(null, name, lookupData, false);
        await apiClient.UpdateLookupTableAsync(tableId, table, cancellationToken);
        return $"Lookup table '{name}' updated successfully.";
    }

    [McpServerTool, Description("Delete a lookup table. " +
        "WARNING: Any client settings that reference this lookup table will lose their dropdown options " +
        "and validation constraints. Verify no active settings depend on this table before deleting.")]
    public static async Task<string> DeleteLookupTable(
        IFigApiClient apiClient,
        [Description("The GUID identifier of the lookup table to delete. Use list_lookup_tables to find table IDs.")] string id,
        CancellationToken cancellationToken)
    {
        await apiClient.DeleteLookupTableAsync(Guid.Parse(id), cancellationToken);
        return $"Lookup table '{id}' deleted successfully.";
    }
}
