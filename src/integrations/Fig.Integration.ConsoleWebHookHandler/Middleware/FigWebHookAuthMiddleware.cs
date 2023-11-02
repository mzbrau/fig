using System.Net;
using Fig.Integration.ConsoleWebHookHandler.Configuration;
using Microsoft.Extensions.Options;

namespace Fig.Integration.ConsoleWebHookHandler.Middleware;

public class FigWebHookAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IOptionsMonitor<Settings> _settings;

    public FigWebHookAuthMiddleware(RequestDelegate next, IOptionsMonitor<Settings> settings)
    {
        _next = next;
        _settings = settings;
    }

    public async Task Invoke(HttpContext context)
    {
        var secret = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
        if (string.IsNullOrWhiteSpace(secret) ||
            !BCrypt.Net.BCrypt.EnhancedVerify(secret, _settings.CurrentValue.HashedSecret))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        }
        else
        {
            await _next(context);
        }
    }
}