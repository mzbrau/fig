using System.Diagnostics;
using Fig.Api.ExtensionMethods;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Fig.Api.Attributes;

/// <summary>
/// Logs entry, exit, and duration of API endpoints called by the Fig client.
/// Emits at Information level. Emits at Warning level when the request exceeds
/// <see cref="SlowRequestThresholdMs"/> milliseconds.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class LogFigClientCallAttribute : Attribute, IAsyncActionFilter
{
    private const int SlowRequestThresholdMs = 1000;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Fig.Api.ClientCalls");

        var method = request.Method.Sanitize();
        var path = (request.Path.Value ?? string.Empty).Sanitize();
        var requestSize = request.ContentLength;
        var remoteIp = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var clientName = ExtractClientName(context);

        logger.LogInformation(
            "[FigClientCall] {Method} {Path} started for {ClientName}. RequestSize: {RequestSizeBytes} bytes, RemoteIp: {RemoteIp}",
            method, path, clientName, requestSize, remoteIp);

        var sw = Stopwatch.StartNew();
        var executed = await next();
        sw.Stop();

        var statusCode = context.HttpContext.Response.StatusCode;
        var elapsed = sw.ElapsedMilliseconds;

        if (executed.Exception != null)
        {
            logger.LogWarning(
                executed.Exception,
                "[FigClientCall] {Method} {Path} failed for {ClientName} after {ElapsedMs} ms",
                method, path, clientName, elapsed);
        }
        else if (elapsed > SlowRequestThresholdMs)
        {
            logger.LogWarning(
                "[FigClientCall] {Method} {Path} completed for {ClientName} in {ElapsedMs} ms with status {StatusCode} (slow — threshold is {ThresholdMs} ms)",
                method, path, clientName, elapsed, statusCode, SlowRequestThresholdMs);
        }
        else
        {
            logger.LogInformation(
                "[FigClientCall] {Method} {Path} completed for {ClientName} in {ElapsedMs} ms with status {StatusCode}",
                method, path, clientName, elapsed, statusCode);
        }
    }

    /// <summary>
    /// Extracts and sanitizes the client name from route parameters or body DTO properties.
    /// Checks for a <c>clientName</c> route value first, then falls back to inspecting
    /// action argument objects for <c>ClientName</c> or <c>Name</c> properties.
    /// </summary>
    private static string ExtractClientName(ActionExecutingContext context)
    {
        if (context.RouteData.Values.TryGetValue("clientName", out var routeValue) &&
            routeValue is string routeClientName &&
            !string.IsNullOrEmpty(routeClientName))
        {
            return routeClientName.Sanitize();
        }

        foreach (var arg in context.ActionArguments.Values)
        {
            if (arg is null) continue;
            var prop = arg.GetType().GetProperty("ClientName") ?? arg.GetType().GetProperty("Name");
            if (prop?.PropertyType == typeof(string) && prop.GetValue(arg) is string name && !string.IsNullOrEmpty(name))
                return name.Sanitize();
        }

        return "unknown";
    }
}
