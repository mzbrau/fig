using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fig.Api.Constants;
using Fig.Contracts.Authentication;
using Fig.Contracts.EventHistory;
using Fig.Contracts.Settings;
using Fig.Contracts.Status;
using Fig.Integration.Test.Api.TestSettings;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

[TestFixture]
public class EventsTests : IntegrationTestBase
{
    [SetUp]
    public async Task Setup()
    {
        await DeleteAllClients();
        await ResetUsers();
    }

    [TearDown]
    public async Task TearDown()
    {
        await DeleteAllClients();
        await ResetUsers();
    }

    private const string TestUser = "TestUser";

    [Test]
    public async Task ShallLogInitialRegistrationEvents()
    {
        var startTime = DateTime.UtcNow;
        var settings = await RegisterSettings<ThreeSettings>();
        var endTime = DateTime.UtcNow;
        var result = await GetEvents(startTime, endTime);

        VerifySingleEvent(result, EventMessage.InitialRegistration, settings.ClientName);
    }

    [Test]
    public async Task ShallLogNoChangeRegistrationEvents()
    {
        var settings = await RegisterSettings<ThreeSettings>();
        var startTime = DateTime.UtcNow;
        await RegisterSettings<ThreeSettings>();
        var endTime = DateTime.UtcNow;
        var result = await GetEvents(startTime, endTime);

        VerifySingleEvent(result, EventMessage.RegistrationNoChange, settings.ClientName);
    }

    [Test]
    public async Task ShallLogDefinitionChangedRegistrationEvents()
    {
        var settings = await RegisterSettings<ClientXWithTwoSettings>();
        var startTime = DateTime.UtcNow;
        await RegisterSettings<ClientXWithThreeSettings>();
        var endTime = DateTime.UtcNow;
        var result = await GetEvents(startTime, endTime);

        VerifySingleEvent(result, EventMessage.RegistrationWithChange, settings.ClientName);
    }

    [Test]
    public async Task ShallLogSettingValueUpdatedEvents()
    {
        var settings = await RegisterSettings<ThreeSettings>();
        var originalValues = await GetSettingsForClient(settings.ClientName, settings.ClientSecret);
        var originalValue = originalValues.FirstOrDefault(a => a.Name == nameof(settings.AStringSetting)).Value;
        var newValue = "some new value";
        var settingsToUpdate = new List<SettingDataContract>
        {
            new()
            {
                Name = nameof(settings.AStringSetting),
                Value = newValue
            }
        };
        var startTime = DateTime.UtcNow;
        await SetSettings(settings.ClientName, settingsToUpdate);
        var endTime = DateTime.UtcNow;
        var result = await GetEvents(startTime, endTime);

        var updatedEvent = VerifySingleEvent(result, EventMessage.SettingValueUpdated, settings.ClientName);
        Assert.That(updatedEvent.SettingName, Is.EqualTo(nameof(settings.AStringSetting)));
        Assert.That(updatedEvent.OriginalValue, Is.EqualTo(originalValue));
        Assert.That(updatedEvent.NewValue, Is.EqualTo(newValue));
        Assert.That(updatedEvent.AuthenticatedUser, Is.EqualTo(UserName));
    }

    [Test]
    public async Task ShallLogClientDeletedEvents()
    {
        var settings = await RegisterSettings<ThreeSettings>();

        var startTime = DateTime.UtcNow;
        await DeleteClient(settings.ClientName);
        var endTime = DateTime.UtcNow;
        var result = await GetEvents(startTime, endTime);

        VerifySingleEvent(result, EventMessage.ClientDeleted, settings.ClientName);
    }

    [Test]
    public async Task ShallLogClientInstanceCreatedEvents()
    {
        var settings = await RegisterSettings<ThreeSettings>();
        var settingsToUpdate = new List<SettingDataContract>
        {
            new()
            {
                Name = nameof(settings.AStringSetting),
                Value = "newValue"
            }
        };

        var instanceName = "instance1";
        var startTime = DateTime.UtcNow;
        await SetSettings(settings.ClientName, settingsToUpdate, instanceName);
        var endTime = DateTime.UtcNow;
        var result = await GetEvents(startTime, endTime);

        Assert.That(result.Events.Count(), Is.EqualTo(2));
        var firstEvent = result.Events.FirstOrDefault(a => a.EventType == EventMessage.ClientInstanceCreated);
        Assert.That(firstEvent, Is.Not.Null, "Instance creation should be logged");
        Assert.That(firstEvent.ClientName, Is.EqualTo(settings.ClientName));
        Assert.That(firstEvent.Instance, Is.EqualTo(instanceName));
    }

    [Test]
    public async Task ShallLogDynamicSettingVerificationRunEvents()
    {
        var settings = await RegisterSettings<SettingsWithVerifications>();
        var client = await GetClient(settings);

        var verification = client.DynamicVerifications.Single();
        var startTime = DateTime.UtcNow;
        await RunVerification(settings.ClientName, verification.Name);
        var endTime = DateTime.UtcNow;

        var result = await GetEvents(startTime, endTime);
        VerifySingleEvent(result, EventMessage.SettingVerificationRun, settings.ClientName);
    }

    [Test]
    public async Task ShallLogPluginSettingVerificationRunEvents()
    {
        var settings = await RegisterSettings<SettingsWithVerifications>();
        var client = await GetClient(settings);

        var verification = client.PluginVerifications.Single();
        var startTime = DateTime.UtcNow;
        await RunVerification(settings.ClientName, verification.Name);
        var endTime = DateTime.UtcNow;

        var result = await GetEvents(startTime, endTime);
        VerifySingleEvent(result, EventMessage.SettingVerificationRun, settings.ClientName);
    }

    [Test]
    public async Task ShallLogSettingSettingsReadEvents()
    {
        var settings = await RegisterSettings<ThreeSettings>();

        var startTime = DateTime.UtcNow;
        await GetSettingsForClient(settings.ClientName, settings.ClientSecret);
        var endTime = DateTime.UtcNow;
        var result = await GetEvents(startTime, endTime);

        VerifySingleEvent(result, EventMessage.SettingsRead, settings.ClientName);
    }

    [Test]
    public async Task ShallLogLoginEvents()
    {
        var startTime = DateTime.UtcNow;
        await Login(UserName, "admin");
        var endTime = DateTime.UtcNow;

        var result = await GetEvents(startTime, endTime);

        VerifySingleEvent(result, EventMessage.Login, authenticatedUser: UserName);
    }

    [Test]
    public async Task ShallLogUserCreatedEvents()
    {
        var startTime = DateTime.UtcNow;
        var user = await CreateUser();
        var endTime = DateTime.UtcNow;

        var result = await GetEvents(startTime, endTime);

        var updatedEvent = VerifySingleEvent(result, EventMessage.UserCreated, authenticatedUser: UserName);
        Assert.That(updatedEvent.NewValue,
            Is.EqualTo($"{user.Username} ({user.FirstName} {user.LastName}) Role:{user.Role}"));
        Assert.That(updatedEvent.OriginalValue, Is.Null);
    }

    [Test]
    public async Task ShallLogPasswordChangedEvents()
    {
        var user = await CreateUser();
        var update = new UpdateUserRequestDataContract
        {
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = Role.Administrator,
            Password = "some new password"
        };

        var matchingUser = (await GetUsers()).FirstOrDefault(a => a.Username == user.Username);
        Assert.That(matchingUser, Is.Not.Null, "User should have been created so we can use the id");

        var startTime = DateTime.UtcNow;
        await UpdateUser(matchingUser.Id, update);
        var endTime = DateTime.UtcNow;

        var result = await GetEvents(startTime, endTime);

        var updatedEvent = VerifySingleEvent(result, EventMessage.PasswordUpdated, authenticatedUser: UserName);
        var userDetails = $"{user.Username} ({user.FirstName} {user.LastName}) Role:{user.Role}";
        Assert.That(updatedEvent.NewValue, Is.EqualTo(userDetails));
        Assert.That(updatedEvent.OriginalValue, Is.EqualTo(userDetails));
    }

    [Test]
    public async Task ShallLogUserUpdatedEvents()
    {
        var user = await CreateUser();
        var update = new UpdateUserRequestDataContract
        {
            Username = user.Username,
            FirstName = "new-first-name",
            LastName = "new-last-name",
            Role = Role.Administrator
        };

        var matchingUser = (await GetUsers()).FirstOrDefault(a => a.Username == user.Username);
        Assert.That(matchingUser, Is.Not.Null, "User should have been created so we can use the id");

        var startTime = DateTime.UtcNow;
        await UpdateUser(matchingUser.Id, update);
        var endTime = DateTime.UtcNow;

        var result = await GetEvents(startTime, endTime);

        var updatedEvent = VerifySingleEvent(result, EventMessage.UserUpdated, authenticatedUser: UserName);
        Assert.That(updatedEvent.NewValue,
            Is.EqualTo($"{update.Username} ({update.FirstName} {update.LastName}) Role:{update.Role}"));
        Assert.That(updatedEvent.OriginalValue,
            Is.EqualTo($"{user.Username} ({user.FirstName} {user.LastName}) Role:{user.Role}"));
    }

    [Test]
    public async Task ShallLogUserDeletedEvents()
    {
        var user = await CreateUser();

        var users = await GetUsers();
        var createdUser = users.FirstOrDefault(a => a.Username == TestUser);
        Assert.That(createdUser, Is.Not.Null);

        var startTime = DateTime.UtcNow;
        await DeleteUser(createdUser.Id);
        var endTime = DateTime.UtcNow;
        var result = await GetEvents(startTime, endTime);

        var updatedEvent = VerifySingleEvent(result, EventMessage.UserDeleted, authenticatedUser: UserName);
        Assert.That(updatedEvent.OriginalValue,
            Is.EqualTo($"{user.Username} ({user.FirstName} {user.LastName}) Role:{user.Role}"));
        Assert.That(updatedEvent.NewValue, Is.Null);
    }

    [Test]
    public async Task ShallOnlyReturnEventsWithinTheTimeRange()
    {
        await RegisterSettings<ClientA>();
        var startTime = DateTime.UtcNow;
        var settings = await RegisterSettings<ThreeSettings>();
        var endTime = DateTime.UtcNow;
        var result = await GetEvents(startTime, endTime);

        Assert.That(result.Events.Count(), Is.EqualTo(1));
        var firstEvent = result.Events.First();
        Assert.That(firstEvent.ClientName, Is.EqualTo(settings.ClientName));
    }

    [Test]
    public async Task ShallLogNewSessionEventLog()
    {
        var settings = await RegisterSettings<ThreeSettings>();

        var clientStatus = new StatusRequestDataContract
        {
            UptimeSeconds = 500,
            LastSettingUpdate = DateTime.UtcNow,
            PollIntervalMs = 5000,
            LiveReload = true,
            RunSessionId = Guid.NewGuid()
        };
        var startTime = DateTime.UtcNow;
        await GetStatus(settings.ClientName, settings.ClientSecret, clientStatus);
        var endTime = DateTime.UtcNow;
        var result = await GetEvents(startTime, endTime);

        Assert.That(result.Events.Count(), Is.EqualTo(1));
        var firstEvent = result.Events.First();
        Assert.That(firstEvent.EventType, Is.EqualTo(EventMessage.NewSession));
    }

    [Test]
    public async Task ShallLogExpiredSessionEventLog()
    {
        var settings = await RegisterSettings<ThreeSettings>();

        var clientStatus1 = new StatusRequestDataContract
        {
            UptimeSeconds = 500,
            LastSettingUpdate = DateTime.UtcNow,
            PollIntervalMs = 50,
            LiveReload = true,
            RunSessionId = Guid.NewGuid()
        };

        await GetStatus(settings.ClientName, settings.ClientSecret, clientStatus1);

        await Task.Delay(TimeSpan.FromMilliseconds(200));

        var clientStatus2 = new StatusRequestDataContract
        {
            UptimeSeconds = 600,
            LastSettingUpdate = DateTime.UtcNow,
            PollIntervalMs = 30000,
            LiveReload = true,
            RunSessionId = Guid.NewGuid()
        };

        var startTime = DateTime.UtcNow;
        await GetStatus(settings.ClientName, settings.ClientSecret, clientStatus2);
        var endTime = DateTime.UtcNow;

        var result = await GetEvents(startTime, endTime);

        Assert.That(result.Events.Count(), Is.EqualTo(2));
        var expiredSessionEvent = result.Events.FirstOrDefault(a => a.EventType == EventMessage.ExpiredSession);
        Assert.That(expiredSessionEvent, Is.Not.Null);
    }

    private EventLogDataContract VerifySingleEvent(EventLogCollectionDataContract result, string eventType,
        string? clientName = null, string? authenticatedUser = null)
    {
        Assert.That(result.Events.Count(), Is.EqualTo(1));
        var firstEvent = result.Events.First();
        Assert.That(firstEvent.EventType, Is.EqualTo(eventType));
        if (clientName != null)
            Assert.That(firstEvent.ClientName, Is.EqualTo(clientName));

        if (authenticatedUser != null)
            Assert.That(firstEvent.AuthenticatedUser, Is.EqualTo(authenticatedUser));

        return firstEvent;
    }

    private async Task<RegisterUserRequestDataContract> CreateUser()
    {
        var user = new RegisterUserRequestDataContract
        {
            Username = TestUser,
            FirstName = "First",
            LastName = "Last",
            Password = "this is a long and complex password",
            Role = Role.Administrator
        };

        await CreateUser(user);

        return user;
    }
}