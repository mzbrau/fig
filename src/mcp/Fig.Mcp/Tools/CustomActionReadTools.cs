using System.ComponentModel;
using Fig.Mcp.ApiClient;
using ModelContextProtocol.Server;
using Newtonsoft.Json;

namespace Fig.Mcp.Tools;

[McpServerToolType]
public class CustomActionReadTools
{
    [McpServerTool, Description("Get the current execution status of a custom action by its execution ID. " +
        "Custom actions are client-defined operations (e.g., cache clears, connection pool resets, log level changes) " +
        "that can be triggered remotely through Fig. This returns whether the action is pending, running, " +
        "completed, or failed, along with any result message from the client. " +
        "Use this to poll the progress of a previously triggered custom action.")]
    public static async Task<string> GetCustomActionStatus(
        IFigApiClient apiClient,
        [Description("The unique execution ID (GUID) of the custom action execution to check. This is returned when a custom action is triggered.")] string executionId,
        CancellationToken cancellationToken)
    {
        var status = await apiClient.GetCustomActionStatusAsync(Guid.Parse(executionId), cancellationToken);
        return JsonConvert.SerializeObject(status, Formatting.Indented);
    }

    [McpServerTool, Description("Get the execution history of a specific custom action on a client. " +
        "Returns a chronological record of all past executions of the custom action, including timestamps, " +
        "who triggered each execution, the outcome (success/failure), and any result messages. " +
        "Use this to audit when and how often a custom action has been used, or to investigate past execution failures.")]
    public static async Task<string> GetCustomActionHistory(
        IFigApiClient apiClient,
        [Description("The exact name of the Fig client that defines the custom action. Use get_client_descriptions to find available client names.")] string clientName,
        [Description("The unique identifier (GUID) of the custom action to retrieve history for. Find this in the client's custom action definitions from list_clients.")] string customActionId,
        CancellationToken cancellationToken)
    {
        var history = await apiClient.GetCustomActionHistoryAsync(clientName, Guid.Parse(customActionId), cancellationToken);
        return JsonConvert.SerializeObject(history, Formatting.Indented);
    }
}
