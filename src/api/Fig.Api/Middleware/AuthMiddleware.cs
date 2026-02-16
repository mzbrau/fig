using Fig.Api.Authorization.UserAuth;
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
        IEnumerable<IAuthenticatedService> authenticatedServices,
        IUserAuthenticationModeService userAuthenticationModeService)
    {
        try
        {
            var user = await userAuthenticationModeService.ResolveAuthenticatedUser(context);
            if (user != null)
            {
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