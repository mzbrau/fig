using System.ComponentModel;
using Fig.Mcp.ApiClient;
using ModelContextProtocol.Server;
using Newtonsoft.Json;

namespace Fig.Mcp.Tools;

[McpServerToolType]
public class StatusTools
{
    [McpServerTool, Description("Get the Fig API version string and the timestamp of the last settings update. " +
        "Use this to verify which version of the Fig server is running and when configuration was last modified.")]
    public static async Task<string> GetApiVersion(
        IFigApiClient apiClient,
        CancellationToken cancellationToken)
    {
        var version = await apiClient.GetApiVersionAsync(cancellationToken);
        return JsonConvert.SerializeObject(new
        {
            version.ApiVersion,
            version.LastSettingChange
        }, Formatting.Indented);
    }

    [McpServerTool, Description("Get the health status of the Fig API and all its integrated services. " +
        "Returns the operational state of the API server, database connectivity, and any configured external integrations. " +
        "Use this to diagnose connectivity issues or verify the Fig server is fully operational before making changes.")]
    public static async Task<string> GetApiStatus(
        IFigApiClient apiClient,
        CancellationToken cancellationToken)
    {
        var statuses = await apiClient.GetApiStatusAsync(cancellationToken);
        return JsonConvert.SerializeObject(statuses, Formatting.Indented);
    }

    [McpServerTool, Description("Get the list of clients that have pending deferred imports waiting to be applied. " +
        "Deferred imports are configuration imports that have been uploaded but not yet activated, typically requiring " +
        "manual approval or a scheduled activation window. Use this to check if any imports are waiting for processing.")]
    public static async Task<string> GetDeferredImports(
        IFigApiClient apiClient,
        CancellationToken cancellationToken)
    {
        var imports = await apiClient.GetDeferredImportsAsync(cancellationToken);
        return JsonConvert.SerializeObject(imports, Formatting.Indented);
    }

    [McpServerTool, Description("Get the historical record of all client registrations including their setting snapshots at registration time. " +
        "Each entry captures the full setting definitions a client registered with, allowing you to track how a client's " +
        "configuration schema has evolved across deployments. Use this to understand when new settings were introduced, " +
        "when defaults changed, or to compare current vs. original registration state.")]
    public static async Task<string> GetClientRegistrationHistory(
        IFigApiClient apiClient,
        CancellationToken cancellationToken)
    {
        var history = await apiClient.GetClientRegistrationHistoryAsync(cancellationToken);
        return JsonConvert.SerializeObject(history, Formatting.Indented);
    }
}
