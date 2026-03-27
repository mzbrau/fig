using System.ComponentModel;
using Fig.Mcp.ApiClient;
using ModelContextProtocol.Server;
using Newtonsoft.Json;

namespace Fig.Mcp.Tools;

[McpServerToolType]
public class TimeMachineReadTools
{
    [McpServerTool, Description("List available configuration checkpoints within a time range. " +
        "Checkpoints are automatic snapshots of the entire Fig configuration state taken before significant changes " +
        "such as bulk imports, setting deletions, or schema changes. Each checkpoint records the timestamp, " +
        "trigger event, and a reference ID to retrieve the full snapshot data. " +
        "Use this to find restore points when investigating configuration issues or preparing to roll back changes. " +
        "Retrieve the actual checkpoint data using get_check_point_data with the returned data ID.")]
    public static async Task<string> ListCheckPoints(
        IFigApiClient apiClient,
        [Description("Start of the time range to search for checkpoints (UTC). Example: 2024-01-01T00:00:00Z")] DateTime startTime,
        [Description("End of the time range to search for checkpoints (UTC). Example: 2024-01-31T23:59:59Z")] DateTime endTime,
        CancellationToken cancellationToken)
    {
        var checkPoints = await apiClient.GetCheckPointsAsync(startTime, endTime, cancellationToken);
        return JsonConvert.SerializeObject(checkPoints, Formatting.Indented);
    }

    [McpServerTool, Description("Get the full configuration snapshot data for a specific checkpoint. " +
        "Returns the complete serialized configuration state as it existed at the time the checkpoint was created. " +
        "Use this to inspect the exact settings and values that were in place at a given point in time. " +
        "Get the data ID from the list_check_points tool results.")]
    public static async Task<string> GetCheckPointData(
        IFigApiClient apiClient,
        [Description("The unique identifier (GUID) of the checkpoint data to retrieve. Obtain this from the list_check_points results.")] string dataId,
        CancellationToken cancellationToken)
    {
        var data = await apiClient.GetCheckPointDataAsync(Guid.Parse(dataId), cancellationToken);
        return data;
    }
}
