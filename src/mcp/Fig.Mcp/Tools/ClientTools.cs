using System.ComponentModel;
using Fig.Mcp.ApiClient;
using ModelContextProtocol.Server;
using Newtonsoft.Json;

namespace Fig.Mcp.Tools;

[McpServerToolType]
public class ClientTools
{
    [McpServerTool, Description("List all registered Fig clients with their complete setting definitions. " +
        "Returns client names, descriptions, instances, setting types, current values, defaults, validation rules, " +
        "categories, groups, and custom actions. Use this to get a full picture of all configuration across all services. " +
        "For a lightweight summary of just client names and descriptions, use get_client_descriptions instead.")]
    public static async Task<string> ListClients(
        IFigApiClient apiClient,
        CancellationToken cancellationToken)
    {
        var clients = await apiClient.GetClientsAsync(cancellationToken);
        return JsonConvert.SerializeObject(clients, Formatting.Indented);
    }

    [McpServerTool, Description("Get a lightweight list of all registered client names and descriptions. " +
        "Use this when you only need to know what clients exist without loading their full setting definitions. " +
        "Much faster than list_clients for large deployments.")]
    public static async Task<string> GetClientDescriptions(
        IFigApiClient apiClient,
        CancellationToken cancellationToken)
    {
        var descriptions = await apiClient.GetClientDescriptionsAsync(cancellationToken);
        return JsonConvert.SerializeObject(descriptions, Formatting.Indented);
    }
}
