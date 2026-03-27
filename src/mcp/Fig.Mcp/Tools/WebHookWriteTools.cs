using System.ComponentModel;
using Fig.Contracts.WebHook;
using Fig.Mcp.ApiClient;
using ModelContextProtocol.Server;
using Newtonsoft.Json;

namespace Fig.Mcp.Tools;

[McpServerToolType]
public class WebHookWriteTools
{
    [McpServerTool, Description("Create a new webhook trigger that fires when matching Fig events occur. " +
        "The webhook will send HTTP notifications to the associated webhook client endpoint. " +
        "Use list_web_hook_clients to find available client IDs to associate with this webhook.")]
    public static async Task<string> CreateWebHook(
        IFigApiClient apiClient,
        [Description("The GUID of the webhook client endpoint that will receive notifications. Use list_web_hook_clients to find available IDs.")] string clientId,
        [Description("The event type that triggers this webhook. Valid values: ClientStatusChanged, SettingValueChanged, NewClientRegistration, UpdatedClientRegistration, MinRunSessions, HealthStatusChanged, SecurityEvent.")] string webHookType,
        [Description("A regex pattern to match client names. Only events from clients matching this pattern will trigger the webhook. Use '.*' to match all clients.")] string clientNameRegex,
        [Description("Optional regex pattern to match setting names. Only setting-related events matching this pattern will trigger the webhook. Null matches all settings.")] string? settingNameRegex,
        [Description("Minimum number of active run sessions required before the webhook fires. Use 0 for no minimum.")] int minSessions,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<WebHookType>(webHookType, ignoreCase: true, out var type))
            return $"Invalid webhook type '{webHookType}'. Valid values: {string.Join(", ", Enum.GetNames<WebHookType>())}";
        var webHook = new WebHookDataContract(null, Guid.Parse(clientId), type, clientNameRegex, settingNameRegex, minSessions);
        await apiClient.CreateWebHookAsync(webHook, cancellationToken);
        return $"Webhook created successfully for event type '{webHookType}'.";
    }

    [McpServerTool, Description("Update an existing webhook trigger's configuration including its event type, filters, and target client.")]
    public static async Task<string> UpdateWebHook(
        IFigApiClient apiClient,
        [Description("The GUID identifier of the webhook to update. Use list_web_hooks to find webhook IDs.")] string id,
        [Description("The GUID of the webhook client endpoint that will receive notifications.")] string clientId,
        [Description("The event type that triggers this webhook. Valid values: ClientStatusChanged, SettingValueChanged, NewClientRegistration, UpdatedClientRegistration, MinRunSessions, HealthStatusChanged, SecurityEvent.")] string webHookType,
        [Description("A regex pattern to match client names. Only events from clients matching this pattern will trigger the webhook.")] string clientNameRegex,
        [Description("Optional regex pattern to match setting names. Null matches all settings.")] string? settingNameRegex,
        [Description("Minimum number of active run sessions required before the webhook fires.")] int minSessions,
        CancellationToken cancellationToken)
    {
        var webhookId = Guid.Parse(id);
        if (!Enum.TryParse<WebHookType>(webHookType, ignoreCase: true, out var type))
            return $"Invalid webhook type '{webHookType}'. Valid values: {string.Join(", ", Enum.GetNames<WebHookType>())}";
        var webHook = new WebHookDataContract(null, Guid.Parse(clientId), type, clientNameRegex, settingNameRegex, minSessions);
        await apiClient.UpdateWebHookAsync(webhookId, webHook, cancellationToken);
        return $"Webhook '{id}' updated successfully.";
    }

    [McpServerTool, Description("Delete a webhook trigger. The associated webhook client endpoint will no longer receive notifications for this trigger.")]
    public static async Task<string> DeleteWebHook(
        IFigApiClient apiClient,
        [Description("The GUID identifier of the webhook to delete. Use list_web_hooks to find webhook IDs.")] string id,
        CancellationToken cancellationToken)
    {
        await apiClient.DeleteWebHookAsync(Guid.Parse(id), cancellationToken);
        return $"Webhook '{id}' deleted successfully.";
    }

    [McpServerTool, Description("Register a new webhook client endpoint that can receive webhook notifications. " +
        "Webhook clients are HTTP endpoints that Fig sends event payloads to when associated webhooks fire. " +
        "After creating a webhook client, create webhook triggers that reference its ID.")]
    public static async Task<string> CreateWebHookClient(
        IFigApiClient apiClient,
        [Description("A descriptive name for the webhook client endpoint.")] string name,
        [Description("The base URI of the endpoint that will receive webhook payloads. Example: https://example.com/webhooks")] string baseUri,
        [Description("Optional shared secret for HMAC payload verification. The receiving endpoint can use this to verify requests originate from Fig.")] string? secret,
        CancellationToken cancellationToken)
    {
        var client = new WebHookClientDataContract(null, name, new Uri(baseUri), secret);
        await apiClient.CreateWebHookClientAsync(client, cancellationToken);
        return $"Webhook client '{name}' created successfully.";
    }

    [McpServerTool, Description("Update an existing webhook client endpoint's name, URI, or secret.")]
    public static async Task<string> UpdateWebHookClient(
        IFigApiClient apiClient,
        [Description("The GUID identifier of the webhook client to update. Use list_web_hook_clients to find IDs.")] string id,
        [Description("The new name for the webhook client endpoint.")] string name,
        [Description("The new base URI of the endpoint. Example: https://example.com/webhooks")] string baseUri,
        [Description("Optional new shared secret for payload verification. Pass null to remove the secret.")] string? secret,
        CancellationToken cancellationToken)
    {
        var whClientId = Guid.Parse(id);
        var client = new WebHookClientDataContract(null, name, new Uri(baseUri), secret);
        await apiClient.UpdateWebHookClientAsync(whClientId, client, cancellationToken);
        return $"Webhook client '{name}' updated successfully.";
    }

    [McpServerTool, Description("Delete a webhook client endpoint. " +
        "WARNING: Any webhooks associated with this client will no longer be able to send notifications. " +
        "Remove or reassign associated webhooks before deleting the client endpoint.")]
    public static async Task<string> DeleteWebHookClient(
        IFigApiClient apiClient,
        [Description("The GUID identifier of the webhook client to delete. Use list_web_hook_clients to find IDs.")] string id,
        CancellationToken cancellationToken)
    {
        await apiClient.DeleteWebHookClientAsync(Guid.Parse(id), cancellationToken);
        return $"Webhook client '{id}' deleted successfully.";
    }

    [McpServerTool, Description("Test connectivity to a webhook client endpoint by sending a test payload. " +
        "Use this to verify the endpoint is reachable and correctly configured before creating webhook triggers.")]
    public static async Task<string> TestWebHookClient(
        IFigApiClient apiClient,
        [Description("The GUID identifier of the webhook client to test. Use list_web_hook_clients to find IDs.")] string id,
        CancellationToken cancellationToken)
    {
        await apiClient.TestWebHookClientAsync(Guid.Parse(id), cancellationToken);
        return $"Test payload sent successfully to webhook client '{id}'.";
    }
}
