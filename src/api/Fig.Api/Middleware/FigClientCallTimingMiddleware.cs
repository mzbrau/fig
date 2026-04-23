using System.Diagnostics;

namespace Fig.Api.Middleware;

/// <summary>
/// Records the timestamp at which a client-facing request arrived at the pipeline
/// (before model binding or action filters). Downstream components — in particular
/// <see cref="Fig.Api.Attributes.LogFigClientCallAttribute"/> — read this value to
/// derive how long was spent in model binding / resource / authorization filters
/// before the action method ran.
/// </summary>
public class FigClientCallTimingMiddleware
{
    internal const string RequestArrivedAtKey = "FigRequestArrivedAt";

    private readonly RequestDelegate _next;

    public FigClientCallTimingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task Invoke(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Stamp timing for all client-facing paths (GET, POST, PUT, etc.) that the
        // LogFigClientCallAttribute covers, so pre-action latency is always available.
        if (IsClientFacingPath(path))
        {
            context.Items[RequestArrivedAtKey] = Stopwatch.GetTimestamp();
        }

        return _next(context);
    }

    private static bool IsClientFacingPath(string path)
    {
        return path.StartsWith("/clients", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/customactions", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/lookuptables", StringComparison.OrdinalIgnoreCase);
    }
}
