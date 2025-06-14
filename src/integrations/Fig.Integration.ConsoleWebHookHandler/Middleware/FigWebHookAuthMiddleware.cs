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
        var hashedSecret = _settings.CurrentValue.HashedSecret;
        
        if (string.IsNullOrWhiteSpace(secret) || 
            string.IsNullOrWhiteSpace(hashedSecret) ||
            !BCrypt.Net.BCrypt.EnhancedVerify(secret, hashedSecret))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        }
        else
        {
            await _next(context);
        }
    }
}