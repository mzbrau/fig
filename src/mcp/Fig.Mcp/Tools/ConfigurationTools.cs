using System.ComponentModel;
using Fig.Contracts.Configuration;
using Fig.Mcp.ApiClient;
using ModelContextProtocol.Server;
using Newtonsoft.Json;

namespace Fig.Mcp.Tools;

[McpServerToolType]
public class ConfigurationTools
{
    [McpServerTool, Description("Get the current Fig API configuration. " +
        "Returns system-wide settings including: AllowNewRegistrations, AllowUpdatedRegistrations, " +
        "AllowFileImports, AllowOfflineSettings, AllowClientOverrides, ClientOverridesRegex, " +
        "WebApplicationBaseAddress, UseAzureKeyVault, AzureKeyVaultName, PollIntervalOverride, " +
        "AllowDisplayScripts, EnableTimeMachine, TimelineDurationDays, cleanup retention days " +
        "(TimeMachineCleanupDays, EventLogsCleanupDays, ApiStatusCleanupDays, SettingHistoryCleanupDays), " +
        "and Fig Assistant settings (EnableFigAssistant, FigAssistantEndpoint, FigAssistantModel; access token is masked).")]
    public static async Task<string> GetConfiguration(
        IFigApiClient apiClient,
        CancellationToken cancellationToken)
    {
        var config = await apiClient.GetConfigurationAsync(cancellationToken);
        return JsonConvert.SerializeObject(config, Formatting.Indented);
    }

    [McpServerTool, Description("Update the Fig API configuration. " +
        "⚠️ WARNING: These settings affect the entire Fig system and all connected clients. " +
        "Key settings include: " +
        "AllowNewRegistrations — whether new clients can register; " +
        "AllowUpdatedRegistrations — whether clients can update their setting definitions; " +
        "AllowFileImports — whether file-based imports are permitted; " +
        "AllowOfflineSettings — whether clients can use cached offline settings; " +
        "AllowClientOverrides — whether instance-level overrides are allowed; " +
        "ClientOverridesRegex — regex controlling which clients can use overrides; " +
        "EnableTimeMachine — whether configuration checkpoints are created; " +
        "PollIntervalOverride — override the default client poll interval (seconds); " +
        "Cleanup retention days control how long historical data is kept. " +
        "Use get_configuration first to see current values, then modify and submit the full configuration object.")]
    public static async Task<string> UpdateConfiguration(
        IFigApiClient apiClient,
        [Description("The full JSON configuration object. Must match the FigConfigurationDataContract structure. Use get_configuration to get the current values as a template.")] string configJson,
        CancellationToken cancellationToken)
    {
        var config = JsonConvert.DeserializeObject<FigConfigurationDataContract>(configJson)
            ?? throw new ArgumentException("Failed to deserialize configuration JSON. Ensure the format matches FigConfigurationDataContract.");

        await apiClient.UpdateConfigurationAsync(config, cancellationToken);
        return "Fig API configuration updated successfully.";
    }
}
