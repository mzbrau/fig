using System.ComponentModel;
using Fig.Contracts.Status;
using Fig.Mcp.ApiClient;
using ModelContextProtocol.Server;
using Newtonsoft.Json;

namespace Fig.Mcp.Tools;

[McpServerToolType]
public class SessionTools
{
    [McpServerTool, Description("Get all active client run sessions showing which service instances are currently connected to Fig. " +
        "Returns operational health data for each running client including: connection status, last seen timestamp, " +
        "application version, .NET runtime version, memory usage, polling interval, IP address, hostname, " +
        "and whether the client has pending configuration changes. " +
        "Use this as an operational health dashboard to identify disconnected clients, high-memory instances, " +
        "outdated application versions, or clients that haven't polled recently.")]
    public static async Task<string> GetRunSessions(
        IFigApiClient apiClient,
        CancellationToken cancellationToken)
    {
        var sessions = await apiClient.GetRunSessionsAsync(cancellationToken);
        return JsonConvert.SerializeObject(sessions, Formatting.Indented);
    }

    [McpServerTool, Description("Get custom status properties for connected client run sessions. " +
        "Returns a lightweight payload with client name, instance, run session id, last seen, and developer-defined " +
        "custom properties (timestamps, booleans, strings, numbers, TimeSpans, etc.) without full session diagnostics. " +
        "Optionally filter by client name and instance.")]
    public static async Task<string> GetCustomStatusProperties(
        IFigApiClient apiClient,
        [Description("Optional client name filter")] string? clientName,
        [Description("Optional instance filter (requires clientName)")] string? instance,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(instance) && string.IsNullOrWhiteSpace(clientName))
            throw new ArgumentException("Instance filter requires clientName.", nameof(instance));

        IEnumerable<CustomStatusSessionPropertiesDataContract> properties;
        if (string.IsNullOrWhiteSpace(clientName))
            properties = await apiClient.GetCustomStatusPropertiesAsync(cancellationToken);
        else
            properties = await apiClient.GetCustomStatusPropertiesAsync(clientName, instance, cancellationToken);

        return JsonConvert.SerializeObject(properties, Formatting.Indented);
    }
}
