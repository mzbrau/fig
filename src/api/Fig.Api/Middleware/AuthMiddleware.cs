using Fig.Api.Authorization;
using Fig.Api.Exceptions;
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
        IEnumerable<IAuthenticatedService> authenticatedServices,
        ITokenHandler tokenHandler)
    {
        try
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            var userId = tokenHandler.Validate(token);
            if (userId != null)
            {
                // attach user to context on successful jwt validation
                var user = await userService.GetById(userId.Value);
                context.Items["User"] = user;
                foreach (var service in authenticatedServices)
                    service.SetAuthenticatedUser(user);
            }
        }
        catch (UnknownUserException)
        {
            throw new UnauthorizedAccessException("User in provided authorization token is not known");
        }

        await _next(context);
    }
}