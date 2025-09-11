using Fig.Api.Services;

namespace Fig.Api.Middleware;

public class CallerDetailsMiddleware
{
    private readonly RequestDelegate _next;

    public CallerDetailsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context,
        IEventLogFactory eventLogFactory,
        IStatusService statusService,
        ISettingsService settingsService)
    {
        var ipAddress = context.Request.Headers["Fig_IpAddress"].FirstOrDefault() 
                       ?? context.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim()
                       ?? context.Request.Headers["X-Real-IP"].FirstOrDefault()
                       ?? context.Connection.RemoteIpAddress?.ToString();
        var hostname = context.Request.Headers["Fig_Hostname"].FirstOrDefault() 
                      ?? context.Request.Host.Host;
        eventLogFactory.SetRequesterDetails(ipAddress, hostname);
        statusService.SetRequesterDetails(ipAddress, hostname);
        settingsService.SetRequesterDetails(ipAddress, hostname);

        await _next(context);
    }
}