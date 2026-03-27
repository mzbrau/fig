using System.ComponentModel;
using Fig.Mcp.ApiClient;
using ModelContextProtocol.Server;
using Newtonsoft.Json;

namespace Fig.Mcp.Tools;

[McpServerToolType]
public class WebHookReadTools
{
    [McpServerTool, Description("List all configured webhooks with their trigger types, URL match patterns, and associated clients. " +
        "Webhooks are HTTP callbacks fired when specific Fig events occur (e.g., setting changes, client registrations). " +
        "Each webhook defines which event types trigger it and optional filters to scope it to specific clients or settings. " +
        "Use this to understand what external notifications are configured and how Fig integrates with other systems.")]
    public static async Task<string> ListWebHooks(
        IFigApiClient apiClient,
        CancellationToken cancellationToken)
    {
        var webHooks = await apiClient.GetWebHooksAsync(cancellationToken);
        return JsonConvert.SerializeObject(webHooks, Formatting.Indented);
    }

    [McpServerTool, Description("List all webhook client endpoints that receive webhook notifications. " +
        "Webhook clients are the target HTTP endpoints that Fig sends event payloads to when webhooks fire. " +
        "Each webhook client has a base URL and secret for payload verification. " +
        "Use this to see which external systems are registered to receive Fig notifications.")]
    public static async Task<string> ListWebHookClients(
        IFigApiClient apiClient,
        CancellationToken cancellationToken)
    {
        var webHookClients = await apiClient.GetWebHookClientsAsync(cancellationToken);
        return JsonConvert.SerializeObject(webHookClients, Formatting.Indented);
    }
}
