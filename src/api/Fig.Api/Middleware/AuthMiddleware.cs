using Fig.Api.Controllers;
using Fig.Contracts.Authentication;
using Fig.Api.Authorization.UserAuth;
using Fig.Api.Exceptions;
using Fig.Api.Services;
using Microsoft.AspNetCore.Mvc.Controllers;

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

                if (user.PasswordChangeRequired && !IsAllowedDuringForcedPasswordChange(context, user))
                    throw new UnauthorizedAccessException("Password change is required before this endpoint can be accessed.");
            }
        }
        catch (UnknownUserException)
        {
            throw new UnauthorizedAccessException("User in provided authorization token is not known");
        }

        await _next(context);
    }

    private static bool IsAllowedDuringForcedPasswordChange(HttpContext context, UserDataContract user)
    {
        if (!HttpMethods.IsPut(context.Request.Method))
            return false;

        if (!MatchesUsersSelfUpdateEndpoint(context))
            return false;

        if (!context.Request.RouteValues.TryGetValue("id", out var routeValue))
            return false;

        return Guid.TryParse(routeValue?.ToString(), out var routeId) && routeId == user.Id;
    }

    private static bool MatchesUsersSelfUpdateEndpoint(HttpContext context)
    {
        var actionDescriptor = context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();
        if (actionDescriptor != null)
        {
            return actionDescriptor.ControllerTypeInfo.AsType() == typeof(UsersController) &&
                   string.Equals(actionDescriptor.ActionName, nameof(UsersController.Update), StringComparison.Ordinal);
        }

        var path = context.Request.Path.Value;
        return !string.IsNullOrEmpty(path) &&
               path.StartsWith("/users/", StringComparison.OrdinalIgnoreCase);
    }
}
