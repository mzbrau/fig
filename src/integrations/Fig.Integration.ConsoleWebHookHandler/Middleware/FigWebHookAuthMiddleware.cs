using System.Net;

namespace Fig.Integration.ConsoleWebHookHandler;

public class FigWebHookAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ISettings _settings;

    public FigWebHookAuthMiddleware(RequestDelegate next, ISettings settings)
    {
        _next = next;
        _settings = settings;
    }

    public async Task Invoke(HttpContext context)
    {
        var secret = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
        if (string.IsNullOrWhiteSpace(secret) ||
            !BCrypt.Net.BCrypt.EnhancedVerify(secret, _settings.HashedSecret))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        }
        else
        {
            await _next(context);
        }
    }
}