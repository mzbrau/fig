using System.Net;
using Fig.Contracts.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Fig.Api.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly Role[] _validRoles;

    public AuthorizeAttribute(params Role[] validRoles)
    {
        if (!validRoles.Any())
            throw new ArgumentException("At least one role must be specified.");

        _validRoles = validRoles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (context.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any())
            return;

        if (context.HttpContext.Items["User"] is UserDataContract user)
            if (_validRoles.Contains(user.Role))
                return;

        context.Result = new JsonResult(new {message = nameof(HttpStatusCode.Unauthorized)})
            {StatusCode = StatusCodes.Status401Unauthorized};
    }
}