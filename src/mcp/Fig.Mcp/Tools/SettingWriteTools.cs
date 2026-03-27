using System.ComponentModel;
using Fig.Common.NetStandard.Json;
using Fig.Contracts.Settings;
using Fig.Mcp.ApiClient;
using ModelContextProtocol.Server;
using Newtonsoft.Json;

namespace Fig.Mcp.Tools;

[McpServerToolType]
public class SettingWriteTools
{
    [McpServerTool, Description("Update one or more setting values for a client. " +
        "WARNING: Changes take effect immediately (or on the client's next poll cycle) and are audited. " +
        "The settingsJson parameter must be a JSON array of setting objects in the format: " +
        "[{\"Name\":\"settingName\",\"Value\":{\"$type\":\"Fig.Contracts.Settings.StringSettingDataContract\",\"Value\":\"newValue\"}}]. " +
        "Common $type values include StringSettingDataContract, IntSettingDataContract, BoolSettingDataContract, " +
        "LongSettingDataContract, DoubleSettingDataContract, DateTimeSettingDataContract, and JsonSettingDataContract. " +
        "Use list_clients to discover available setting names and their current types before updating.")]
    public static async Task<string> UpdateSettingValues(
        IFigApiClient apiClient,
        [Description("The exact name of the Fig client whose settings should be updated. Use get_client_descriptions to find available client names.")] string clientName,
        [Description("A JSON array of setting objects to update. Each object must have a 'Name' and 'Value' field with the appropriate $type discriminator for polymorphic deserialization.")] string settingsJson,
        [Description("Optional message describing the reason for the change. This is recorded in the audit trail.")] string? changeMessage,
        CancellationToken cancellationToken)
    {
        var settings = JsonConvert.DeserializeObject<IEnumerable<SettingDataContract>>(settingsJson, JsonSettings.FigDefault)
            ?? throw new ArgumentException("Failed to deserialize settings JSON. Ensure the format is a valid JSON array of SettingDataContract objects.");

        await apiClient.UpdateSettingsAsync(clientName, settings, changeMessage, cancellationToken);
        return $"Successfully updated settings for client '{clientName}'.";
    }

    [McpServerTool, Description("Enable or disable live reload for a specific client session. " +
        "When live reload is enabled, the client automatically picks up setting changes without requiring a restart. " +
        "When disabled, the client must be restarted to apply new settings.")]
    public static async Task<string> ToggleLiveReload(
        IFigApiClient apiClient,
        [Description("The run session ID (GUID) of the client instance. Use list_run_sessions to find active session IDs.")] string runSessionId,
        [Description("True to enable live reload, false to disable it.")] bool enabled,
        CancellationToken cancellationToken)
    {
        await apiClient.SetLiveReloadAsync(Guid.Parse(runSessionId), enabled, cancellationToken);
        return $"Live reload {(enabled ? "enabled" : "disabled")} for session '{runSessionId}'.";
    }

    [McpServerTool, Description("Request a running client instance to restart. " +
        "WARNING: This will briefly interrupt the client application while it restarts. " +
        "The client must support restart requests and be currently connected. " +
        "Use this when configuration changes require a full application restart to take effect.")]
    public static async Task<string> RequestClientRestart(
        IFigApiClient apiClient,
        [Description("The run session ID (GUID) of the client instance to restart. Use list_run_sessions to find active session IDs.")] string runSessionId,
        CancellationToken cancellationToken)
    {
        await apiClient.RestartSessionAsync(Guid.Parse(runSessionId), cancellationToken);
        return $"Restart request sent to session '{runSessionId}'.";
    }
}
