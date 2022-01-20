using Fig.Api.Authorization;
using Fig.Api.Services;

namespace Fig.Api.Middleware;

public class AuthMiddleware
{
    private readonly RequestDelegate _next;

    public AuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context,
        IUserService userService,
        ISettingsService settingsService,
        ITokenHandler tokenHandler)
    {
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
        var userId = tokenHandler.Validate(token);
        if (userId != null)
        {
            // attach user to context on successful jwt validation
            var user = userService.GetById(userId.Value);
            context.Items["User"] = user;
            userService.SetAuthenticatedUser(user);
            settingsService.SetAuthenticatedUser(user);
        }

        await _next(context);
    }
}