using Fig.Api.Authorization;
using Fig.Api.Authorization.UserAuth;
using Fig.Api.Services;
using Fig.Contracts.Authentication;
using Microsoft.AspNetCore.Http;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class FigManagedUserAuthenticationModeServiceTests
{
    private Mock<IUserService> _userService = null!;
    private Mock<ITokenHandler> _tokenHandler = null!;
    private FigManagedUserAuthenticationModeService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _userService = new Mock<IUserService>();
        _tokenHandler = new Mock<ITokenHandler>();
        _sut = new FigManagedUserAuthenticationModeService(_userService.Object, _tokenHandler.Object);
    }

    [Test]
    public async Task ResolveAuthenticatedUser_ShouldRequirePasswordChange_WhenClaimAndDbAreTrue()
    {
        var userId = Guid.NewGuid();
        SetupToken(userId, passwordChangeRequiredClaim: true);
        SetupUser(userId, passwordChangeRequiredInDb: true);

        var user = await _sut.ResolveAuthenticatedUser(CreateContext());

        Assert.That(user, Is.Not.Null);
        Assert.That(user!.PasswordChangeRequired, Is.True);
    }

    [Test]
    public async Task ResolveAuthenticatedUser_ShouldNotRequirePasswordChange_WhenDbClearedButClaimStillTrue()
    {
        var userId = Guid.NewGuid();
        SetupToken(userId, passwordChangeRequiredClaim: true);
        SetupUser(userId, passwordChangeRequiredInDb: false);

        var user = await _sut.ResolveAuthenticatedUser(CreateContext());

        Assert.That(user, Is.Not.Null);
        Assert.That(user!.PasswordChangeRequired, Is.False);
    }

    [Test]
    public async Task ResolveAuthenticatedUser_ShouldNotRequirePasswordChange_WhenClaimFalseButDbTrue()
    {
        var userId = Guid.NewGuid();
        SetupToken(userId, passwordChangeRequiredClaim: false);
        SetupUser(userId, passwordChangeRequiredInDb: true);

        var user = await _sut.ResolveAuthenticatedUser(CreateContext());

        Assert.That(user, Is.Not.Null);
        Assert.That(user!.PasswordChangeRequired, Is.False);
    }

    private void SetupToken(Guid userId, bool passwordChangeRequiredClaim)
    {
        _tokenHandler.Setup(a => a.Validate(It.IsAny<string?>()))
            .Returns(new ValidatedTokenData(userId, passwordChangeRequiredClaim));
    }

    private void SetupUser(Guid userId, bool passwordChangeRequiredInDb)
    {
        _userService.Setup(a => a.GetById(userId))
            .ReturnsAsync(new UserDataContract(
                userId,
                "user",
                "Test",
                "User",
                Role.User,
                ".*",
                [],
                passwordChangeRequiredInDb));
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer token";
        return context;
    }
}
