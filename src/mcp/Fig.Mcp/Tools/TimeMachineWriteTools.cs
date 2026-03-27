using System.ComponentModel;
using Fig.Contracts.CheckPoint;
using Fig.Mcp.ApiClient;
using ModelContextProtocol.Server;

namespace Fig.Mcp.Tools;

[McpServerToolType]
public class TimeMachineWriteTools
{
    [McpServerTool, Description("Restore configuration to a previous checkpoint state. " +
        "⚠️ WARNING: This REPLACES the current configuration of ALL clients with the state captured in the checkpoint. " +
        "All setting values, definitions, and client registrations will be reverted to match the checkpoint. " +
        "Changes made since the checkpoint was created will be lost. " +
        "A new checkpoint is created before applying so you can undo this operation if needed. " +
        "Use list_check_points to find available checkpoint IDs and review their contents with get_check_point_data first.")]
    public static async Task<string> ApplyCheckPoint(
        IFigApiClient apiClient,
        [Description("The GUID identifier of the checkpoint to restore. Use list_check_points to find available checkpoint IDs.")] string checkPointId,
        CancellationToken cancellationToken)
    {
        await apiClient.ApplyCheckPointAsync(Guid.Parse(checkPointId), cancellationToken);
        return $"Checkpoint '{checkPointId}' applied successfully. Configuration has been restored to the checkpoint state.";
    }

    [McpServerTool, Description("Add or update a descriptive note on a checkpoint. " +
        "Notes help identify the purpose or context of a checkpoint when reviewing the timeline later.")]
    public static async Task<string> UpdateCheckPointNote(
        IFigApiClient apiClient,
        [Description("The GUID identifier of the checkpoint to annotate. Use list_check_points to find available checkpoint IDs.")] string checkPointId,
        [Description("The note text to attach to the checkpoint. This replaces any existing note.")] string note,
        CancellationToken cancellationToken)
    {
        var update = new CheckPointUpdateDataContract(note);
        await apiClient.UpdateCheckPointNoteAsync(Guid.Parse(checkPointId), update, cancellationToken);
        return $"Note updated on checkpoint '{checkPointId}'.";
    }
}
