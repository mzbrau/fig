using System.Net;

namespace Fig.WebHooks.TestClient;

public class FigWebHookAuthMiddleware
{
    private const string HashedSecret = "$2a$11$3xvQfzPYHvedNwRz.DWX8ecezdoiarEEuvAjL9BxBiUiPZ9BC/oYC";
    private readonly RequestDelegate _next;

    public FigWebHookAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var secret = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
        if (string.IsNullOrWhiteSpace(secret) ||
            !BCrypt.Net.BCrypt.EnhancedVerify(secret, HashedSecret))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        }
        else
        {
            await _next(context);
        }
    }
}