using Fig.Api.Datalayer.Repositories;
using Fig.Api.Reports;
using Fig.Api.Reports.Implementations;
using Fig.Api.Reports.Rendering.Components;
using Fig.Common.Constants;
using Fig.Contracts.SettingGroups;
using Fig.Datalayer.BusinessEntities;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Unit.Test.Api.Reports;

[TestFixture]
public class AnalyticsReportExecuteTests
{
    private static string SummaryValue(IReadOnlyList<SummaryCardItem> summary, string label)
        => summary.Single(s => s.Label == label).Value;

    [Test]
    public async Task ChangeAnalyticsReport_ExecuteAsync_AggregatesChangeEvents()
    {
        var (from, to) = ReportTestFixtures.DefaultRange(7);
        var events = new List<EventLogBusinessEntity>
        {
            ReportTestFixtures.CreateEvent(EventMessage.SettingValueUpdated, from.AddDays(1), "App", settingName: "A"),
            ReportTestFixtures.CreateEvent(EventMessage.ExternallyManagedSettingUpdatedByUser, from.AddDays(2), "App", settingName: "B")
        };

        var eventLog = new Mock<IEventLogRepository>();
        eventLog.Setup(r => r.GetEventsByTypes(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<Fig.Contracts.Authentication.UserDataContract>(), null, null))
            .ReturnsAsync(events);

        var clients = new Mock<ISettingClientRepository>();
        var report = new ChangeAnalyticsReport(eventLog.Object, clients.Object);
        ReportTestFixtures.Authenticate(report);

        var model = (ChangeAnalyticsReportModel)await report.ExecuteAsync(new ChangeAnalyticsParameters { From = from, To = to });

        Assert.That(model.ScopeDisplay, Is.EqualTo("All clients"));
        Assert.That(SummaryValue(model.Summary, "Total Changes"), Is.EqualTo("2"));
        Assert.That(SummaryValue(model.Summary, "Externally Managed Updates"), Is.EqualTo("1"));
        Assert.That(model.TopSettings, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task ChangeAnalyticsReport_ExecuteAsync_WithClient_ListsUnchangedSettings()
    {
        var (from, to) = ReportTestFixtures.DefaultRange(7);
        var client = ReportTestFixtures.CreateClient(
            "App",
            null,
            ReportTestFixtures.CreateSetting("Changed"),
            ReportTestFixtures.CreateSetting("Quiet"));

        var events = new List<EventLogBusinessEntity>
        {
            ReportTestFixtures.CreateEvent(EventMessage.SettingValueUpdated, from.AddDays(1), "App", settingName: "Changed")
        };

        var eventLog = new Mock<IEventLogRepository>();
        eventLog.Setup(r => r.GetEventsByTypes(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<Fig.Contracts.Authentication.UserDataContract>(), "App", null))
            .ReturnsAsync(events);

        var clients = new Mock<ISettingClientRepository>();
        clients.Setup(r => r.GetClient("App", null)).ReturnsAsync(client);

        var report = new ChangeAnalyticsReport(eventLog.Object, clients.Object);
        ReportTestFixtures.Authenticate(report);

        var model = (ChangeAnalyticsReportModel)await report.ExecuteAsync(new ChangeAnalyticsParameters
        {
            From = from,
            To = to,
            ClientName = "App"
        });

        Assert.That(model.UnchangedSettings.Select(u => u.SettingName), Is.EquivalentTo(new[] { "Quiet" }));
        Assert.That(SummaryValue(model.Summary, "Total Changes"), Is.EqualTo("1"));
    }

    [Test]
    public async Task RegistrationDriftReport_ExecuteAsync_ClassifiesRegistrationActivity()
    {
        var (from, to) = ReportTestFixtures.DefaultRange(7);
        var events = new List<EventLogBusinessEntity>
        {
            ReportTestFixtures.CreateEvent(EventMessage.RegistrationWithChange, from.AddDays(1), "App"),
            ReportTestFixtures.CreateEvent(EventMessage.ClientInstanceCreated, from.AddDays(2), "App"),
            ReportTestFixtures.CreateEvent(EventMessage.RegistrationNoChange, from.AddDays(3), "App")
        };

        var staleClient = ReportTestFixtures.CreateClient("Stale");
        staleClient.LastRegistration = from.AddDays(-30);

        var eventLog = new Mock<IEventLogRepository>();
        eventLog.Setup(r => r.GetEventsByTypes(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<Fig.Contracts.Authentication.UserDataContract>(), null, null))
            .ReturnsAsync(events);

        var clients = new Mock<ISettingClientRepository>();
        clients.Setup(r => r.GetAllClients(It.IsAny<Fig.Contracts.Authentication.UserDataContract>(), false, true))
            .ReturnsAsync(new List<SettingClientBusinessEntity> { staleClient });

        var report = new RegistrationDriftReport(eventLog.Object, clients.Object);
        ReportTestFixtures.Authenticate(report);

        var model = (RegistrationDriftReportModel)await report.ExecuteAsync(new RegistrationDriftParameters { From = from, To = to });

        Assert.That(SummaryValue(model.Summary, "Registration Events"), Is.EqualTo("3"));
        Assert.That(SummaryValue(model.Summary, "Definition Changes"), Is.EqualTo("1"));
        Assert.That(SummaryValue(model.Summary, "New Instances / Clients"), Is.EqualTo("1"));
        Assert.That(model.DefinitionChanges, Has.Count.EqualTo(1));
        Assert.That(model.NoRegistrationInRange, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task RestartLiveReloadDebtReport_ExecuteAsync_SurfacesLiveDebtAndEvents()
    {
        var (from, to) = ReportTestFixtures.DefaultRange(7);
        var status = ReportTestFixtures.CreateStatus(
            "App",
            null,
            ReportTestFixtures.CreateSession(restartRequired: true, liveReload: false));

        var events = new List<EventLogBusinessEntity>
        {
            ReportTestFixtures.CreateEvent(EventMessage.RestartRequested, from.AddDays(1), "App"),
            ReportTestFixtures.CreateEvent(EventMessage.LiveReloadChanged, from.AddDays(2), "App")
        };

        var clientStatus = new Mock<IClientStatusRepository>();
        clientStatus.Setup(r => r.GetAllClients(It.IsAny<Fig.Contracts.Authentication.UserDataContract>()))
            .ReturnsAsync(new List<ClientStatusBusinessEntity> { status });

        var eventLog = new Mock<IEventLogRepository>();
        eventLog.Setup(r => r.GetEventsByTypes(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<Fig.Contracts.Authentication.UserDataContract>(), null, null))
            .ReturnsAsync(events);

        var report = new RestartLiveReloadDebtReport(clientStatus.Object, eventLog.Object);
        ReportTestFixtures.Authenticate(report);

        var model = (RestartLiveReloadDebtReportModel)await report.ExecuteAsync(new RestartLiveReloadDebtParameters { From = from, To = to });

        Assert.That(model.LiveDebt, Has.Count.EqualTo(1));
        Assert.That(SummaryValue(model.Summary, "Live Debt Sessions"), Is.EqualTo("1"));
        Assert.That(SummaryValue(model.Summary, "Restart Requests"), Is.EqualTo("1"));
        Assert.That(model.HistoricalEvents, Has.Count.EqualTo(2));
        Assert.That(model.TopRestartClients.Single().RestartRequests, Is.EqualTo(1));
    }

    [Test]
    public async Task ExternallyManagedOverridesReport_ExecuteAsync_ListsInventoryAndOverrides()
    {
        var (from, to) = ReportTestFixtures.DefaultRange(7);
        var client = ReportTestFixtures.CreateClient(
            "App",
            null,
            ReportTestFixtures.CreateSetting("Managed", isExternallyManaged: true),
            ReportTestFixtures.CreateSetting("Regular"));

        var overrideEvents = new List<EventLogBusinessEntity>
        {
            ReportTestFixtures.CreateEvent(EventMessage.ExternallyManagedSettingUpdatedByUser, from.AddDays(1), "App", settingName: "Managed")
        };

        var clients = new Mock<ISettingClientRepository>();
        clients.Setup(r => r.GetAllClients(It.IsAny<Fig.Contracts.Authentication.UserDataContract>(), false, true))
            .ReturnsAsync(new List<SettingClientBusinessEntity> { client });

        var eventLog = new Mock<IEventLogRepository>();
        eventLog.Setup(r => r.GetEventsByTypes(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<Fig.Contracts.Authentication.UserDataContract>(), null, null))
            .ReturnsAsync(overrideEvents);

        var report = new ExternallyManagedOverridesReport(clients.Object, eventLog.Object);
        ReportTestFixtures.Authenticate(report);

        var model = (ExternallyManagedOverridesReportModel)await report.ExecuteAsync(new ExternallyManagedOverridesParameters { From = from, To = to });

        Assert.That(model.Inventory, Has.Count.EqualTo(1));
        Assert.That(model.Inventory[0].SettingName, Is.EqualTo("Managed"));
        Assert.That(SummaryValue(model.Summary, "Override Events"), Is.EqualTo("1"));
        Assert.That(model.OverrideEvents, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task FleetHealthReport_ExecuteAsync_MinSessionsFlagsUnderProvisionedClients()
    {
        var (from, to) = ReportTestFixtures.DefaultRange(7);
        var empty = ReportTestFixtures.CreateStatus("Empty");
        var oneSession = ReportTestFixtures.CreateStatus("One", null, ReportTestFixtures.CreateSession());

        var clientStatus = new Mock<IClientStatusRepository>();
        clientStatus.Setup(r => r.GetAllClients(It.IsAny<Fig.Contracts.Authentication.UserDataContract>()))
            .ReturnsAsync(new List<ClientStatusBusinessEntity> { empty, oneSession });

        var eventLog = new Mock<IEventLogRepository>();
        eventLog.Setup(r => r.GetEventsByTypes(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<Fig.Contracts.Authentication.UserDataContract>(), null, null))
            .ReturnsAsync(new List<EventLogBusinessEntity>());

        var report = new FleetHealthReport(clientStatus.Object, eventLog.Object);
        ReportTestFixtures.Authenticate(report);

        var model = (FleetHealthReportModel)await report.ExecuteAsync(new FleetHealthParameters
        {
            From = from,
            To = to,
            MinSessions = 2
        });

        Assert.That(SummaryValue(model.Summary, "Clients"), Is.EqualTo("2"));
        Assert.That(SummaryValue(model.Summary, "Below Min Sessions (2)"), Is.EqualTo("2"));
        Assert.That(model.BelowMinSessions, Has.Count.EqualTo(2));
        Assert.That(model.ZeroSessionClients, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task BlastRadiusReport_ExecuteAsync_IncludeGroupMembersFalse_OnlyTargetsPrimarySetting()
    {
        var client = ReportTestFixtures.CreateClient("App", null, ReportTestFixtures.CreateSetting("Target"));
        var other = ReportTestFixtures.CreateClient("Other", null, ReportTestFixtures.CreateSetting("SharedSetting"));

        var groupJson = JsonConvert.SerializeObject(new List<GroupedSettingDataContract>
        {
            new("Bundle", null, "string",
            [
                new SourceSettingDataContract("App", "Target"),
                new SourceSettingDataContract("Other", "SharedSetting")
            ])
        });

        var groups = new Mock<ISettingGroupRepository>();
        groups.Setup(r => r.GetAllGroups()).ReturnsAsync(new List<SettingGroupBusinessEntity>
        {
            new() { Name = "G1", GroupSettingsJson = groupJson }
        });

        var clients = new Mock<ISettingClientRepository>();
        clients.Setup(r => r.GetClient("App", null)).ReturnsAsync(client);
        clients.Setup(r => r.GetAllClients(It.IsAny<Fig.Contracts.Authentication.UserDataContract>(), false, true))
            .ReturnsAsync(new List<SettingClientBusinessEntity> { client, other });

        var statuses = new Mock<IClientStatusRepository>();
        statuses.Setup(r => r.GetAllClients(It.IsAny<Fig.Contracts.Authentication.UserDataContract>()))
            .ReturnsAsync(new List<ClientStatusBusinessEntity>());

        var eventLog = new Mock<IEventLogRepository>();
        eventLog.Setup(r => r.GetEventsByTypes(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<Fig.Contracts.Authentication.UserDataContract>(), null, null))
            .ReturnsAsync(new List<EventLogBusinessEntity>());

        var report = new BlastRadiusReport(clients.Object, groups.Object, statuses.Object, eventLog.Object);
        ReportTestFixtures.Authenticate(report);

        var model = (BlastRadiusReportModel)await report.ExecuteAsync(new BlastRadiusParameters
        {
            ClientName = "App",
            SettingName = "Target",
            IncludeGroupMembers = false
        });

        Assert.That(SummaryValue(model.Summary, "Affected Settings"), Is.EqualTo("1"));
        Assert.That(model.Affected, Has.Count.EqualTo(1));
        Assert.That(model.Affected[0].SettingName, Is.EqualTo("Target"));
        Assert.That(SummaryValue(model.Summary, "Matching Groups"), Is.EqualTo("1"));
    }

    [Test]
    public void BlastRadiusReport_ExecuteAsync_ThrowsWhenClientMissing()
    {
        var clients = new Mock<ISettingClientRepository>();
        clients.Setup(r => r.GetClient("Missing", null)).ReturnsAsync((SettingClientBusinessEntity?)null);

        var groups = new Mock<ISettingGroupRepository>();
        var statuses = new Mock<IClientStatusRepository>();
        var eventLog = new Mock<IEventLogRepository>();

        var report = new BlastRadiusReport(clients.Object, groups.Object, statuses.Object, eventLog.Object);
        ReportTestFixtures.Authenticate(report);

        Assert.ThrowsAsync<KeyNotFoundException>(() => report.ExecuteAsync(new BlastRadiusParameters
        {
            ClientName = "Missing",
            SettingName = "Any"
        }));
    }

    [Test]
    public async Task AnomalyQuietPeriodReport_ExecuteAsync_ComparesPeriodToBaseline()
    {
        var to = DateTime.UtcNow;
        var from = to.AddDays(-7);
        var baselineFrom = from.AddDays(-7);
        var baselineTo = from;

        var periodEvents = new List<EventLogBusinessEntity>
        {
            ReportTestFixtures.CreateEvent(EventMessage.LoginFailed, from.AddDays(1))
        };
        var baselineEvents = Enumerable.Range(0, 5)
            .Select(i => ReportTestFixtures.CreateEvent(EventMessage.LoginFailed, baselineFrom.AddDays(i + 1)))
            .ToList();

        var eventLog = new Mock<IEventLogRepository>();
        eventLog.Setup(r => r.GetEventsByTypes(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<Fig.Contracts.Authentication.UserDataContract>(),
                null,
                null))
            .ReturnsAsync((DateTime start, DateTime end, IReadOnlyCollection<string> _, Fig.Contracts.Authentication.UserDataContract __, string? ___, string? ____) =>
                start == baselineFrom ? baselineEvents : periodEvents);

        var quietClient = ReportTestFixtures.CreateClient("Quiet");
        quietClient.LastRegistration = baselineFrom.AddDays(-1);

        var clientStatus = new Mock<IClientStatusRepository>();
        clientStatus.Setup(r => r.GetAllClients(It.IsAny<Fig.Contracts.Authentication.UserDataContract>()))
            .ReturnsAsync(new List<ClientStatusBusinessEntity>());

        var clients = new Mock<ISettingClientRepository>();
        clients.Setup(r => r.GetAllClients(It.IsAny<Fig.Contracts.Authentication.UserDataContract>(), false, true))
            .ReturnsAsync(new List<SettingClientBusinessEntity> { quietClient });

        var report = new AnomalyQuietPeriodReport(eventLog.Object, clientStatus.Object, clients.Object);
        ReportTestFixtures.Authenticate(report);

        var model = (AnomalyQuietPeriodReportModel)await report.ExecuteAsync(new AnomalyQuietPeriodParameters { From = from, To = to });

        Assert.That(SummaryValue(model.Summary, "Period Events"), Is.EqualTo("1"));
        Assert.That(SummaryValue(model.Summary, "Baseline Events"), Is.EqualTo("5"));
        Assert.That(model.Anomalies, Has.Count.EqualTo(4));
        Assert.That(model.Anomalies.Single(a => a.Metric == "Failed Logins").Flagged, Is.EqualTo("No"));
        Assert.That(model.QuietClients.Select(q => q.ClientDisplay), Is.EquivalentTo(new[] { "Quiet" }));
    }

    [Test]
    public async Task StaleConfigReport_ExecuteAsync_IdentifiesStaleSettingsAndSilentClients()
    {
        var (from, to) = ReportTestFixtures.DefaultRange(7);
        var staleSetting = ReportTestFixtures.CreateSetting("Old", lastChanged: DateTime.UtcNow.AddDays(-120));
        var client = ReportTestFixtures.CreateClient("App", null, staleSetting);
        var silent = ReportTestFixtures.CreateClient("Silent");

        var clients = new Mock<ISettingClientRepository>();
        clients.Setup(r => r.GetAllClients(It.IsAny<Fig.Contracts.Authentication.UserDataContract>(), false, true))
            .ReturnsAsync(new List<SettingClientBusinessEntity> { client, silent });

        var clientStatus = new Mock<IClientStatusRepository>();
        clientStatus.Setup(r => r.GetAllClients(It.IsAny<Fig.Contracts.Authentication.UserDataContract>()))
            .ReturnsAsync(new List<ClientStatusBusinessEntity>());

        var eventLog = new Mock<IEventLogRepository>();
        eventLog.Setup(r => r.GetEventsByTypes(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.Is<IReadOnlyCollection<string>>(t => t.Contains(EventMessage.SettingsRead)), It.IsAny<Fig.Contracts.Authentication.UserDataContract>(), null, null))
            .ReturnsAsync(new List<EventLogBusinessEntity>());
        eventLog.Setup(r => r.GetEventsByTypes(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.Is<IReadOnlyCollection<string>>(t => t.Contains(EventMessage.NewSession)), It.IsAny<Fig.Contracts.Authentication.UserDataContract>(), null, null))
            .ReturnsAsync(new List<EventLogBusinessEntity>());

        var report = new StaleConfigReport(clients.Object, clientStatus.Object, eventLog.Object);
        ReportTestFixtures.Authenticate(report);

        var model = (StaleConfigReportModel)await report.ExecuteAsync(new StaleConfigParameters
        {
            From = from,
            To = to,
            StaleDays = 90
        });

        Assert.That(model.StaleSettings, Has.Count.EqualTo(1));
        Assert.That(model.StaleSettings[0].SettingName, Is.EqualTo("Old"));
        Assert.That(SummaryValue(model.Summary, "No Settings Read"), Is.EqualTo("2"));
        Assert.That(model.OrphanedSilentClients, Has.Count.EqualTo(2));
    }

    [Test]
    public void StaleConfigReport_ExecuteAsync_ThrowsWhenStaleDaysBelowOne()
    {
        var (from, to) = ReportTestFixtures.DefaultRange(7);
        var report = new StaleConfigReport(
            Mock.Of<ISettingClientRepository>(),
            Mock.Of<IClientStatusRepository>(),
            Mock.Of<IEventLogRepository>());
        ReportTestFixtures.Authenticate(report);

        Assert.ThrowsAsync<ReportParameterValidationException>(() => report.ExecuteAsync(new StaleConfigParameters
        {
            From = from,
            To = to,
            StaleDays = 0
        }));
    }

    [Test]
    public async Task IncidentCorrelationReport_ExecuteAsync_BuildsTimelineAndCounts()
    {
        var (from, to) = ReportTestFixtures.DefaultRange(7);
        var events = new List<EventLogBusinessEntity>
        {
            ReportTestFixtures.CreateEvent(EventMessage.SettingValueUpdated, from.AddDays(1), "App", settingName: "Timeout"),
            ReportTestFixtures.CreateEvent(EventMessage.RestartRequested, from.AddDays(2), "App"),
            ReportTestFixtures.CreateEvent(EventMessage.NewSession, from.AddDays(3), "App")
        };

        var eventLog = new Mock<IEventLogRepository>();
        eventLog.Setup(r => r.GetClientEvents(from, to, "App", null, It.IsAny<IReadOnlyCollection<string>?>()))
            .ReturnsAsync(events);

        var status = ReportTestFixtures.CreateStatus("App", null, ReportTestFixtures.CreateSession());
        var clientStatus = new Mock<IClientStatusRepository>();
        clientStatus.Setup(r => r.GetClientReadOnly("App", null)).ReturnsAsync(status);

        var report = new IncidentCorrelationReport(eventLog.Object, clientStatus.Object);
        ReportTestFixtures.Authenticate(report);

        var model = (IncidentCorrelationReportModel)await report.ExecuteAsync(new IncidentCorrelationParameters
        {
            ClientName = "App",
            From = from,
            To = to
        });

        Assert.That(model.ClientDisplay, Is.EqualTo("App"));
        Assert.That(SummaryValue(model.Summary, "Setting Updates"), Is.EqualTo("1"));
        Assert.That(SummaryValue(model.Summary, "Restarts"), Is.EqualTo("1"));
        Assert.That(SummaryValue(model.Summary, "Session Events"), Is.EqualTo("1"));
        Assert.That(model.Timeline, Has.Count.EqualTo(3));
    }
}
