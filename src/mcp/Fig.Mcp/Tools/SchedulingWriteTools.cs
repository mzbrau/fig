using System.ComponentModel;
using Fig.Contracts.Scheduling;
using Fig.Mcp.ApiClient;
using ModelContextProtocol.Server;

namespace Fig.Mcp.Tools;

[McpServerToolType]
public class SchedulingWriteTools
{
    [McpServerTool, Description("Change the scheduled execution time of a deferred setting change. " +
        "Use this to postpone or bring forward a previously scheduled configuration update. " +
        "The deferred change will be applied at the new time instead of the originally scheduled time.")]
    public static async Task<string> RescheduleDeferredChange(
        IFigApiClient apiClient,
        [Description("The GUID identifier of the deferred change to reschedule. Use list_deferred_changes to find available IDs.")] string id,
        [Description("The new UTC date and time when the deferred change should be applied. Example: 2024-02-15T14:30:00Z")] DateTime newExecuteAtUtc,
        CancellationToken cancellationToken)
    {
        var request = new RescheduleDeferredChangeDataContract { NewExecuteAtUtc = newExecuteAtUtc };
        await apiClient.RescheduleChangeAsync(Guid.Parse(id), request, cancellationToken);
        return $"Deferred change '{id}' rescheduled to {newExecuteAtUtc:O}.";
    }

    [McpServerTool, Description("Cancel a scheduled setting change by deleting the deferred change entry. " +
        "The setting modification will not be applied. This cannot be undone — the deferred change must be recreated if needed.")]
    public static async Task<string> DeleteDeferredChange(
        IFigApiClient apiClient,
        [Description("The GUID identifier of the deferred change to cancel. Use list_deferred_changes to find available IDs.")] string id,
        CancellationToken cancellationToken)
    {
        await apiClient.DeleteScheduledChangeAsync(Guid.Parse(id), cancellationToken);
        return $"Deferred change '{id}' has been cancelled and deleted.";
    }
}
