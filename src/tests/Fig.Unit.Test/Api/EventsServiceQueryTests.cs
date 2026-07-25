using Fig.Api.Converters;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Services;
using Fig.Contracts.Authentication;
using Fig.Contracts.EventHistory;
using Fig.Datalayer.BusinessEntities;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class EventsServiceQueryTests
{
    [Test]
    public async Task GetEventLogs_ForwardsFiltersToRepository()
    {
        EventLogQuery? captured = null;
        var repo = new Mock<IEventLogRepository>();
        repo.Setup(a => a.GetEarliestEntry()).ReturnsAsync(DateTime.UtcNow.AddDays(-90));
        repo.Setup(a => a.QueryLogs(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<EventLogQuery>(),
                It.IsAny<bool>(),
                It.IsAny<UserDataContract>()))
            .Callback<DateTime, DateTime, EventLogQuery, bool, UserDataContract>(
                (_, _, query, _, _) => captured = query)
            .ReturnsAsync(new List<EventLogBusinessEntity>
            {
                new()
                {
                    Timestamp = DateTime.UtcNow,
                    EventType = "Login",
                    AuthenticatedUser = "alice",
                    ClientName = "MyApp"
                }
            });

        var converter = new Mock<IEventsConverter>();
        converter.Setup(a => a.Convert(It.IsAny<EventLogBusinessEntity>()))
            .Returns((EventLogBusinessEntity e) => new EventLogDataContract(
                e.Timestamp, e.ClientName, e.Instance, e.SettingName, e.EventType,
                e.OriginalValue, e.NewValue, e.AuthenticatedUser, e.Message, e.IpAddress, e.Hostname));

        var service = new EventsService(
            repo.Object,
            converter.Object,
            Mock.Of<ILogger<EventsService>>());
        service.SetAuthenticatedUser(new UserDataContract(
            Guid.NewGuid(),
            "admin",
            "Admin",
            "User",
            Role.Administrator,
            ".*",
            [],
            false));

        var query = new EventLogQuery
        {
            ClientName = "MyApp",
            AuthenticatedUser = "alice",
            EventTypes = ["Login"],
            SearchText = "timeout",
            MaxResults = 50
        };

        var result = await service.GetEventLogs(
            DateTime.UtcNow.AddDays(-7),
            DateTime.UtcNow,
            query);

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.ClientName, Is.EqualTo("MyApp"));
        Assert.That(captured.AuthenticatedUser, Is.EqualTo("alice"));
        Assert.That(captured.EventTypes, Is.EquivalentTo(new[] { "Login" }));
        Assert.That(captured.SearchText, Is.EqualTo("timeout"));
        Assert.That(captured.MaxResults, Is.EqualTo(50));
        Assert.That(result.Events.Count(), Is.EqualTo(1));

        repo.Verify(a => a.QueryLogs(
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<EventLogQuery>(),
            false,
            It.IsAny<UserDataContract>()), Times.Once);
    }

    [Test]
    public async Task GetEventLogs_WithoutQuery_UsesEmptyFiltersAndAdminUnrestrictedFalse()
    {
        var repo = new Mock<IEventLogRepository>();
        repo.Setup(a => a.GetEarliestEntry()).ReturnsAsync(DateTime.UtcNow.AddDays(-30));
        repo.Setup(a => a.QueryLogs(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<EventLogQuery>(),
                It.IsAny<bool>(),
                It.IsAny<UserDataContract>()))
            .ReturnsAsync(new List<EventLogBusinessEntity>());

        var service = new EventsService(
            repo.Object,
            Mock.Of<IEventsConverter>(),
            Mock.Of<ILogger<EventsService>>());
        service.SetAuthenticatedUser(new UserDataContract(
            Guid.NewGuid(),
            "admin",
            "Admin",
            "User",
            Role.Administrator,
            ".*",
            [],
            false));

        await service.GetEventLogs(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);

        repo.Verify(a => a.QueryLogs(
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.Is<EventLogQuery>(q =>
                q.ClientName == null &&
                q.EventTypes == null &&
                q.SearchText == null &&
                q.MaxResults == null),
            false,
            It.IsAny<UserDataContract>()), Times.Once);
    }

    [Test]
    public async Task GetEventLogs_NonAdmin_RequestsOnlyUnrestricted()
    {
        var repo = new Mock<IEventLogRepository>();
        repo.Setup(a => a.GetEarliestEntry()).ReturnsAsync(DateTime.UtcNow.AddDays(-30));
        repo.Setup(a => a.QueryLogs(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<EventLogQuery>(),
                It.IsAny<bool>(),
                It.IsAny<UserDataContract>()))
            .ReturnsAsync(new List<EventLogBusinessEntity>());

        var service = new EventsService(
            repo.Object,
            Mock.Of<IEventsConverter>(),
            Mock.Of<ILogger<EventsService>>());
        service.SetAuthenticatedUser(new UserDataContract(
            Guid.NewGuid(),
            "viewer",
            "View",
            "User",
            Role.User,
            ".*",
            [],
            false));

        await service.GetEventLogs(
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow,
            new EventLogQuery { ClientName = "App" });

        repo.Verify(a => a.QueryLogs(
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.Is<EventLogQuery>(q => q.ClientName == "App"),
            true,
            It.IsAny<UserDataContract>()), Times.Once);
    }
}
