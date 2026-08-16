using Fig.Api.Datalayer.Repositories;
using Fig.Api.Services;
using Fig.Api.Utils;
using Fig.Api.WebHooks;
using Fig.Contracts.WebHook;
using Fig.Datalayer.BusinessEntities;
using Fig.WebHooks.Contracts;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class WebHookDisseminationServiceTests
{
    private Mock<IWebHookRepository> _webHookRepository = null!;
    private Mock<IWebHookQueue> _webHookQueue = null!;
    private WebHookDisseminationService _sut = null!;
    private List<WebHookQueueItem> _queued = null!;

    [SetUp]
    public void SetUp()
    {
        _webHookRepository = new Mock<IWebHookRepository>();
        _webHookQueue = new Mock<IWebHookQueue>();
        _queued = [];

        _webHookQueue
            .Setup(q => q.QueueWebHook(It.IsAny<WebHookQueueItem>()))
            .Callback<WebHookQueueItem>(item => _queued.Add(item));

        _sut = new WebHookDisseminationService(
            _webHookRepository.Object,
            _webHookQueue.Object,
            Mock.Of<ILogger<WebHookDisseminationService>>());
    }

    [Test]
    public async Task ClientDisconnected_QueuesBelowMinimum_WhenCrossingThreshold()
    {
        var client = CreateStatusClient("Api", sessionCount: 2);
        var webHook = CreateWebHook(WebHookType.MinRunSessions, "Api", minSessions: 2);

        _webHookRepository.Setup(r => r.GetWebHooksByType(WebHookType.ClientStatusChanged))
            .ReturnsAsync(new List<WebHookBusinessEntity>());
        _webHookRepository.Setup(r => r.GetWebHooksByType(WebHookType.MinRunSessions))
            .ReturnsAsync(new List<WebHookBusinessEntity> { webHook });

        await _sut.ClientDisconnected(CreateSession(), client);

        var minItem = _queued.Single(i => i.WebHookType == WebHookType.MinRunSessions);
        var data = (MinRunSessionsWebHookData)minItem.WebHookData;
        Assert.That(data.RunSessionsEvent, Is.EqualTo(RunSessionsEvent.BelowMinimum));
        Assert.That(data.SessionCount, Is.EqualTo(1));
        Assert.That(minItem.MatchingWebHooks, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task ClientConnected_QueuesMinimumRestored_WhenCrossingThreshold()
    {
        var client = CreateStatusClient("Api", sessionCount: 2);
        var webHook = CreateWebHook(WebHookType.MinRunSessions, "Api", minSessions: 2);

        _webHookRepository.Setup(r => r.GetWebHooksByType(WebHookType.ClientStatusChanged))
            .ReturnsAsync(new List<WebHookBusinessEntity>());
        _webHookRepository.Setup(r => r.GetWebHooksByType(WebHookType.MinRunSessions))
            .ReturnsAsync(new List<WebHookBusinessEntity> { webHook });

        await _sut.ClientConnected(CreateSession(), client);

        var minItem = _queued.Single(i => i.WebHookType == WebHookType.MinRunSessions);
        var data = (MinRunSessionsWebHookData)minItem.WebHookData;
        Assert.That(data.RunSessionsEvent, Is.EqualTo(RunSessionsEvent.MinimumRestored));
        Assert.That(data.SessionCount, Is.EqualTo(2));
    }

    [Test]
    public async Task ClientDisconnected_DoesNotQueueMinSessions_WhenStillAtOrAboveMinimum()
    {
        var client = CreateStatusClient("Api", sessionCount: 3);
        var webHook = CreateWebHook(WebHookType.MinRunSessions, "Api", minSessions: 2);

        _webHookRepository.Setup(r => r.GetWebHooksByType(WebHookType.ClientStatusChanged))
            .ReturnsAsync(new List<WebHookBusinessEntity>());
        _webHookRepository.Setup(r => r.GetWebHooksByType(WebHookType.MinRunSessions))
            .ReturnsAsync(new List<WebHookBusinessEntity> { webHook });

        await _sut.ClientDisconnected(CreateSession(), client);

        Assert.That(_queued.Where(i => i.WebHookType == WebHookType.MinRunSessions), Is.Empty);
    }

    [Test]
    public async Task NewClientRegistration_QueuesOnlyMatchingClientRegex()
    {
        var client = new SettingClientBusinessEntity { Name = "OrdersApi", Settings = [] };
        var matching = CreateWebHook(WebHookType.NewClientRegistration, "^Orders");
        var nonMatching = CreateWebHook(WebHookType.NewClientRegistration, "^Payments");

        _webHookRepository.Setup(r => r.GetWebHooksByType(WebHookType.NewClientRegistration))
            .ReturnsAsync(new List<WebHookBusinessEntity> { matching, nonMatching });

        await _sut.NewClientRegistration(client);

        Assert.That(_queued, Has.Count.EqualTo(1));
        Assert.That(_queued[0].MatchingWebHooks.Single().ClientNameRegex, Is.EqualTo("^Orders"));
        Assert.That(_queued[0].WebHookData, Is.TypeOf<NewClientRegistrationWebHookData>());
    }

    [Test]
    public async Task NewClientRegistration_DoesNotQueue_WhenNoMatches()
    {
        var client = new SettingClientBusinessEntity { Name = "OrdersApi", Settings = [] };
        _webHookRepository.Setup(r => r.GetWebHooksByType(WebHookType.NewClientRegistration))
            .ReturnsAsync(new List<WebHookBusinessEntity>
            {
                CreateWebHook(WebHookType.NewClientRegistration, "^Payments")
            });

        await _sut.NewClientRegistration(client);

        Assert.That(_queued, Is.Empty);
    }

    [Test]
    public async Task SettingValueChanged_UsesClientRegexFilter()
    {
        var client = new SettingClientBusinessEntity { Name = "Billing", Settings = [] };
        var matching = CreateWebHook(WebHookType.SettingValueChanged, "Billing", settingRegex: ".*");
        var changes = new List<ChangedSetting>
        {
            new("Timeout", null, null, false, null, false)
        };

        _webHookRepository.Setup(r => r.GetWebHooksByType(WebHookType.SettingValueChanged))
            .ReturnsAsync(new List<WebHookBusinessEntity> { matching });

        await _sut.SettingValueChanged(changes, client, "user", "msg");

        Assert.That(_queued, Has.Count.EqualTo(1));
        Assert.That(_queued[0].WebHookType, Is.EqualTo(WebHookType.SettingValueChanged));
        Assert.That(((SettingValueChangedWebHookData)_queued[0].WebHookData).Changes, Is.SameAs(changes));
    }

    [Test]
    public async Task SecurityEvent_QueuesAllSecurityWebHooksWithoutClientFilter()
    {
        var hooks = new List<WebHookBusinessEntity>
        {
            CreateWebHook(WebHookType.SecurityEvent, "unused-a"),
            CreateWebHook(WebHookType.SecurityEvent, "unused-b")
        };
        _webHookRepository.Setup(r => r.GetWebHooksByType(WebHookType.SecurityEvent))
            .ReturnsAsync(hooks);

        var securityEvent = new SecurityEventWebHookData(
            "LoginFailed",
            DateTime.UtcNow,
            "alice",
            false,
            "1.2.3.4",
            "host",
            "bad password");

        await _sut.SecurityEvent(securityEvent);

        Assert.That(_queued, Has.Count.EqualTo(1));
        Assert.That(_queued[0].WebHookType, Is.EqualTo(WebHookType.SecurityEvent));
        Assert.That(_queued[0].MatchingWebHooks, Has.Count.EqualTo(2));
        Assert.That(_queued[0].WebHookData, Is.SameAs(securityEvent));
    }

    private static ClientStatusBusinessEntity CreateStatusClient(string name, int sessionCount)
    {
        var sessions = Enumerable.Range(0, sessionCount)
            .Select(_ => CreateSession())
            .ToList();

        return new ClientStatusBusinessEntity
        {
            Name = name,
            RunSessions = sessions
        };
    }

    private static ClientRunSessionBusinessEntity CreateSession() =>
        new()
        {
            Id = Guid.NewGuid(),
            RunSessionId = Guid.NewGuid(),
            StartTimeUtc = DateTime.UtcNow,
            FigVersion = "1.0",
            ApplicationVersion = "1.0",
            RunningUser = "user"
        };

    private static WebHookBusinessEntity CreateWebHook(
        WebHookType type,
        string clientRegex,
        int minSessions = 0,
        string? settingRegex = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            WebHookType = type,
            ClientNameRegex = clientRegex,
            SettingNameRegex = settingRegex,
            MinSessions = minSessions
        };
}
