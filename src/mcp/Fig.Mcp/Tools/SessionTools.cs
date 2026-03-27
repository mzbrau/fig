using System.ComponentModel;
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
}
