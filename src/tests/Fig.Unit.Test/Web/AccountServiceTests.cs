using System.Text;
using Fig.Common.Events;
using Fig.Contracts.Authentication;
using Fig.Web.Converters;
using Fig.Web.Events;
using Fig.Web.Models.Authentication;
using Fig.Web.Notifications;
using Fig.Web.Services;
using Fig.Web.Services.Authentication;
using Microsoft.AspNetCore.Components;
using Moq;
using NUnit.Framework;
using Radzen;

namespace Fig.Unit.Test.Web;

[TestFixture]
public class AccountServiceTests
{
    private Mock<IHttpService> _httpService = null!;
    private Mock<ILocalStorageService> _localStorageService = null!;
    private Mock<IUserConverter> _userConverter = null!;
    private Mock<IEventDistributor> _eventDistributor = null!;
    private Mock<INotificationHistoryService> _notificationHistoryService = null!;
    private TestNavigationManager _navigationManager = null!;
    private NotificationService _notificationService = null!;
    private FigManagedWebAuthenticationModeService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _httpService = new Mock<IHttpService>();
        _localStorageService = new Mock<ILocalStorageService>();
        _userConverter = new Mock<IUserConverter>();
        _eventDistributor = new Mock<IEventDistributor>();
        _notificationHistoryService = new Mock<INotificationHistoryService>();
        _navigationManager = new TestNavigationManager();
        _notificationService = new NotificationService();

        _sut = new FigManagedWebAuthenticationModeService(
            _httpService.Object,
            _navigationManager,
            _localStorageService.Object,
            _userConverter.Object,
            _eventDistributor.Object,
            _notificationHistoryService.Object,
            _notificationService);
    }

    [Test]
    public async Task Update_ShouldLogoutAfterForcedPasswordChange()
    {
        var userId = Guid.NewGuid();
        var user = CreateAuthenticatedUser(userId, CreateJwt(DateTimeOffset.UtcNow.AddMinutes(5)), true);
        _localStorageService.Setup(a => a.GetItem<AuthenticatedUserModel>("user")).ReturnsAsync(user);
        _httpService.Setup(a => a.Put($"/users/{userId}", It.IsAny<object?>(), null)).Returns(Task.CompletedTask);

        await _sut.Initialize();
        await _sut.Update(userId, new UpdateUserRequestDataContract { Password = "new-password!" });

        Assert.That(_sut.AuthenticatedUser, Is.Null);
        _localStorageService.Verify(a => a.RemoveItem("user"), Times.Once);
        _eventDistributor.Verify(a => a.PublishAsync(EventConstants.LogoutEvent), Times.Once);
        _notificationHistoryService.Verify(a => a.Clear(), Times.Once);
    }

    [Test]
    public async Task Update_ShouldNotClearForcedPasswordChange_WhenPasswordIsEmpty()
    {
        var userId = Guid.NewGuid();
        var user = CreateAuthenticatedUser(userId, CreateJwt(DateTimeOffset.UtcNow.AddMinutes(5)), true);
        _localStorageService.Setup(a => a.GetItem<AuthenticatedUserModel>("user")).ReturnsAsync(user);
        _httpService.Setup(a => a.Put($"/users/{userId}", It.IsAny<object?>(), null)).Returns(Task.CompletedTask);

        await _sut.Initialize();
        await _sut.Update(userId, new UpdateUserRequestDataContract
        {
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Password = string.Empty
        });

        Assert.That(_sut.AuthenticatedUser, Is.Not.Null);
        Assert.That(_sut.AuthenticatedUser!.PasswordChangeRequired, Is.True);
        _localStorageService.Verify(a => a.SetItem("user",
            It.Is<AuthenticatedUserModel>(storedUser => storedUser.PasswordChangeRequired)), Times.Once);
        _localStorageService.Verify(a => a.RemoveItem("user"), Times.Never);
        _eventDistributor.Verify(a => a.PublishAsync(EventConstants.LogoutEvent), Times.Never);
    }

    [Test]
    public async Task Initialize_ShouldLogoutForcedPasswordChangeUser_WhenTokenExpired()
    {
        var user = CreateAuthenticatedUser(Guid.NewGuid(), CreateJwt(DateTimeOffset.UtcNow.AddMinutes(-5)), true);
        _localStorageService.Setup(a => a.GetItem<AuthenticatedUserModel>("user")).ReturnsAsync(user);

        await _sut.Initialize();

        Assert.That(_sut.AuthenticatedUser, Is.Null);
        _localStorageService.Verify(a => a.RemoveItem("user"), Times.Once);
        _notificationHistoryService.Verify(a => a.Clear(), Times.Once);
    }

    [Test]
    public async Task Login_ShouldClearNotificationsBeforePersistingUser()
    {
        var response = new AuthenticateResponseDataContract(
            Guid.NewGuid(),
            "user",
            "Test",
            "User",
            Role.User,
            "token",
            false,
            []);
        var convertedUser = CreateAuthenticatedUser(response.Id, response.Token, false);

        _httpService.Setup(a => a.Post<AuthenticateResponseDataContract>("/users/authenticate", It.IsAny<AuthenticateRequestDataContract>()))
            .ReturnsAsync(response);
        _userConverter.Setup(a => a.Convert(response)).Returns(convertedUser);
        _notificationService.Notify(NotificationSeverity.Success, "Old", "Toast", 1000);

        await _sut.Login(new LoginModel { Username = "user", Password = "password" });

        _notificationHistoryService.Verify(a => a.Clear(), Times.Once);
        _localStorageService.Verify(a => a.SetItem("user", convertedUser), Times.Once);
        Assert.That(_notificationService.Messages, Is.Empty);
    }

    private static AuthenticatedUserModel CreateAuthenticatedUser(Guid id, string token, bool passwordChangeRequired)
    {
        return new AuthenticatedUserModel
        {
            Id = id,
            Username = "user",
            FirstName = "Test",
            LastName = "User",
            Token = token,
            PasswordChangeRequired = passwordChangeRequired
        };
    }

    private static string CreateJwt(DateTimeOffset expiry)
    {
        var header = Base64UrlEncode("{\"alg\":\"HS256\",\"typ\":\"JWT\"}");
        var payload = Base64UrlEncode($"{{\"exp\":{expiry.ToUnixTimeSeconds()}}}");
        return $"{header}.{payload}.signature";
    }

    private static string Base64UrlEncode(string value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager()
        {
            Initialize("http://localhost/", "http://localhost/");
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            Uri = ToAbsoluteUri(uri).ToString();
        }
    }
}
