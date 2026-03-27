using System.ComponentModel;
using Fig.Mcp.ApiClient;
using ModelContextProtocol.Server;
using Newtonsoft.Json;

namespace Fig.Mcp.Tools;

[McpServerToolType]
public class EventTools
{
    [McpServerTool, Description("Query Fig event logs within a time range. Events capture all significant system activity " +
        "including setting value changes, client registrations, user logins, webhook triggers, import/export operations, " +
        "lookup table modifications, scheduling changes, and administrative actions. There are 47 distinct event types " +
        "covering the full lifecycle of configuration management. Each event includes a timestamp, event type, user or " +
        "client identity, and contextual details. Use this to audit who changed what and when, investigate incidents, " +
        "or review recent system activity.")]
    public static async Task<string> GetEvents(
        IFigApiClient apiClient,
        [Description("Start of the time range to query (UTC). Example: 2024-01-01T00:00:00Z")] DateTime startTime,
        [Description("End of the time range to query (UTC). Example: 2024-01-31T23:59:59Z")] DateTime endTime,
        CancellationToken cancellationToken)
    {
        var events = await apiClient.GetEventsAsync(startTime, endTime, cancellationToken);
        return JsonConvert.SerializeObject(events, Formatting.Indented);
    }

    [McpServerTool, Description("Get the total count of event log entries in the Fig system. " +
        "Use this to gauge the volume of activity before querying the full event log with get_events.")]
    public static async Task<string> GetEventCount(
        IFigApiClient apiClient,
        CancellationToken cancellationToken)
    {
        var count = await apiClient.GetEventCountAsync(cancellationToken);
        return JsonConvert.SerializeObject(count, Formatting.Indented);
    }

    [McpServerTool, Description("Get the chronological timeline of all setting changes for a specific client. " +
        "Returns an ordered sequence of events showing every modification made to the client's configuration over time, " +
        "including who made each change and when. Use this to understand the full change history of a particular " +
        "service's configuration or to investigate when a specific change was introduced.")]
    public static async Task<string> GetClientTimeline(
        IFigApiClient apiClient,
        [Description("The exact name of the Fig client to retrieve the timeline for. Use get_client_descriptions to find available client names.")] string clientName,
        CancellationToken cancellationToken)
    {
        var timeline = await apiClient.GetClientTimelineAsync(clientName, cancellationToken);
        return JsonConvert.SerializeObject(timeline, Formatting.Indented);
    }
}
