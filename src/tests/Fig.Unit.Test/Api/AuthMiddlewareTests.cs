using System.Reflection;
using Fig.Api.Authorization;
using Fig.Api.Controllers;
using Fig.Api.Middleware;
using Fig.Api.Services;
using Fig.Contracts.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class AuthMiddlewareTests
{
    private Mock<IUserService> _userService = null!;
    private Mock<IAuthenticatedService> _authenticatedService = null!;
    private Mock<ITokenHandler> _tokenHandler = null!;
    private UserDataContract _user = null!;
    private AuthMiddleware _sut = null!;
    private bool _nextCalled;

    [SetUp]
    public void SetUp()
    {
        _nextCalled = false;
        _user = new UserDataContract(
            Guid.NewGuid(),
            "forced-user",
            "Forced",
            "User",
            Role.Administrator,
            ".*",
            [],
            false);

        _userService = new Mock<IUserService>();
        _userService.Setup(x => x.GetById(_user.Id)).ReturnsAsync(_user);

        _authenticatedService = new Mock<IAuthenticatedService>();
        _tokenHandler = new Mock<ITokenHandler>();
        _tokenHandler.Setup(x => x.Validate("token")).Returns(new ValidatedTokenData(_user.Id, true));

        _sut = new AuthMiddleware(_ =>
        {
            _nextCalled = true;
            return Task.CompletedTask;
        });
    }

    [Test]
    public async Task Invoke_ShouldAllowForcedPasswordChangeUser_ToUpdateOwnUserEndpoint()
    {
        var context = CreateContext(
            HttpMethods.Put,
            $"/users/{_user.Id}",
            typeof(UsersController),
            nameof(UsersController.Update),
            _user.Id);

        await _sut.Invoke(context, _userService.Object, [_authenticatedService.Object], _tokenHandler.Object);

        Assert.That(_nextCalled, Is.True);
        _authenticatedService.Verify(x => x.SetAuthenticatedUser(It.Is<UserDataContract>(user =>
            user.Id == _user.Id && user.PasswordChangeRequired)), Times.Once);
    }

    [Test]
    public void Invoke_ShouldRejectForcedPasswordChangeUser_OnOtherPutEndpoint()
    {
        var context = CreateContext(
            HttpMethods.Put,
            $"/settinggroups/{_user.Id}",
            typeof(SettingGroupsController),
            nameof(SettingGroupsController.UpdateGroup),
            _user.Id);

        var exception = Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await _sut.Invoke(context, _userService.Object, [_authenticatedService.Object], _tokenHandler.Object));

        Assert.That(exception!.Message, Does.Contain("Password change is required"));
        Assert.That(_nextCalled, Is.False);
    }

    [Test]
    public void Invoke_ShouldRejectForcedPasswordChangeUser_WhenUpdatingDifferentUser()
    {
        var otherUserId = Guid.NewGuid();
        var context = CreateContext(
            HttpMethods.Put,
            $"/users/{otherUserId}",
            typeof(UsersController),
            nameof(UsersController.Update),
            otherUserId);

        var exception = Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await _sut.Invoke(context, _userService.Object, [_authenticatedService.Object], _tokenHandler.Object));

        Assert.That(exception!.Message, Does.Contain("Password change is required"));
        Assert.That(_nextCalled, Is.False);
    }

    private static DefaultHttpContext CreateContext(
        string method,
        string path,
        Type controllerType,
        string actionName,
        Guid routeId)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Request.Headers.Authorization = "Bearer token";
        context.Request.RouteValues["id"] = routeId.ToString();
        context.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new ControllerActionDescriptor
            {
                ActionName = actionName,
                ControllerName = controllerType.Name.Replace("Controller", string.Empty, StringComparison.Ordinal),
                ControllerTypeInfo = controllerType.GetTypeInfo()
            }),
            $"{controllerType.Name}.{actionName}"));
        return context;
    }
}
