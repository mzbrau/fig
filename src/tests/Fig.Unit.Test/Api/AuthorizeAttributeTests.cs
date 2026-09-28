using Fig.Api.Attributes;
using Fig.Contracts.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class AuthorizeAttributeTests
{
    [Test]
    public void OnAuthorization_ThrowsAuthenticationRequired_WhenUserIsMissing()
    {
        var attribute = new AuthorizeAttribute(Role.Administrator);
        var context = CreateContext();

        var exception = Assert.Throws<UnauthorizedAccessException>(() => attribute.OnAuthorization(context));

        Assert.That(exception!.Message, Is.EqualTo("Authentication required for this endpoint"));
    }

    [Test]
    public void OnAuthorization_ThrowsRoleMessage_WhenUserRoleIsNotAllowed()
    {
        var attribute = new AuthorizeAttribute(Role.Administrator);
        var context = CreateContext();
        context.HttpContext.Items["User"] = new UserDataContract(
            Guid.NewGuid(),
            "readonly",
            "Read",
            "Only",
            Role.ReadOnly,
            ".*",
            [],
            false);

        var exception = Assert.Throws<UnauthorizedAccessException>(() => attribute.OnAuthorization(context));

        Assert.That(exception!.Message, Does.StartWith("Role ReadOnly not authorized for this endpoint"));
    }

    [Test]
    public void OnAuthorization_Allows_WhenUserRoleIsAllowed()
    {
        var attribute = new AuthorizeAttribute(Role.Administrator, Role.User);
        var context = CreateContext();
        context.HttpContext.Items["User"] = new UserDataContract(
            Guid.NewGuid(),
            "admin",
            "Admin",
            "User",
            Role.Administrator,
            ".*",
            [],
            false);

        Assert.DoesNotThrow(() => attribute.OnAuthorization(context));
    }

    private static AuthorizationFilterContext CreateContext()
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }
}
