using System.ComponentModel;
using Fig.Mcp.ApiClient;
using ModelContextProtocol.Server;

namespace Fig.Mcp.Tools;

[McpServerToolType]
public class ClientDeleteTools
{
    [McpServerTool, Description("Permanently delete a client and ALL its settings, history, and configuration. " +
        "⚠️ THIS ACTION IS IRREVERSIBLE. All setting definitions, current values, change history, " +
        "audit logs, and run session data for this client will be permanently destroyed. " +
        "Consider exporting the client's data first using export_all_data. " +
        "Only proceed if you are absolutely certain this client is no longer needed.")]
    public static async Task<string> DeleteClient(
        IFigApiClient apiClient,
        [Description("The exact name of the Fig client to permanently delete. Use get_client_descriptions to verify the client name.")] string clientName,
        CancellationToken cancellationToken)
    {
        await apiClient.DeleteClientAsync(clientName, cancellationToken);
        return $"Client '{clientName}' and all associated data have been permanently deleted.";
    }
}
