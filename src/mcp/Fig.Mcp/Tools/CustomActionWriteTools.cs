using System.ComponentModel;
using Fig.Contracts.CustomActions;
using Fig.Mcp.ApiClient;
using ModelContextProtocol.Server;
using Newtonsoft.Json;

namespace Fig.Mcp.Tools;

[McpServerToolType]
public class CustomActionWriteTools
{
    [McpServerTool, Description("Execute a custom action on a running client instance. " +
        "Custom actions are client-defined operations (e.g., cache clearing, connection pool resets, " +
        "log level changes, health checks) that trigger side effects on the running client application. " +
        "The action must be defined in the client's setting registration. " +
        "Optionally target a specific instance by providing a run session ID. " +
        "Use list_clients to discover available custom actions for each client, " +
        "and get_custom_action_status to poll the execution result.")]
    public static async Task<string> ExecuteCustomAction(
        IFigApiClient apiClient,
        [Description("The exact name of the Fig client that defines the custom action. Use get_client_descriptions to find available client names.")] string clientName,
        [Description("The name of the custom action to execute, as defined in the client's setting registration.")] string customActionName,
        [Description("Optional run session ID (GUID) to target a specific client instance. If omitted, the action is sent to all instances. Use list_run_sessions to find session IDs.")] string? runSessionId,
        CancellationToken cancellationToken)
    {
        Guid? sessionId = runSessionId != null ? Guid.Parse(runSessionId) : null;
        var request = new CustomActionExecutionRequestDataContract(customActionName, sessionId);
        var result = await apiClient.ExecuteCustomActionAsync(clientName, request, cancellationToken);
        return JsonConvert.SerializeObject(result, Formatting.Indented);
    }
}
