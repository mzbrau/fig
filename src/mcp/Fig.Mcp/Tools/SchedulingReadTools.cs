using System.ComponentModel;
using Fig.Mcp.ApiClient;
using ModelContextProtocol.Server;
using Newtonsoft.Json;

namespace Fig.Mcp.Tools;

[McpServerToolType]
public class SchedulingReadTools
{
    [McpServerTool, Description("List all scheduled (deferred) setting changes that are pending application. " +
        "Deferred changes are setting value modifications that have been configured to take effect at a specific " +
        "future date and time rather than immediately. Each entry includes the target client, setting name, " +
        "the new value to be applied, and the scheduled activation time. " +
        "Use this to review upcoming configuration changes, verify scheduled rollouts, " +
        "or identify changes that may impact services at a future time.")]
    public static async Task<string> ListDeferredChanges(
        IFigApiClient apiClient,
        CancellationToken cancellationToken)
    {
        var changes = await apiClient.GetDeferredChangesAsync(cancellationToken);
        return JsonConvert.SerializeObject(changes, Formatting.Indented);
    }
}
