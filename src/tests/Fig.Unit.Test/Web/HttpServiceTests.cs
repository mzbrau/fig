using System.Net;
using System.Net.Http;
using System.Text;
using Fig.Common.NetStandard.Json;
using Fig.Contracts.Json;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Web;
using Fig.Web.Models.Authentication;
using Fig.Web.Notifications;
using Fig.Web.Services;
using Fig.Web.Services.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using Radzen;

namespace Fig.Unit.Test.Web;

[TestFixture]
public class HttpServiceTests
{
    private Mock<IHttpClientFactory> _httpClientFactory = null!;
    private Mock<ILocalStorageService> _localStorageService = null!;
    private Mock<INotificationFactory> _notificationFactory = null!;
    private TestNavigationManager _navigationManager = null!;
    private NotificationService _notificationService = null!;
    private TestHttpMessageHandler _httpMessageHandler = null!;
    private HttpService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _httpClientFactory = new Mock<IHttpClientFactory>();
        _localStorageService = new Mock<ILocalStorageService>();
        _notificationFactory = new Mock<INotificationFactory>();
        _navigationManager = new TestNavigationManager("http://localhost/dashboard");
        _notificationService = new NotificationService();
        _httpMessageHandler = new TestHttpMessageHandler();

        var httpClient = new HttpClient(_httpMessageHandler)
        {
            BaseAddress = new Uri("https://localhost:5260/")
        };

        _httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);
        _notificationFactory.Setup(x => x.Failure(It.IsAny<string>(), It.IsAny<string?>()))
            .Returns((string summary, string? detail) => new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = summary,
                Detail = detail ?? string.Empty
            });
        _notificationFactory.Setup(x => x.Warning(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string summary, string detail) => new NotificationMessage
            {
                Severity = NotificationSeverity.Warning,
                Summary = summary,
                Detail = detail
            });

        _sut = CreateSut();
    }

    private HttpService CreateSut(WebAuthMode mode = WebAuthMode.FigManaged)
    {
        return new HttpService(
            _httpClientFactory.Object,
            _navigationManager,
            CreateAccessTokenProvider(),
            _notificationService,
            _notificationFactory.Object,
            Options.Create(new WebSettings
            {
                Authentication = new WebAuthenticationSettings { Mode = mode }
            }));
    }

    private IFigApiAccessTokenProvider CreateAccessTokenProvider()
    {
        return new TestFigApiAccessTokenProvider(_localStorageService.Object);
    }

    [Test]
    public async Task GetAnonymous_ShouldNotNavigateToLogout_OnUnauthorized()
    {
        _localStorageService.Setup(x => x.GetItem<AuthenticatedUserModel>("user"))
            .ReturnsAsync(CreateAuthenticatedUser());
        _httpMessageHandler.Response = new HttpResponseMessage(HttpStatusCode.Unauthorized);

        var result = await _sut.GetAnonymous<object>("/apiversion", false);

        Assert.That(result, Is.Null);
        Assert.That(_navigationManager.Uri, Is.EqualTo("http://localhost/dashboard"));
        Assert.That(_httpMessageHandler.LastRequest?.Headers.Authorization, Is.Null);
    }

    [Test]
    public async Task Get_ShouldNavigateToLogout_WhenJwtWasAttachedAndResponseIsUnauthorized()
    {
        _localStorageService.Setup(x => x.GetItem<AuthenticatedUserModel>("user"))
            .ReturnsAsync(CreateAuthenticatedUser());
        _httpMessageHandler.Response = new HttpResponseMessage(HttpStatusCode.Unauthorized);

        var result = await _sut.Get<object>("/users", false);

        Assert.That(result, Is.Null);
        Assert.That(_navigationManager.Uri, Is.EqualTo("http://localhost/account/logout?sessionExpired=1"));
        Assert.That(_httpMessageHandler.LastRequest?.Headers.Authorization?.Scheme, Is.EqualTo("Bearer"));
        _notificationFactory.Verify(x => x.Warning(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task Get_ShouldNotNavigateToLogout_WhenNoJwtWasAttached()
    {
        _localStorageService.Setup(x => x.GetItem<AuthenticatedUserModel>("user"))
            .ReturnsAsync((AuthenticatedUserModel?)null);
        _httpMessageHandler.Response = new HttpResponseMessage(HttpStatusCode.Unauthorized);

        var result = await _sut.Get<object>("/users", false);

        Assert.That(result, Is.Null);
        Assert.That(_navigationManager.Uri, Is.EqualTo("http://localhost/dashboard"));
        Assert.That(_httpMessageHandler.LastRequest?.Headers.Authorization, Is.Null);
        _notificationFactory.Verify(x => x.Warning(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task Get_ShouldLogoutWithoutSendingRequest_WhenFigManagedJwtIsExpired()
    {
        _localStorageService.Setup(x => x.GetItem<AuthenticatedUserModel>("user"))
            .ReturnsAsync(CreateAuthenticatedUser(CreateJwt(DateTimeOffset.UtcNow.AddHours(-1))));
        _httpMessageHandler.Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };

        var result = await _sut.Get<object>("/statuses");

        Assert.That(result, Is.Null);
        Assert.That(_httpMessageHandler.SendCount, Is.EqualTo(0));
        Assert.That(_navigationManager.Uri, Is.EqualTo("http://localhost/account/logout?sessionExpired=1"));
        _notificationFactory.Verify(x => x.Warning(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task PostAnonymous_ShouldSendRequestWithoutJwt_WhenExpiredTokenIsInStorage()
    {
        _localStorageService.Setup(x => x.GetItem<AuthenticatedUserModel>("user"))
            .ReturnsAsync(CreateAuthenticatedUser(CreateJwt(DateTimeOffset.UtcNow.AddHours(-1))));
        _httpMessageHandler.Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };

        var result = await _sut.PostAnonymous<object>("/users/authenticate", new { Username = "a", Password = "b" });

        Assert.That(result, Is.Not.Null);
        Assert.That(_httpMessageHandler.SendCount, Is.EqualTo(1));
        Assert.That(_httpMessageHandler.LastRequest?.Headers.Authorization, Is.Null);
        Assert.That(_navigationManager.Uri, Is.EqualTo("http://localhost/dashboard"));
        _notificationFactory.Verify(x => x.Warning(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task Get_ShouldSendExpiredJwt_WhenKeycloakMode()
    {
        _sut = CreateSut(WebAuthMode.Keycloak);
        var expiredToken = CreateJwt(DateTimeOffset.UtcNow.AddHours(-1));
        _localStorageService.Setup(x => x.GetItem<AuthenticatedUserModel>("user"))
            .ReturnsAsync(CreateAuthenticatedUser(expiredToken));
        _httpMessageHandler.Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };

        var result = await _sut.Get<object>("/statuses");

        Assert.That(result, Is.Not.Null);
        Assert.That(_httpMessageHandler.SendCount, Is.EqualTo(1));
        Assert.That(_httpMessageHandler.LastRequest?.Headers.Authorization?.Parameter, Is.EqualTo(expiredToken));
        Assert.That(_navigationManager.Uri, Is.EqualTo("http://localhost/dashboard"));
    }

    [Test]
    public async Task Get_ShouldReturnDefaultAndNotify_WhenErrorResponseHasNoContent()
    {
        _localStorageService.Setup(x => x.GetItem<AuthenticatedUserModel>("user"))
            .ReturnsAsync(CreateAuthenticatedUser());
        _httpMessageHandler.Response = new HttpResponseMessage(HttpStatusCode.InternalServerError);

        var result = await _sut.Get<object>("/settinggroups");

        Assert.That(result, Is.Null);
        _notificationFactory.Verify(x => x.Failure("Server Side Error",
            It.Is<string>(message => message.Contains("500"))), Times.Once);
    }

    [Test]
    public async Task Get_ShouldNotifyWithApiMessage_WhenErrorResponseUsesErrorResultContract()
    {
        _localStorageService.Setup(x => x.GetItem<AuthenticatedUserModel>("user"))
            .ReturnsAsync(CreateAuthenticatedUser());
        _httpMessageHandler.Response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent(
                "{\"ErrorType\":\"500\",\"Message\":\"Database failed\",\"Detail\":null,\"Reference\":\"abc\"}")
        };

        var result = await _sut.Get<object>("/settinggroups");

        Assert.That(result, Is.Null);
        _notificationFactory.Verify(x => x.Failure("Server Side Error", "Database failed"), Times.Once);
    }

    [Test]
    public void PutOrThrow_ShouldThrowApiMessageAndNotNotify_WhenErrorResponseUsesErrorResultContract()
    {
        _localStorageService.Setup(x => x.GetItem<AuthenticatedUserModel>("user"))
            .ReturnsAsync(CreateAuthenticatedUser());
        _httpMessageHandler.Response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(
                "{\"ErrorType\":\"400\",\"Message\":\"PreviousSecret is required\",\"Detail\":null,\"Reference\":\"abc\"}")
        };

        var exception = Assert.ThrowsAsync<HttpRequestException>(async () =>
            await _sut.PutOrThrow("/encryptionmigration", null));

        Assert.That(exception!.Message, Is.EqualTo("PreviousSecret is required"));
        _notificationFactory.Verify(x => x.Failure(It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
    }

    [Test]
    public async Task GetLargeTimed_ReturnsParsedValueWithTimingSplit()
    {
        _localStorageService.Setup(x => x.GetItem<AuthenticatedUserModel>("user"))
            .ReturnsAsync(CreateAuthenticatedUser());
        _httpMessageHandler.Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"Name\":\"ClientA\"}", Encoding.UTF8, "application/json")
        };

        var result = await _sut.GetLargeTimed<SimpleNamedDto>("/clients/descriptions");

        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value!.Name, Is.EqualTo("ClientA"));
        Assert.That(result.RequestMs, Is.GreaterThanOrEqualTo(0));
        Assert.That(result.DeserializeMs, Is.GreaterThanOrEqualTo(0));
        Assert.That(result.BodyReadMs, Is.Not.Null);
        Assert.That(result.ParseMs, Is.Not.Null);
        Assert.That(result.BodyReadMs!.Value, Is.GreaterThanOrEqualTo(0));
        Assert.That(result.ParseMs!.Value, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public async Task GetLargeTimed_DeserializesClientsListWithFigWebLoadCompactJson()
    {
        _localStorageService.Setup(x => x.GetItem<AuthenticatedUserModel>("user"))
            .ReturnsAsync(CreateAuthenticatedUser());

        var clients = new List<SettingsClientDefinitionDataContract>
        {
            new(
                "CompactClient",
                description: null,
                instance: null,
                hasDisplayScripts: false,
                [
                    new SettingDefinitionDataContract(
                        "Timeout",
                        "d",
                        new StringSettingDataContract("30"),
                        false,
                        typeof(string))
                ],
                clientSettingOverrides: Array.Empty<SettingDataContract>())
        };
        var compactJson = JsonConvert.SerializeObject(clients, FigWebLoadJsonSettings.Instance);
        Assert.That(compactJson, Does.Not.Contain("$type"));
        Assert.That(compactJson, Does.Contain("\"t\":\"s\""));

        _httpMessageHandler.Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(compactJson, Encoding.UTF8, "application/json")
        };

        var result = await _sut.GetLargeTimed<List<SettingsClientDefinitionDataContract>>("/clients");

        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value!, Has.Count.EqualTo(1));
        Assert.That(result.Value![0].Name, Is.EqualTo("CompactClient"));
        Assert.That(result.Value[0].Settings[0].Value, Is.TypeOf<StringSettingDataContract>());
        Assert.That(((StringSettingDataContract)result.Value[0].Settings[0].Value!).Value, Is.EqualTo("30"));
    }

    [Test]
    public async Task GetLargeTimed_DeserializesNonClientsUriWithFigHttpTypeMetadata()
    {
        _localStorageService.Setup(x => x.GetItem<AuthenticatedUserModel>("user"))
            .ReturnsAsync(CreateAuthenticatedUser());

        var payload = new SettingsClientDefinitionDataContract(
            "FigHttpClient",
            description: null,
            instance: null,
            hasDisplayScripts: false,
            [
                new SettingDefinitionDataContract(
                    "Timeout",
                    "d",
                    new StringSettingDataContract("30"),
                    false,
                    typeof(string))
            ],
            clientSettingOverrides: Array.Empty<SettingDataContract>());
        var figHttpJson = JsonConvert.SerializeObject(
            new List<SettingsClientDefinitionDataContract> { payload },
            JsonSettings.FigHttp);
        Assert.That(figHttpJson, Does.Contain("$type"));

        _httpMessageHandler.Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(figHttpJson, Encoding.UTF8, "application/json")
        };

        // Non-/clients URI must use FigHttp (not FigWebLoad), so $type payload succeeds.
        var result = await _sut.GetLargeTimed<List<SettingsClientDefinitionDataContract>>("/clients/descriptions");

        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value!, Has.Count.EqualTo(1));
        Assert.That(result.Value![0].Name, Is.EqualTo("FigHttpClient"));
        Assert.That(result.Value[0].Settings[0].Value, Is.TypeOf<StringSettingDataContract>());
    }

    private static AuthenticatedUserModel CreateAuthenticatedUser(string? token = null)
    {
        return new AuthenticatedUserModel
        {
            Id = Guid.NewGuid(),
            Username = "user",
            FirstName = "Test",
            LastName = "User",
            Token = token ?? CreateJwt(DateTimeOffset.UtcNow.AddHours(1))
        };
    }

    private static string CreateJwt(DateTimeOffset expiry)
    {
        static string ToBase64Url(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        var header = ToBase64Url("{\"alg\":\"none\",\"typ\":\"JWT\"}");
        var payload = ToBase64Url($"{{\"exp\":{expiry.ToUnixTimeSeconds()}}}");
        return $"{header}.{payload}.sig";
    }

    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        public HttpResponseMessage Response { get; set; } = new(HttpStatusCode.OK);

        public HttpRequestMessage? LastRequest { get; private set; }

        public int SendCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            SendCount++;
            LastRequest = request;
            return Task.FromResult(Response);
        }
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager(string uri)
        {
            Initialize("http://localhost/", uri);
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            Uri = ToAbsoluteUri(uri).ToString();
        }
    }

    private sealed class SimpleNamedDto
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestFigApiAccessTokenProvider : IFigApiAccessTokenProvider
    {
        private readonly ILocalStorageService _localStorageService;

        public TestFigApiAccessTokenProvider(ILocalStorageService localStorageService)
        {
            _localStorageService = localStorageService;
        }

        public async Task<string?> GetAccessTokenAsync()
        {
            var user = await _localStorageService.GetItem<AuthenticatedUserModel>(
                WebAuthenticationConstants.AuthenticatedUserStorageKey);
            return string.IsNullOrWhiteSpace(user?.Token) ? null : user.Token;
        }
    }
}
