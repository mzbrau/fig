using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fig.Common.Events;
using Fig.Common.NetStandard.Scripting;
using Fig.Contracts.Health;
using Fig.Web.Converters;
using Fig.Web.Events;
using Fig.Web.Facades;
using Fig.Web.Models.Clients;
using Fig.Web.Models.Setting;
using Fig.Web.Notifications;
using Fig.Web.Services;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Radzen;

namespace Fig.Unit.Test.Web;

/// <summary>
/// Tests for CheckClientRunSessions to verify that run session counts are assigned
/// correctly to base clients and instances.
/// </summary>
[TestFixture]
public class SettingClientFacadeRunSessionTests
{
    private SettingClientFacade _sut = null!;
    private Mock<IClientStatusFacade> _clientStatusFacade = null!;
    private List<ClientRunSessionModel> _runSessions = null!;

    [SetUp]
    public void SetUp()
    {
        _runSessions = new List<ClientRunSessionModel>();
        _clientStatusFacade = new Mock<IClientStatusFacade>();
        _clientStatusFacade.Setup(f => f.Refresh()).Returns(Task.CompletedTask);
        _clientStatusFacade.Setup(f => f.ClientRunSessions).Returns(_runSessions);
        _clientStatusFacade.Setup(f => f.GetLastSeen(It.IsAny<string>(), It.IsAny<string?>())).Returns((LastSeenModel?)null);

        _sut = new SettingClientFacade(
            new Mock<IHttpService>().Object,
            new Mock<ISettingsDefinitionConverter>().Object,
            new Mock<ISettingHistoryConverter>().Object,
            new Mock<IScriptRunner>().Object,
            Options.Create(new Fig.Web.WebSettings()),
            new NotificationService(),
            new Mock<INotificationFactory>().Object,
            _clientStatusFacade.Object,
            new Mock<IEventDistributor>().Object,
            new Mock<IApiVersionFacade>().Object,
            new Mock<ISchedulingFacade>().Object,
            Mock.Of<IDisplayScriptStatusService>());
    }

    [Test]
    public async Task ShallCountBaseRunSessionForBaseClientWhenNoInstancesDefined()
    {
        var baseClient = AddSettingClient("MyApp", instance: null);
        _runSessions.Add(CreateRunSession("MyApp", instance: null));

        await _sut.CheckClientRunSessions();

        Assert.That(baseClient.CurrentRunSessions, Is.EqualTo(1));
    }

    [Test]
    public async Task ShallCountInstanceRunSessionForBaseClientWhenNoInstancesDefined()
    {
        // When settings has no named instances, any run session (even with an instance) uses the base
        var baseClient = AddSettingClient("MyApp", instance: null);
        _runSessions.Add(CreateRunSession("MyApp", instance: "prod"));

        await _sut.CheckClientRunSessions();

        Assert.That(baseClient.CurrentRunSessions, Is.EqualTo(1));
    }

    [Test]
    public async Task ShallOnlyCountBaseRunSessionForBaseClientWhenInstancesAreDefined()
    {
        var baseClient = AddSettingClient("MyApp", instance: null);
        var instanceFour = AddSettingClient("MyApp", instance: "four");
        var instanceThree = AddSettingClient("MyApp", instance: "three");

        _runSessions.Add(CreateRunSession("MyApp", instance: null)); // base client running

        await _sut.CheckClientRunSessions();

        Assert.That(baseClient.CurrentRunSessions, Is.EqualTo(1), "Base client should count 1");
        Assert.That(instanceFour.CurrentRunSessions, Is.EqualTo(0), "Instance 'four' should count 0");
        Assert.That(instanceThree.CurrentRunSessions, Is.EqualTo(0), "Instance 'three' should count 0");
    }

    [Test]
    public async Task ShallOnlyCountMatchingInstanceRunSession()
    {
        var baseClient = AddSettingClient("MyApp", instance: null);
        var instanceFour = AddSettingClient("MyApp", instance: "four");
        var instanceThree = AddSettingClient("MyApp", instance: "three");

        _runSessions.Add(CreateRunSession("MyApp", instance: "four")); // only instance 'four' running

        await _sut.CheckClientRunSessions();

        Assert.That(baseClient.CurrentRunSessions, Is.EqualTo(0), "Base client should count 0");
        Assert.That(instanceFour.CurrentRunSessions, Is.EqualTo(1), "Instance 'four' should count 1");
        Assert.That(instanceThree.CurrentRunSessions, Is.EqualTo(0), "Instance 'three' should count 0");
    }

    [Test]
    public async Task ShallFallBackToBaseClientForUndefinedInstanceRunSession()
    {
        // A run session with an instance not listed in settings falls back to the base
        var baseClient = AddSettingClient("MyApp", instance: null);
        var instanceFour = AddSettingClient("MyApp", instance: "four");

        _runSessions.Add(CreateRunSession("MyApp", instance: "unknown"));

        await _sut.CheckClientRunSessions();

        Assert.That(baseClient.CurrentRunSessions, Is.EqualTo(1), "Unknown instance should fall back to base");
        Assert.That(instanceFour.CurrentRunSessions, Is.EqualTo(0), "Instance 'four' should count 0");
    }

    [Test]
    public async Task ShallCountMixedRunSessionsCorrectly()
    {
        var baseClient = AddSettingClient("MyApp", instance: null);
        var instanceFour = AddSettingClient("MyApp", instance: "four");
        var instanceThree = AddSettingClient("MyApp", instance: "three");

        _runSessions.Add(CreateRunSession("MyApp", instance: null));   // base
        _runSessions.Add(CreateRunSession("MyApp", instance: "four")); // instance four

        await _sut.CheckClientRunSessions();

        Assert.That(baseClient.CurrentRunSessions, Is.EqualTo(1), "Base should count 1");
        Assert.That(instanceFour.CurrentRunSessions, Is.EqualTo(1), "Instance 'four' should count 1");
        Assert.That(instanceThree.CurrentRunSessions, Is.EqualTo(0), "Instance 'three' should count 0");
    }

    [Test]
    public async Task ShallNotCountRunSessionsFromDifferentClient()
    {
        var clientA = AddSettingClient("ClientA", instance: null);
        var clientB = AddSettingClient("ClientB", instance: null);
        _runSessions.Add(CreateRunSession("ClientA", instance: null));

        await _sut.CheckClientRunSessions();

        Assert.That(clientA.CurrentRunSessions, Is.EqualTo(1));
        Assert.That(clientB.CurrentRunSessions, Is.EqualTo(0));
    }

    private SettingClientConfigurationModel AddSettingClient(string name, string? instance)
    {
        var client = new SettingClientConfigurationModel(
            name,
            description: string.Empty,
            instance: instance,
            hasDisplayScripts: false,
            scriptRunner: Mock.Of<IScriptRunner>());
        _sut.SettingClients.Add(client);
        return client;
    }

    private static ClientRunSessionModel CreateRunSession(string name, string? instance)
    {
        return new ClientRunSessionModel(
            name: name,
            instance: instance,
            lastRegistration: DateTime.UtcNow,
            lastSettingValueUpdateUtc: null,
            runSessionId: Guid.NewGuid(),
            lastSeen: DateTime.UtcNow,
            liveReload: false,
            pollIntervalMs: 30000,
            startTimeUtc: DateTime.UtcNow,
            ipAddress: "127.0.0.1",
            hostname: "test-host",
            figVersion: "1.0",
            applicationVersion: "1.0",
            offlineSettingsEnabled: false,
            supportsRestart: false,
            restartRequested: false,
            restartRequiredToApplySettings: false,
            runningUser: "test",
            memoryUsageBytes: 0,
            lastSettingLoadUtc: DateTime.UtcNow,
            health: new RunSessionHealthModel(FigHealthStatus.Healthy, new List<ComponentHealthModel>()));
    }
}
