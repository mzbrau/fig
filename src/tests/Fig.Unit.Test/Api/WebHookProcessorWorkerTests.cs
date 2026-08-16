using System.Net;
using System.Net.Http;
using Fig.Api;
using Fig.Api.Converters;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Utils;
using Fig.Api.WebHooks;
using Fig.Api.Workers;
using Fig.Contracts.Health;
using Fig.Contracts.WebHook;
using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.BusinessEntities.SettingValues;
using Fig.WebHooks.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class WebHookProcessorWorkerTests
{
    private Mock<IWebHookQueue> _webHookQueue = null!;
    private Mock<HttpMessageHandler> _httpMessageHandler = null!;
    private Mock<IWebHookClientRepository> _webHookClientRepository = null!;
    private Mock<IEventLogRepository> _eventLogRepository = null!;
    private Mock<IEventLogFactory> _eventLogFactory = null!;
    private Mock<IConfigurationRepository> _configurationRepository = null!;
    private ServiceProvider _serviceProvider = null!;
    private WebHookProcessorWorker _sut = null!;
    private Guid _clientId;

    [SetUp]
    public void SetUp()
    {
        _clientId = Guid.NewGuid();
        _webHookQueue = new Mock<IWebHookQueue>();
        _httpMessageHandler = new Mock<HttpMessageHandler>();
        _webHookClientRepository = new Mock<IWebHookClientRepository>();
        _eventLogRepository = new Mock<IEventLogRepository>();
        _eventLogFactory = new Mock<IEventLogFactory>();
        _configurationRepository = new Mock<IConfigurationRepository>();

        _configurationRepository.Setup(r => r.GetConfiguration(It.IsAny<bool>()))
            .ReturnsAsync(new FigConfigurationBusinessEntity
            {
                WebApplicationBaseAddress = "https://fig.example/"
            });

        _eventLogFactory
            .Setup(f => f.WebHookSent(It.IsAny<WebHookType>(), It.IsAny<WebHookClientBusinessEntity>(), It.IsAny<string>()))
            .Returns(new EventLogBusinessEntity());
        _eventLogRepository
            .Setup(r => r.Add(It.IsAny<EventLogBusinessEntity>()))
            .Returns(Task.CompletedTask);

        _webHookClientRepository
            .Setup(r => r.GetClients(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new List<WebHookClientBusinessEntity>
            {
                new()
                {
                    Id = _clientId,
                    Name = "HookClient",
                    BaseUri = "https://hooks.example/",
                    Secret = "secret"
                }
            });

        var services = new ServiceCollection();
        services.AddScoped(_ => _webHookClientRepository.Object);
        services.AddScoped(_ => _eventLogRepository.Object);
        services.AddScoped(_ => _eventLogFactory.Object);
        services.AddScoped(_ => _configurationRepository.Object);
        services.AddScoped<IWebHookHealthConverter>(_ => new WebHookHealthConverter());
        _serviceProvider = services.BuildServiceProvider();

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(_httpMessageHandler.Object));

        _sut = new WebHookProcessorWorker(
            _webHookQueue.Object,
            _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<WebHookProcessorWorker>.Instance,
            httpClientFactory.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _sut.Dispose();
        _serviceProvider.Dispose();
    }

    [Test]
    public async Task ProcessWebHook_SendsHttpRequestAndLogsSuccess()
    {
        SetupHttpResponse(HttpStatusCode.OK);
        var item = CreateQueueItem(
            WebHookType.NewClientRegistration,
            new NewClientRegistrationWebHookData(CreateSettingClient("Orders")));

        await _sut.ProcessWebHook(item, CancellationToken.None);

        _httpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.Method == HttpMethod.Post &&
                r.RequestUri!.ToString().Contains("NewClientRegistration")),
            ItExpr.IsAny<CancellationToken>());
        _eventLogFactory.Verify(f => f.WebHookSent(
            WebHookType.NewClientRegistration,
            It.IsAny<WebHookClientBusinessEntity>(),
            "Succeeded"), Times.Once);
    }

    [Test]
    public async Task ProcessWebHook_LogsFailure_WhenHttpUnsuccessful()
    {
        SetupHttpResponse(HttpStatusCode.InternalServerError);
        var item = CreateQueueItem(
            WebHookType.SecurityEvent,
            new SecurityEventWebHookData("Login", DateTime.UtcNow, "u", true, "1.1.1.1", "host"));

        await _sut.ProcessWebHook(item, CancellationToken.None);

        _eventLogFactory.Verify(f => f.WebHookSent(
            WebHookType.SecurityEvent,
            It.IsAny<WebHookClientBusinessEntity>(),
            "Failed (InternalServerError)"), Times.Once);
    }

    [Test]
    public async Task ProcessWebHook_SkipsSend_WhenNoSettingsMatchRegex()
    {
        var webHook = CreateMatchingWebHook(WebHookType.SettingValueChanged, settingRegex: "^Other");
        var item = new WebHookQueueItem
        {
            WebHookType = WebHookType.SettingValueChanged,
            MatchingWebHooks = [webHook],
            WebHookData = new SettingValueChangedWebHookData(
                [CreateChangedSetting("Timeout")],
                CreateSettingClient("Api"),
                "user",
                "changed")
        };

        await _sut.ProcessWebHook(item, CancellationToken.None);

        _httpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
        _eventLogRepository.Verify(r => r.Add(It.IsAny<EventLogBusinessEntity>()), Times.Never);
    }

    [Test]
    public async Task ExecuteAsync_ProcessesOneDequeuedItem()
    {
        var processed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        SetupHttpResponse(HttpStatusCode.OK);

        var item = CreateQueueItem(
            WebHookType.NewClientRegistration,
            new NewClientRegistrationWebHookData(CreateSettingClient("Orders")));

        _webHookQueue.SetupSequence(q => q.DequeueWebHook())
            .Returns(item)
            .Returns((WebHookQueueItem?)null);

        _eventLogRepository
            .Setup(r => r.Add(It.IsAny<EventLogBusinessEntity>()))
            .Returns(Task.CompletedTask)
            .Callback(() => processed.TrySetResult());

        await _sut.StartAsync(CancellationToken.None);
        try
        {
            await processed.Task.WaitAsync(TimeSpan.FromSeconds(5));
            _httpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }
        finally
        {
            await _sut.StopAsync(CancellationToken.None);
        }
    }

    [Test]
    public async Task CreateContract_ReturnsNull_WhenNoSettingsMatchRegex()
    {
        var webHook = CreateMatchingWebHook(WebHookType.SettingValueChanged, settingRegex: "^Other");
        var contract = await _sut.CreateContract(
            WebHookType.SettingValueChanged,
            new SettingValueChangedWebHookData(
                [CreateChangedSetting("Timeout")],
                CreateSettingClient("Api"),
                "user",
                "changed"),
            webHook,
            _configurationRepository.Object,
            new WebHookHealthConverter());

        Assert.That(contract, Is.Null);
    }

    [Test]
    public void ShouldSend_ReturnsTrue_ForNonEmptySettingValueChangedContract()
    {
        var contract = new SettingValueChangedDataContract(
            "Api", null, ["Timeout"], "user", "msg", new Uri("https://fig.example/"));

        Assert.That(_sut.ShouldSend(contract), Is.True);
    }

    [Test]
    public void ShouldSend_ReturnsTrue_ForOtherContractTypes()
    {
        var contract = new SecurityEventDataContract(
            "Login", DateTime.UtcNow, "u", true, "1.1.1.1", "host", null, new Uri("https://fig.example/"));

        Assert.That(_sut.ShouldSend(contract), Is.True);
    }

    [Test]
    public async Task CreateContract_CreatesExpectedContract_ForEachWebHookType()
    {
        var client = CreateSettingClient("Orders");
        var statusClient = new ClientStatusBusinessEntity { Name = "Orders", Instance = "one", RunSessions = [CreateSession()] };
        var session = CreateSession();
        var config = _configurationRepository.Object;
        var healthConverter = new WebHookHealthConverter();
        var webHook = CreateMatchingWebHook(WebHookType.SettingValueChanged, settingRegex: "Timeout");

        var newReg = await _sut.CreateContract(
            WebHookType.NewClientRegistration,
            new NewClientRegistrationWebHookData(client),
            webHook,
            config,
            healthConverter);
        Assert.That(newReg, Is.TypeOf<ClientRegistrationDataContract>());
        Assert.That(((ClientRegistrationDataContract)newReg!).RegistrationType, Is.EqualTo(RegistrationType.New));

        var updatedReg = await _sut.CreateContract(
            WebHookType.UpdatedClientRegistration,
            new UpdatedClientRegistrationWebHookData(client),
            webHook,
            config,
            healthConverter);
        Assert.That(((ClientRegistrationDataContract)updatedReg!).RegistrationType, Is.EqualTo(RegistrationType.Updated));

        var settingChanged = await _sut.CreateContract(
            WebHookType.SettingValueChanged,
            new SettingValueChangedWebHookData([CreateChangedSetting("Timeout")], client, "user", "msg"),
            webHook,
            config,
            healthConverter);
        Assert.That(settingChanged, Is.Not.Null);
        Assert.That(((SettingValueChangedDataContract)settingChanged!).UpdatedSettings, Is.EquivalentTo(new[] { "Timeout" }));

        var connected = await _sut.CreateContract(
            WebHookType.ClientStatusChanged,
            new ClientConnectedWebHookData(session, statusClient),
            webHook,
            config,
            healthConverter);
        Assert.That(((ClientStatusChangedDataContract)connected!).ConnectionEvent, Is.EqualTo(ConnectionEvent.Connected));

        var disconnected = await _sut.CreateContract(
            WebHookType.ClientStatusChanged,
            new ClientDisconnectedWebHookData(session, statusClient),
            webHook,
            config,
            healthConverter);
        Assert.That(((ClientStatusChangedDataContract)disconnected!).ConnectionEvent, Is.EqualTo(ConnectionEvent.Disconnected));

        var health = await _sut.CreateContract(
            WebHookType.HealthStatusChanged,
            new HealthStatusChangedWebHookData(session, statusClient, new HealthDataContract { Status = FigHealthStatus.Healthy }),
            webHook,
            config,
            healthConverter);
        Assert.That(health, Is.TypeOf<ClientHealthChangedDataContract>());

        var minSessions = await _sut.CreateContract(
            WebHookType.MinRunSessions,
            new MinRunSessionsWebHookData(statusClient, RunSessionsEvent.BelowMinimum, 0),
            webHook,
            config,
            healthConverter);
        Assert.That(((MinRunSessionsDataContract)minSessions!).RunSessionsEvent, Is.EqualTo(RunSessionsEvent.BelowMinimum));
        Assert.That(((MinRunSessionsDataContract)minSessions).RunSessions, Is.EqualTo(0));

        var security = await _sut.CreateContract(
            WebHookType.SecurityEvent,
            new SecurityEventWebHookData("Login", DateTime.UtcNow, "u", false, "ip", "host", "reason"),
            webHook,
            config,
            healthConverter);
        Assert.That(security, Is.TypeOf<SecurityEventDataContract>());
        Assert.That(((SecurityEventDataContract)security!).EventType, Is.EqualTo("Login"));
    }

    private WebHookQueueItem CreateQueueItem(WebHookType type, object data) =>
        new()
        {
            WebHookType = type,
            WebHookData = data,
            MatchingWebHooks = [CreateMatchingWebHook(type)]
        };

    private WebHookBusinessEntity CreateMatchingWebHook(
        WebHookType type,
        string clientRegex = ".*",
        string? settingRegex = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            ClientId = _clientId,
            WebHookType = type,
            ClientNameRegex = clientRegex,
            SettingNameRegex = settingRegex,
            MinSessions = 1
        };

    private void SetupHttpResponse(HttpStatusCode statusCode)
    {
        _httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode));
    }

    private static SettingClientBusinessEntity CreateSettingClient(string name) =>
        new()
        {
            Name = name,
            Settings =
            [
                new SettingBusinessEntity { Name = "Timeout" }
            ]
        };

    private static ClientRunSessionBusinessEntity CreateSession() =>
        new()
        {
            Id = Guid.NewGuid(),
            RunSessionId = Guid.NewGuid(),
            StartTimeUtc = DateTime.UtcNow,
            IpAddress = "127.0.0.1",
            Hostname = "host",
            FigVersion = "1.0",
            ApplicationVersion = "1.0",
            RunningUser = "user"
        };

    private static ChangedSetting CreateChangedSetting(string name) =>
        new(name,
            new StringSettingBusinessEntity("old"),
            new StringSettingBusinessEntity("new"),
            false,
            null,
            false);
}
