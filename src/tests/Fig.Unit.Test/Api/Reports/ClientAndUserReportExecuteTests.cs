using Fig.Api.Datalayer.Repositories;
using Fig.Api.Reports;
using Fig.Api.Reports.Implementations;
using Fig.Api.Reports.Rendering;
using Fig.Client.Abstractions.Data;
using Fig.Common.Constants;
using Fig.Contracts.Authentication;
using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.BusinessEntities.SettingValues;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Api.Reports;

[TestFixture]
public class ClientAndUserReportExecuteTests
{
    [Test]
    public async Task UserActivityReport_ExecuteAsync_AggregatesUserLogs()
    {
        var (from, to) = ReportTestFixtures.DefaultRange();
        var logs = new List<EventLogBusinessEntity>
        {
            ReportTestFixtures.CreateEvent(EventMessage.Login, from.AddHours(1), user: "alice"),
            ReportTestFixtures.CreateEvent(EventMessage.SettingValueUpdated, from.AddHours(2), user: "alice",
                clientName: "App", settingName: "Port"),
            ReportTestFixtures.CreateEvent(EventMessage.LoginFailed, from.AddHours(3), user: "alice")
        };

        var eventLog = new Mock<IEventLogRepository>();
        eventLog.Setup(r => r.GetLogsForAuthenticatedUser(from, to, "alice")).ReturnsAsync(logs);

        var report = new UserActivityReport(eventLog.Object);
        ReportTestFixtures.Authenticate(report);

        var model = (UserActivityReportModel)await report.ExecuteAsync(new UserActivityParameters
        {
            Username = "alice",
            From = from,
            To = to
        });

        Assert.That(model.Username, Is.EqualTo("alice"));
        Assert.That(model.Rows, Has.Count.EqualTo(3));
        Assert.That(model.Summary.Single(s => s.Label == "Logins").Value, Is.EqualTo("1"));
        Assert.That(model.Summary.Single(s => s.Label == "Setting Changes").Value, Is.EqualTo("1"));
    }

    [Test]
    public void UserActivityReport_ExecuteAsync_RejectsInvertedRange()
    {
        var (from, to) = ReportTestFixtures.DefaultRange();
        var eventLog = new Mock<IEventLogRepository>();
        var report = new UserActivityReport(eventLog.Object);
        ReportTestFixtures.Authenticate(report);

        Assert.ThrowsAsync<ReportParameterValidationException>(() => report.ExecuteAsync(new UserActivityParameters
        {
            Username = "alice",
            From = to,
            To = from
        }));

        eventLog.Verify(
            r => r.GetLogsForAuthenticatedUser(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string>()),
            Times.Never);
    }

    [Test]
    public async Task AccessPrivilegeReport_ExecuteAsync_CorrelatesUsersWithLoginEvents()
    {
        var (from, to) = ReportTestFixtures.DefaultRange();
        var users = new List<UserBusinessEntity>
        {
            new()
            {
                Username = "alice",
                Role = Role.Administrator,
                ClientFilter = ".*",
                PasswordChangeRequired = true,
                AllowedClassifications = [Fig.Client.Abstractions.Data.Classification.Technical]
            },
            new()
            {
                Username = "bob",
                Role = Role.ReadOnly,
                ClientFilter = "^App$",
                PasswordChangeRequired = false
            }
        };

        var loginEvents = new List<EventLogBusinessEntity>
        {
            ReportTestFixtures.CreateEvent(EventMessage.Login, from.AddHours(1), user: "alice"),
            ReportTestFixtures.CreateEvent(EventMessage.LoginFailed, from.AddHours(2), user: "alice"),
            ReportTestFixtures.CreateEvent(EventMessage.Login, from.AddHours(3), user: "bob")
        };

        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetAllUsers()).ReturnsAsync(users);

        var eventLog = new Mock<IEventLogRepository>();
        eventLog.Setup(r => r.GetEventsByTypes(
                from,
                to,
                It.Is<string[]>(t => t.Contains(EventMessage.Login) && t.Contains(EventMessage.LoginFailed)),
                It.IsAny<UserDataContract>(),
                null,
                null))
            .ReturnsAsync(loginEvents);

        var report = new AccessPrivilegeReport(userRepo.Object, eventLog.Object);
        ReportTestFixtures.Authenticate(report);

        var model = (AccessPrivilegeReportModel)await report.ExecuteAsync(new AccessPrivilegeParameters
        {
            From = from,
            To = to
        });

        var alice = model.Rows.Single(r => r.Username == "alice");
        var bob = model.Rows.Single(r => r.Username == "bob");

        Assert.That(alice.LoginCount, Is.EqualTo(1));
        Assert.That(alice.FailCount, Is.EqualTo(1));
        Assert.That(alice.PasswordChangeRequired, Is.EqualTo("Yes"));
        Assert.That(bob.LoginCount, Is.EqualTo(1));
        Assert.That(bob.LastLogin, Is.Not.Null);
        Assert.That(model.Summary.Single(s => s.Label == "Users").Value, Is.EqualTo("2"));
    }

    [Test]
    public async Task AccessPrivilegeReport_ExecuteAsync_UserWithNoLoginsHasNullLastLogin()
    {
        var (from, to) = ReportTestFixtures.DefaultRange();
        var users = new List<UserBusinessEntity>
        {
            new()
            {
                Username = "quiet",
                Role = Role.ReadOnly,
                ClientFilter = ".*"
            }
        };

        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetAllUsers()).ReturnsAsync(users);

        var eventLog = new Mock<IEventLogRepository>();
        eventLog.Setup(r => r.GetEventsByTypes(
                from,
                to,
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<UserDataContract>(),
                null,
                null))
            .ReturnsAsync(new List<EventLogBusinessEntity>());

        var report = new AccessPrivilegeReport(userRepo.Object, eventLog.Object);
        ReportTestFixtures.Authenticate(report);

        var model = (AccessPrivilegeReportModel)await report.ExecuteAsync(new AccessPrivilegeParameters
        {
            From = from,
            To = to
        });

        var row = model.Rows.Single();
        Assert.That(row.LoginCount, Is.Zero);
        Assert.That(row.LastLogin, Is.Null);
    }

    [Test]
    public async Task ClientStatusReport_ExecuteAsync_GroupsSettingsAndMasksSecrets()
    {
        var client = ReportTestFixtures.CreateClient(
            "MyApp",
            null,
            ReportTestFixtures.CreateSetting("Port", "8080", category: "Network"),
            ReportTestFixtures.CreateSetting("ApiKey", "secret", isSecret: true, category: "Security"));

        var repo = new Mock<ISettingClientRepository>();
        repo.Setup(r => r.GetClient("MyApp", null)).ReturnsAsync(client);

        var report = new ClientStatusReport(repo.Object);
        ReportTestFixtures.Authenticate(report);

        var model = (ClientStatusReportModel)await report.ExecuteAsync(new ClientStatusParameters
        {
            ClientName = "MyApp"
        });

        Assert.That(model.ClientDisplay, Is.EqualTo("MyApp"));
        Assert.That(model.Summary.Single(s => s.Label == "Settings").Value, Is.EqualTo("2"));
        Assert.That(model.Summary.Single(s => s.Label == "Secret Settings").Value, Is.EqualTo("1"));
        Assert.That(model.Groups.SelectMany(g => g.Settings).Single(s => s.Name == "ApiKey").ValueHtml,
            Is.EqualTo(ReportDataGridHtml.SecretMask));
    }

    [Test]
    public void ClientStatusReport_ExecuteAsync_ThrowIfNoAccess_DeniesFilteredUser()
    {
        var repo = new Mock<ISettingClientRepository>();
        var report = new ClientStatusReport(repo.Object);
        ReportTestFixtures.Authenticate(report, ReportTestFixtures.CreateFilteredUser("^Other$"));

        Assert.ThrowsAsync<UnauthorizedAccessException>(() => report.ExecuteAsync(new ClientStatusParameters
        {
            ClientName = "MyApp"
        }));

        repo.Verify(r => r.GetClient(It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
    }

    [Test]
    public void ClientStatusReport_ExecuteAsync_ThrowsWhenClientMissing()
    {
        var repo = new Mock<ISettingClientRepository>();
        repo.Setup(r => r.GetClient("Missing", null)).ReturnsAsync((SettingClientBusinessEntity?)null);

        var report = new ClientStatusReport(repo.Object);
        ReportTestFixtures.Authenticate(report);

        Assert.ThrowsAsync<KeyNotFoundException>(() => report.ExecuteAsync(new ClientStatusParameters
        {
            ClientName = "Missing"
        }));
    }

    [Test]
    public async Task ClientHistoryReport_ExecuteAsync_IncludesEventsAndRegistrations()
    {
        var (from, to) = ReportTestFixtures.DefaultRange();
        var events = new List<EventLogBusinessEntity>
        {
            ReportTestFixtures.CreateEvent(EventMessage.SettingValueUpdated, from.AddDays(1), clientName: "MyApp",
                settingName: "Port", originalValue: "80", newValue: "443"),
            ReportTestFixtures.CreateEvent(EventMessage.NewSession, from.AddDays(2), clientName: "MyApp")
        };

        var registrations = new List<ClientRegistrationHistoryBusinessEntity>
        {
            new()
            {
                ClientName = "MyApp",
                RegistrationDateUtc = from.AddDays(1),
                ClientVersion = "2.0.0"
            },
            new()
            {
                ClientName = "Other",
                RegistrationDateUtc = from.AddDays(1),
                ClientVersion = "1.0.0"
            }
        };

        var eventLog = new Mock<IEventLogRepository>();
        eventLog.Setup(r => r.GetClientEvents(
                from,
                to,
                "MyApp",
                null,
                It.IsAny<IReadOnlyCollection<string>?>()))
            .ReturnsAsync(events);

        var registrationRepo = new Mock<IClientRegistrationHistoryRepository>();
        registrationRepo.Setup(r => r.GetAll()).ReturnsAsync(registrations);

        var clientRepo = new Mock<ISettingClientRepository>();
        clientRepo.Setup(r => r.GetClient("MyApp", null))
            .ReturnsAsync(ReportTestFixtures.CreateClient("MyApp"));

        var report = new ClientHistoryReport(eventLog.Object, registrationRepo.Object, clientRepo.Object);
        ReportTestFixtures.Authenticate(report);

        var model = (ClientHistoryReportModel)await report.ExecuteAsync(new ClientHistoryParameters
        {
            ClientName = "MyApp",
            From = from,
            To = to
        });

        Assert.That(model.Rows, Has.Count.EqualTo(2));
        Assert.That(model.Registrations, Has.Count.EqualTo(1));
        Assert.That(model.Registrations[0].ClientVersion, Is.EqualTo("2.0.0"));
        Assert.That(model.Summary.Single(s => s.Label == "Setting Changes").Value, Is.EqualTo("1"));
    }

    [Test]
    public void ClientHistoryReport_ExecuteAsync_ThrowIfNoAccess_DeniesFilteredUser()
    {
        var (from, to) = ReportTestFixtures.DefaultRange();
        var report = new ClientHistoryReport(
            Mock.Of<IEventLogRepository>(),
            Mock.Of<IClientRegistrationHistoryRepository>(),
            Mock.Of<ISettingClientRepository>());
        ReportTestFixtures.Authenticate(report, ReportTestFixtures.CreateFilteredUser("^Other$"));

        Assert.ThrowsAsync<UnauthorizedAccessException>(() => report.ExecuteAsync(new ClientHistoryParameters
        {
            ClientName = "MyApp",
            From = from,
            To = to
        }));
    }

    [Test]
    public async Task ClientUptimeReport_ExecuteAsync_ComputesAvailabilityFromSessionEvents()
    {
        var from = DateTime.UtcNow.AddDays(-1);
        var to = DateTime.UtcNow;
        var events = new List<EventLogBusinessEntity>
        {
            ReportTestFixtures.CreateEvent(EventMessage.NewSession, from.AddHours(1), clientName: "MyApp",
                newValue: "host-1"),
            ReportTestFixtures.CreateEvent(EventMessage.ExpiredSession, from.AddHours(2), clientName: "MyApp",
                originalValue: "host-1")
        };

        var status = ReportTestFixtures.CreateStatus(
            "MyApp",
            null,
            ReportTestFixtures.CreateSession(startTimeUtc: from, lastSeen: to));

        var eventLog = new Mock<IEventLogRepository>();
        eventLog.Setup(r => r.GetClientEvents(
                from,
                to,
                "MyApp",
                null,
                It.IsAny<IReadOnlyCollection<string>?>()))
            .ReturnsAsync(events);

        var statusRepo = new Mock<IClientStatusRepository>();
        statusRepo.Setup(r => r.GetClientReadOnly("MyApp", null)).ReturnsAsync(status);

        var report = new ClientUptimeReport(eventLog.Object, statusRepo.Object);
        ReportTestFixtures.Authenticate(report);

        var model = (ClientUptimeReportModel)await report.ExecuteAsync(new ClientUptimeParameters
        {
            ClientName = "MyApp",
            From = from,
            To = to
        });

        Assert.That(model.ClientDisplay, Is.EqualTo("MyApp"));
        Assert.That(model.UptimePercent + model.DowntimePercent, Is.EqualTo(100).Within(0.01));
        Assert.That(model.SessionLog, Is.Not.Empty);
        Assert.That(model.AvailabilitySlices, Has.Count.EqualTo(2));
    }

    [Test]
    public void ClientUptimeReport_ExecuteAsync_RejectsInvertedRange()
    {
        var (from, to) = ReportTestFixtures.DefaultRange();
        var report = new ClientUptimeReport(Mock.Of<IEventLogRepository>(), Mock.Of<IClientStatusRepository>());
        ReportTestFixtures.Authenticate(report);

        Assert.ThrowsAsync<ReportParameterValidationException>(() => report.ExecuteAsync(new ClientUptimeParameters
        {
            ClientName = "MyApp",
            From = to,
            To = from
        }));
    }

    [Test]
    public async Task ClientVersionReport_ExecuteAsync_ListsSessionsInRangeAndFlagsMultiVersion()
    {
        var (from, to) = ReportTestFixtures.DefaultRange();
        var clients = new List<ClientStatusBusinessEntity>
        {
            ReportTestFixtures.CreateStatus(
                "Multi",
                null,
                ReportTestFixtures.CreateSession(
                    applicationVersion: "1.0.0",
                    startTimeUtc: from.AddHours(-1),
                    lastSeen: from.AddHours(1)),
                ReportTestFixtures.CreateSession(
                    applicationVersion: "2.0.0",
                    startTimeUtc: from.AddHours(-1),
                    lastSeen: from.AddHours(2))),
            ReportTestFixtures.CreateStatus(
                "Single",
                "prod",
                ReportTestFixtures.CreateSession(
                    applicationVersion: "3.0.0",
                    startTimeUtc: from.AddHours(-1),
                    lastSeen: from.AddHours(1)))
        };

        var statusRepo = new Mock<IClientStatusRepository>();
        statusRepo.Setup(r => r.GetAllClients(It.IsAny<UserDataContract>())).ReturnsAsync(clients);

        var report = new ClientVersionReport(statusRepo.Object);
        ReportTestFixtures.Authenticate(report);

        var model = (ClientVersionReportModel)await report.ExecuteAsync(new ClientVersionParameters
        {
            From = from,
            To = to
        });

        Assert.That(model.Sessions, Has.Count.EqualTo(3));
        Assert.That(model.Sessions.Count(s => s.IsMultiVersion), Is.EqualTo(2));
        Assert.That(model.MultiVersionApplications, Has.Count.EqualTo(1));
        Assert.That(model.MultiVersionApplications[0].ClientName, Is.EqualTo("Multi"));
        Assert.That(model.Summary.Single(s => s.Label == "Unique Clients").Value, Is.EqualTo("2"));
    }

    [Test]
    public async Task ClientVersionReport_ExecuteAsync_NoOverlappingSessions_ReturnsEmptySessionList()
    {
        var (from, to) = ReportTestFixtures.DefaultRange();
        var clients = new List<ClientStatusBusinessEntity>
        {
            ReportTestFixtures.CreateStatus(
                "Stale",
                null,
                ReportTestFixtures.CreateSession(
                    startTimeUtc: to.AddDays(-30),
                    lastSeen: to.AddDays(-20)))
        };

        var statusRepo = new Mock<IClientStatusRepository>();
        statusRepo.Setup(r => r.GetAllClients(It.IsAny<UserDataContract>())).ReturnsAsync(clients);

        var report = new ClientVersionReport(statusRepo.Object);
        ReportTestFixtures.Authenticate(report);

        var model = (ClientVersionReportModel)await report.ExecuteAsync(new ClientVersionParameters
        {
            From = from,
            To = to
        });

        Assert.That(model.Sessions, Is.Empty);
        Assert.That(model.Summary.Single(s => s.Label == "Unique Clients").Value, Is.EqualTo("0"));
    }

    [Test]
    public async Task SettingHistoryReport_ExecuteAsync_ReturnsHistoryRows()
    {
        var client = ReportTestFixtures.CreateClient("MyApp", null, ReportTestFixtures.CreateSetting("Port", "443"));

        var history = new List<SettingValueBusinessEntity>
        {
            new()
            {
                ClientId = client.Id,
                SettingName = "Port",
                ChangedAt = DateTime.UtcNow.AddDays(-2),
                ChangedBy = "admin",
                Value = new StringSettingBusinessEntity("80"),
                ChangeMessage = "Initial"
            },
            new()
            {
                ClientId = client.Id,
                SettingName = "Port",
                ChangedAt = DateTime.UtcNow.AddDays(-1),
                ChangedBy = "admin",
                Value = new StringSettingBusinessEntity("443")
            }
        };

        var clientRepo = new Mock<ISettingClientRepository>();
        clientRepo.Setup(r => r.GetClient("MyApp", null)).ReturnsAsync(client);

        var historyRepo = new Mock<ISettingHistoryRepository>();
        historyRepo.Setup(r => r.GetAll(client.Id, "Port")).ReturnsAsync(history);

        var report = new SettingHistoryReport(clientRepo.Object, historyRepo.Object);
        ReportTestFixtures.Authenticate(report);

        var model = (SettingHistoryReportModel)await report.ExecuteAsync(new SettingHistoryParameters
        {
            ClientName = "MyApp",
            SettingName = "Port"
        });

        Assert.That(model.SettingName, Is.EqualTo("Port"));
        Assert.That(model.Rows, Has.Count.EqualTo(2));
        Assert.That(model.Summary.Single(s => s.Label == "History Entries").Value, Is.EqualTo("2"));
        Assert.That(model.IsSecret, Is.False);
    }

    [Test]
    public void SettingHistoryReport_ExecuteAsync_ThrowsWhenSettingMissing()
    {
        var client = ReportTestFixtures.CreateClient("MyApp", null, ReportTestFixtures.CreateSetting("Port", "443"));

        var clientRepo = new Mock<ISettingClientRepository>();
        clientRepo.Setup(r => r.GetClient("MyApp", null)).ReturnsAsync(client);

        var historyRepo = new Mock<ISettingHistoryRepository>();

        var report = new SettingHistoryReport(clientRepo.Object, historyRepo.Object);
        ReportTestFixtures.Authenticate(report);

        Assert.ThrowsAsync<KeyNotFoundException>(() => report.ExecuteAsync(new SettingHistoryParameters
        {
            ClientName = "MyApp",
            SettingName = "Missing"
        }));

        historyRepo.Verify(r => r.GetAll(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }
}
