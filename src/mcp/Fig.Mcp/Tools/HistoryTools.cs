using System.ComponentModel;
using Fig.Mcp.ApiClient;
using ModelContextProtocol.Server;
using Newtonsoft.Json;

namespace Fig.Mcp.Tools;

[McpServerToolType]
public class HistoryTools
{
    [McpServerTool, Description("Get the value change history for a specific setting on a client. " +
        "Returns a chronological list of all previous values the setting has held, including who made each change, " +
        "when it was changed, and the old and new values. Essential for audit trails, understanding configuration " +
        "drift, and answering 'who changed this setting and when?' questions. " +
        "If the client uses multiple instances, provide the instance name to filter to a specific deployment.")]
    public static async Task<string> GetSettingHistory(
        IFigApiClient apiClient,
        [Description("The exact name of the Fig client that owns the setting. Use get_client_descriptions to find available client names.")] string clientName,
        [Description("The exact name of the setting to retrieve history for. Use list_clients to find available setting names for a client.")] string settingName,
        [Description("Optional instance name to filter history for a specific client instance. Omit or pass null for clients that do not use multiple instances.")] string? instance,
        CancellationToken cancellationToken)
    {
        var history = await apiClient.GetSettingHistoryAsync(clientName, settingName, instance, cancellationToken);
        return JsonConvert.SerializeObject(history, Formatting.Indented);
    }

    [McpServerTool, Description("Get the last-changed timestamps for all clients and their settings. " +
        "Returns raw JSON showing when each setting across all clients was most recently modified. " +
        "Use this to quickly identify recently modified settings, detect unexpected changes, " +
        "or find stale configurations that haven't been updated in a long time.")]
    public static async Task<string> GetLastChanged(
        IFigApiClient apiClient,
        CancellationToken cancellationToken)
    {
        var lastChanged = await apiClient.GetLastChangedAsync(cancellationToken);
        return lastChanged;
    }
}
