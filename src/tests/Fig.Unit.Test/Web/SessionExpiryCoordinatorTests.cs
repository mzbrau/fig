using Fig.Common.Timer;
using Fig.Web;
using Fig.Web.Models.Authentication;
using Fig.Web.Services;
using Fig.Web.Services.Authentication;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using System.Text;

namespace Fig.Unit.Test.Web;

[TestFixture]
public class SessionExpiryCoordinatorTests
{
    private Mock<ILocalStorageService> _localStorageService = null!;
    private Mock<ITimerFactory> _timerFactory = null!;
    private Mock<Fig.Common.Timer.ITimer> _timer = null!;

    [SetUp]
    public void SetUp()
    {
        _localStorageService = new Mock<ILocalStorageService>();
        _timerFactory = new Mock<ITimerFactory>();
        _timer = new Mock<Fig.Common.Timer.ITimer>();

        _timerFactory.Setup(x => x.Create(It.IsAny<Func<Task>>(), It.IsAny<TimeSpan>()))
            .Returns(_timer.Object);
    }

    [Test]
    public void NotifySessionExpired_RaisesReauthenticationRequiredOnce()
    {
        var sut = CreateSut(WebAuthMode.FigManaged);
        var raiseCount = 0;
        sut.ReauthenticationRequired += () => raiseCount++;

        sut.NotifySessionExpired();
        sut.NotifySessionExpired();

        Assert.That(raiseCount, Is.EqualTo(1));
        Assert.That(sut.IsReauthenticationPending, Is.True);
        Assert.That(sut.LogoutOnAbandon, Is.True);
    }

    [Test]
    public async Task RequestReauthentication_DoesNotForceLogoutOnAbandon_WhenTokenStillValid()
    {
        var token = CreateJwt(DateTimeOffset.UtcNow.AddMinutes(-30), DateTimeOffset.UtcNow.AddMinutes(30));
        _localStorageService.Setup(x => x.GetItem<AuthenticatedUserModel>("user"))
            .ReturnsAsync(new AuthenticatedUserModel { Token = token, Username = "user" });

        var sut = CreateSut(WebAuthMode.FigManaged);
        await sut.CheckExpiryAsync();

        sut.RequestReauthentication();

        Assert.That(sut.IsReauthenticationPending, Is.True);
        Assert.That(sut.LogoutOnAbandon, Is.False);
    }

    [Test]
    public void NotifySessionExpired_IsNoOp_InKeycloakMode()
    {
        var sut = CreateSut(WebAuthMode.Keycloak);
        var raiseCount = 0;
        sut.ReauthenticationRequired += () => raiseCount++;

        sut.NotifySessionExpired();

        Assert.That(raiseCount, Is.EqualTo(0));
        Assert.That(sut.IsReauthenticationPending, Is.False);
        _timerFactory.Verify(x => x.Create(It.IsAny<Func<Task>>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Test]
    public async Task CheckExpiryAsync_RaisesSessionExpiringSoonOncePerToken()
    {
        var issuedAt = DateTimeOffset.UtcNow.AddMinutes(-9);
        var expiry = DateTimeOffset.UtcNow.AddMinutes(1);
        var token = CreateJwt(issuedAt, expiry);
        _localStorageService.Setup(x => x.GetItem<AuthenticatedUserModel>("user"))
            .ReturnsAsync(new AuthenticatedUserModel { Token = token, Username = "user" });

        var sut = CreateSut(WebAuthMode.FigManaged);
        var warnCount = 0;
        sut.SessionExpiringSoon += () => warnCount++;

        await sut.CheckExpiryAsync();
        await sut.CheckExpiryAsync();

        Assert.That(warnCount, Is.EqualTo(1));
    }

    [Test]
    public async Task CheckExpiryAsync_DoesNotWarn_WhenOutsideLeadWindow()
    {
        var issuedAt = DateTimeOffset.UtcNow.AddDays(-1);
        var expiry = DateTimeOffset.UtcNow.AddDays(6);
        var token = CreateJwt(issuedAt, expiry);
        _localStorageService.Setup(x => x.GetItem<AuthenticatedUserModel>("user"))
            .ReturnsAsync(new AuthenticatedUserModel { Token = token, Username = "user" });

        var sut = CreateSut(WebAuthMode.FigManaged);
        var warnCount = 0;
        sut.SessionExpiringSoon += () => warnCount++;

        await sut.CheckExpiryAsync();

        Assert.That(warnCount, Is.EqualTo(0));
    }

    [Test]
    public void MarkReauthenticationCompleted_AllowsAnotherPrompt()
    {
        var sut = CreateSut(WebAuthMode.FigManaged);
        var raiseCount = 0;
        sut.ReauthenticationRequired += () => raiseCount++;

        sut.NotifySessionExpired();
        sut.MarkReauthenticationCompleted();
        sut.NotifySessionExpired();

        Assert.That(raiseCount, Is.EqualTo(2));
    }

    [Test]
    public void GetWarningLead_UsesHalfLifetime_ForShortTokens()
    {
        Assert.That(JwtTokenHelper.GetWarningLead(TimeSpan.FromMinutes(1)), Is.EqualTo(TimeSpan.FromSeconds(30)));
        Assert.That(JwtTokenHelper.GetWarningLead(TimeSpan.FromMinutes(10)), Is.EqualTo(TimeSpan.FromMinutes(2)));
    }

    private SessionExpiryCoordinator CreateSut(WebAuthMode mode)
    {
        return new SessionExpiryCoordinator(
            Options.Create(new WebSettings
            {
                Authentication = new WebAuthenticationSettings { Mode = mode }
            }),
            _localStorageService.Object,
            _timerFactory.Object);
    }

    private static string CreateJwt(DateTimeOffset issuedAt, DateTimeOffset expiry)
    {
        static string ToBase64Url(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        var header = ToBase64Url("{\"alg\":\"none\",\"typ\":\"JWT\"}");
        var payload = ToBase64Url(
            $"{{\"iat\":{issuedAt.ToUnixTimeSeconds()},\"exp\":{expiry.ToUnixTimeSeconds()}}}");
        return $"{header}.{payload}.sig";
    }
}
