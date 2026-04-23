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

        // Only pay the cost for client-facing write paths (POST/PUT) that the
        // LogFigClientCallAttribute also covers. Read paths (/settings GET) are
        // very fast and don't need the extra instrumentation.
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
