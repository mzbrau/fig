using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fig.Api.Constants;
using Fig.Common.Constants;
using Fig.Client.Abstractions.Data;
using Fig.Contracts.Authentication;
using Fig.Contracts.CustomActions;
using Fig.Contracts.EventHistory;
using Fig.Contracts.Health;
using Fig.Contracts.ImportExport;
using Fig.Contracts.Settings;
using Fig.Contracts.WebHook;
using Fig.Integration.Test.Utils;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

[TestFixture]
public class EventsTests : IntegrationTestBase
{
    private const string TestUser = "TestUser";

    [Test]
    public async Task ShallLogInitialRegistrationEvents()
    {
        var secret = Guid.NewGuid().ToString();
        var startTime = DateTime.UtcNow;
        var settings = await RegisterSettings<ThreeSettings>(secret);
        var endTime = DateTime.UtcNow;
        var result = await GetEvents(startTime, endTime);

        VerifySingleEvent(result, EventMessage.InitialRegistration, settings.ClientName, checkPointEvent: false);
    }

    [Test]
    public async Task ShallLogNoChangeRegistrationEvents()
    {
        var secret = Guid.NewGuid().ToString();
        var settings = await RegisterSettings<ClientA>(secret);
        var startTime = DateTime.UtcNow;
        await RegisterSettings<ClientA>(secret);
        var endTime = DateTime.UtcNow;
        var result = await GetEvents(startTime, endTime);

        VerifySingleEvent(result, EventMessage.RegistrationNoChange, settings.ClientName);
    }

    [Test]
    public async Task ShallLogDefinitionChangedRegistrationEvents()
    {
        var secret = Guid.NewGuid().ToString();
        var settings = await RegisterSettings<ClientXWithTwoSettings>(secret);
        var startTime = DateTime.UtcNow;
        await RegisterSettings<ClientXWithThreeSettings>(secret);
        var endTime = DateTime.UtcNow;
        var result = await GetEvents(startTime, endTime);

        VerifySingleEvent(result, EventMessage.RegistrationWithChange, settings.ClientName, checkPointEvent: false);
    }

    [Test]
    public async Task ShallLogSettingValueUpdatedEvents()
    {
        var secret = Guid.NewGuid().ToString();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        var originalValues = await GetSettingsForClient(settings.ClientName, secret);
        var originalValue = originalValues.First(a => a.Name == nameof(settings.AStringSetting)).Value?.GetValue();
        const string newValue = "some new value";
        const string message = "because...";
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract(newValue))
        };
        var startTime = DateTime.UtcNow;
        await SetSettings(settings.ClientName, settingsToUpdate, message: message);
        var endTime = DateTime.UtcNow;
        var result = await GetEvents(startTime, endTime);

        var updatedEvent = VerifySingleEvent(result, EventMessage.SettingValueUpdated, settings.ClientName, checkPointEvent: false);
        Assert.That(updatedEvent.SettingName, Is.EqualTo(nameof(settings.AStringSetting)));
        Assert.That(updatedEvent.OriginalValue, Is.EqualTo(originalValue));
        Assert.That(updatedEvent.NewValue, Is.EqualTo(newValue));
        Assert.That(updatedEvent.AuthenticatedUser, Is.EqualTo(UserName));
        Assert.That(updatedEvent.Message, Is.EqualTo(message));
    }
    
    [Test]
    public async Task ShallMaskSecretValueWhenLoggingSettingsUpdatedEvent()
    {
        var secret = Guid.NewGuid().ToString();
        var settings = await RegisterSettings<SecretSettings>(secret);
        const string newValue = "some new value";
        const string message = "because...";
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.SecretNoDefault), new StringSettingDataContract(newValue))
        };
        var startTime = DateTime.UtcNow;
        await SetSettings(settings.ClientName, settingsToUpdate, message: message);
        var endTime = DateTime.UtcNow;
        var result = await GetEvents(startTime, endTime);

        var updatedEvent = VerifySingleEvent(result, EventMessage.SettingValueUpdated, settings.ClientName, checkPointEvent: false);
        Assert.That(updatedEvent.SettingName, Is.EqualTo(nameof(settings.SecretNoDefault)));
        Assert.That(updatedEvent.OriginalValue, Is.EqualTo(SecretConstants.SecretPlaceholder));
        Assert.That(updatedEvent.NewValue, Is.EqualTo(SecretConstants.SecretPlaceholder));
        Assert.That(updatedEvent.AuthenticatedUser, Is.EqualTo(UserName));
        Assert.That(updatedEvent.Message, Is.EqualTo(message));
    }
    
    [Test]
    public async Task ShallMaskSecretValueInDataGridWhenLoggingSettingsUpdatedEvent()
    {
        var secret = Guid.NewGuid().ToString();
        var settings = await RegisterSettings<SecretSettings>(secret);
        const string message = "because...";
        var startTime = DateTime.UtcNow;
        await SetSettings(settings.ClientName, new List<SettingDataContract>()
        {
            new(nameof(settings.LoginsWithDefault), new DataGridSettingDataContract(new List<Dictionary<string, object?>>()
            {
                new()
                {
                    { nameof(Fig.Test.Common.TestSettings.Login.Username), "user1" },
                    { nameof(Fig.Test.Common.TestSettings.Login.Password), "my very secret password" },
                    { nameof(Fig.Test.Common.TestSettings.Login.AnotherSecret), "snap" }
                }
            }))
        }, message: message);
        var endTime = DateTime.UtcNow;
        var result = await GetEvents(startTime, endTime);

        var updatedEvent = VerifySingleEvent(result, EventMessage.SettingValueUpdated, settings.ClientName, checkPointEvent: false);
        Assert.That(updatedEvent.SettingName, Is.EqualTo(nameof(settings.LoginsWithDefault)));
        Assert.That(updatedEvent.OriginalValue?.Replace("\r", string.Empty).Replace("\n", string.Empty), Is.EqualTo($"myUser,{SecretConstants.SecretPlaceholder},{SecretConstants.SecretPlaceholder}myUser2,{SecretConstants.SecretPlaceholder},{SecretConstants.SecretPlaceholder}"));
        Assert.That(updatedEvent.NewValue?.Replace("\r", string.Empty).Replace("\n", string.Empty), Is.EqualTo($"user1,{SecretConstants.SecretPlaceholder},{SecretConstants.SecretPlaceholder}"));
        Assert.That(updatedEvent.AuthenticatedUser, Is.EqualTo(UserName));
        Assert.That(updatedEvent.Message, Is.EqualTo(message));
    }
    
    [Test]
    public async Task ShallLogForEachSettingValueUpdate()
    {
        var secret = Guid.NewGuid().ToString();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        const string message = "a reason";
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract("stuff")),
            new(nameof(settings.ABoolSetting), new BoolSettingDataContract(false)),
            new(nameof(settings.AnIntSetting), new IntSettingDataContract(88)),
        };
        var startTime = DateTime.UtcNow;
        await SetSettings(settings.ClientName, settingsToUpdate, message: message);
        var endTime = DateTime.UtcNow;
        var result = await GetEvents(startTime, endTime);
        
        var nonCheckPointEvents = result.Events.RemoveCheckPointEvents();
        
        Assert.That(nonCheckPointEvents.Count, Is.EqualTo(3));
        var events = nonCheckPointEvents.OrderBy(a => a.SettingName).ToList();
        Assert.That(events[0].SettingName, Is.EqualTo(nameof(settings.ABoolSetting)));
        Assert.That(events[1].SettingName, Is.EqualTo(nameof(settings.AnIntSetting)));
        Assert.That(events[2].SettingName, Is.EqualTo(nameof(settings.AStringSetting)));

        foreach (var eventItem in events.Where(a => a.SettingName is not null))
            Assert.That(eventItem.Message, Is.EqualTo(message));
    }
    
    [Test]
    public async Task ShallNotLogIfValueIsUpdatedToTheSameValue()
    {
        var secret = Guid.NewGuid().ToString();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        const string message = "a reason";
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract("bla")),
            new(nameof(settings.ABoolSetting), new BoolSettingDataContract(true)),
            new(nameof(settings.AnIntSetting), new IntSettingDataContract(77)),
        };
        await SetSettings(settings.ClientName, settingsToUpdate, message: message);
        
        var startTime = DateTime.UtcNow;
        await SetSettings(settings.ClientName, settingsToUpdate, message: message);
        var endTime = DateTime.UtcNow;
        var result = await GetEvents(startTime, endTime);
        
        var nonCheckPointEvents = result.Events.RemoveCheckPointEvents();
        Assert.That(nonCheckPointEvents.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task ShallLogClientDeletedEvents()
    {
        var secret = Guid.NewGuid().ToString();
        var settings = await RegisterSettings<ThreeSettings>(secret);

        var startTime = DateTime.UtcNow;
        await DeleteClient(settings.ClientName);
        var endTime = DateTime.UtcNow;
        var result = await GetEvents(startTime, endTime);

        VerifySingleEvent(result, EventMessage.ClientDeleted, settings.ClientName);
    }

    [Test]
    public async Task ShallLogClientInstanceCreatedEvents()
    {
        var secret = Guid.NewGuid().ToString();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract("newValue"))
        };

        var instanceName = "instance1";
        var startTime = DateTime.UtcNow;
        await SetSettings(settings.ClientName, settingsToUpdate, instanceName);
        var endTime = DateTime.UtcNow;
        var result = await GetEvents(startTime, endTime);

        var nonCheckPointEvents = result.Events.RemoveCheckPointEvents();
        
        Assert.That(nonCheckPointEvents.Count, Is.EqualTo(2));
        var firstEvent = result.Events.First(a => a.EventType == EventMessage.ClientInstanceCreated);
        Assert.That(firstEvent, Is.Not.Null, "Instance creation should be logged");
        Assert.That(firstEvent.ClientName, Is.EqualTo(settings.ClientName));
        Assert.That(firstEvent.Instance, Is.EqualTo(instanceName));
    }

    [Test]
    public async Task ShallLogSettingSettingsReadEvents()
    {
        var secret = Guid.NewGuid().ToString();
        var settings = await RegisterSettings<ThreeSettings>(secret);

        var startTime = DateTime.UtcNow;
        await GetSettingsForClient(settings.ClientName, secret);
        var endTime = DateTime.UtcNow;
        var result = await GetEvents(startTime, endTime);

        VerifySingleEvent(result, EventMessage.SettingsRead, settings.ClientName);
    }

    [Test]
    public async Task ShallLogLoginEvents()
    {
        var startTime = DateTime.UtcNow;
        await Login();
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
            Is.EqualTo($"{user.Username} ({user.FirstName} {user.LastName}) Role:{user.Role} Classifications:{string.Join(",", user.AllowedClassifications)}"));
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
            Password = "some new password",
            AllowedClassifications = Enum.GetValues(typeof(Classification)).Cast<Classification>().ToList()
        };

        var matchingUser = (await GetUsers()).FirstOrDefault(a => a.Username == user.Username);
        Assert.That(matchingUser, Is.Not.Null, "User should have been created so we can use the id");

        var startTime = DateTime.UtcNow;
        await UpdateUser(matchingUser!.Id, update);
        var endTime = DateTime.UtcNow;

        var result = await GetEvents(startTime, endTime);

        var updatedEvent = VerifySingleEvent(result, EventMessage.PasswordUpdated, authenticatedUser: UserName);
        var userDetails = $"{user.Username} ({user.FirstName} {user.LastName}) Role:{user.Role} Classifications:{string.Join(",", user.AllowedClassifications)}";
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
            Role = Role.Administrator,
            AllowedClassifications = Enum.GetValues(typeof(Classification)).Cast<Classification>().ToList()
        };

        var matchingUser = (await GetUsers()).FirstOrDefault(a => a.Username == user.Username);
        Assert.That(matchingUser, Is.Not.Null, "User should have been created so we can use the id");

        var startTime = DateTime.UtcNow;
        await UpdateUser(matchingUser!.Id, update);
        var endTime = DateTime.UtcNow;

        var result = await GetEvents(startTime, endTime);

        var updatedEvent = VerifySingleEvent(result, EventMessage.UserUpdated, authenticatedUser: UserName);
        Assert.That(updatedEvent.NewValue,
            Is.EqualTo($"{update.Username} ({update.FirstName} {update.LastName}) Role:{update.Role} Classifications:{string.Join(",", update.AllowedClassifications)}"));
        Assert.That(updatedEvent.OriginalValue,
            Is.EqualTo($"{user.Username} ({user.FirstName} {user.LastName}) Role:{user.Role} Classifications:{string.Join(",", user.AllowedClassifications)}"));
    }

    [Test]
    public async Task ShallLogUserDeletedEvents()
    {
        var user = await CreateUser();

        var users = await GetUsers();
        var createdUser = users.FirstOrDefault(a => a.Username == TestUser);
        Assert.That(createdUser, Is.Not.Null);

        var startTime = DateTime.UtcNow;
        await DeleteUser(createdUser!.Id);
        var endTime = DateTime.UtcNow;
        var result = await GetEvents(startTime, endTime);

        var updatedEvent = VerifySingleEvent(result, EventMessage.UserDeleted, authenticatedUser: UserName);
        Assert.That(updatedEvent.OriginalValue,
            Is.EqualTo($"{user.Username} ({user.FirstName} {user.LastName}) Role:{user.Role} Classifications:{string.Join(",", user.AllowedClassifications)}"));
        Assert.That(updatedEvent.NewValue, Is.Null);
    }

    [Test]
    public async Task ShallOnlyReturnEventsWithinTheTimeRange()
    {
        var secret = Guid.NewGuid().ToString();
        await RegisterSettings<ClientA>(secret);
        var startTime = DateTime.UtcNow;
        var settings = await RegisterSettings<ThreeSettings>(Guid.NewGuid().ToString());
        var endTime = DateTime.UtcNow;
        var result = await GetEvents(startTime, endTime);
        
        var singleEvent = VerifySingleEvent(result, EventMessage.InitialRegistration);
        Assert.That(singleEvent.ClientName, Is.EqualTo(settings.ClientName));
    }

    [Test]
    public async Task ShallLogNewSessionEventLog()
    {
        var secret = Guid.NewGuid().ToString();
        var settings = await RegisterSettings<ThreeSettings>(secret);

        var clientStatus = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 5000, true);

        var startTime = DateTime.UtcNow;
        await GetStatus(settings.ClientName, secret, clientStatus);
        var endTime = DateTime.UtcNow;
        var result = await GetEvents(startTime, endTime);

        VerifySingleEvent(result, EventMessage.NewSession);
    }

    [Test]
    public async Task ShallLogExpiredSessionEventLog()
    {
        var secret = Guid.NewGuid().ToString();
        var settings = await RegisterSettings<ThreeSettings>(secret);

        var clientStatus1 = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 50, true);

        await GetStatus(settings.ClientName, secret, clientStatus1);

        await Task.Delay(TimeSpan.FromMilliseconds(200));

        var clientStatus2 = CreateStatusRequest(DateTime.UtcNow - TimeSpan.FromMilliseconds(600), DateTime.UtcNow, 30000, true);

        var startTime = DateTime.UtcNow;
        await GetStatus(settings.ClientName, secret, clientStatus2);
        var endTime = DateTime.UtcNow;

        var result = await GetEvents(startTime, endTime);

        var nonCheckPointEvents = result.Events.RemoveCheckPointEvents();
        Assert.That(nonCheckPointEvents.Count, Is.EqualTo(2));
        var expiredSessionEvent = nonCheckPointEvents.FirstOrDefault(a => a.EventType == EventMessage.ExpiredSession);
        Assert.That(expiredSessionEvent, Is.Not.Null);
    }

    [Test]
    public async Task ShallLogDataExportedEventLog()
    {
        var secret = Guid.NewGuid().ToString();
        await RegisterSettings<ThreeSettings>(secret);

        var startTime = DateTime.UtcNow;
        await ExportData();
        var endTime = DateTime.UtcNow;

        var result = await GetEvents(startTime, endTime);
        
        VerifySingleEvent(result, EventMessage.DataExported, authenticatedUser: UserName);
    }

    [Test]
    public async Task ShallLogDataImportedEventLog()
    {
        var result = await PerformImport(ImportType.ClearAndImport);

        var nonCheckPointEvents = result.Events.RemoveCheckPointEvents();
        Assert.That(nonCheckPointEvents.Count, Is.EqualTo(4));
        var dataImportedEvent = nonCheckPointEvents.FirstOrDefault(a => a.EventType == EventMessage.DataImported);
        Assert.That(dataImportedEvent, Is.Not.Null);
    }

    [Test]
    public async Task ShallNotLogDataImportedEventLogIfNoClientsWereImported()
    {
        var result = await PerformImport(ImportType.AddNew);

        var nonCheckPointEvents = result.Events.RemoveCheckPointEvents();
        Assert.That(nonCheckPointEvents.Count, Is.EqualTo(1));
        var dataImportedEvent = nonCheckPointEvents.FirstOrDefault(a => a.EventType == EventMessage.DataImported);
        Assert.That(dataImportedEvent, Is.Null);
    }

    [Test]
    public async Task ShallLogDataImportStartedEventLog()
    {
        var result = await PerformImport(ImportType.AddNew);

        VerifySingleEvent(result, EventMessage.DataImportStarted, authenticatedUser: UserName);
    }

    [Test]
    public async Task ShallLogClientImportedEventLog()
    {
        var result = await PerformImport(ImportType.ReplaceExisting);

        var nonCheckPointEvents = result.Events.RemoveCheckPointEvents();
        Assert.That(nonCheckPointEvents.Count(), Is.EqualTo(4));
        var dataImportStartingEvent = nonCheckPointEvents.FirstOrDefault(a => a.EventType == EventMessage.DataImportStarted);
        Assert.That(dataImportStartingEvent, Is.Not.Null);
    }

    [Test]
    public async Task ShallLogConfigurationChangedEventLog()
    {
        var startTime = DateTime.UtcNow;
        await SetConfiguration(CreateConfiguration(false));
        var endTime = DateTime.UtcNow;

        var result = await GetEvents(startTime, endTime);
        VerifySingleEvent(result, EventMessage.ConfigurationChanged, authenticatedUser: UserName);
    }

    [Test]
    public async Task ShallLogValueOnlyImport()
    {
        var allSettings = await RegisterSettings<AllSettingsAndTypes>();

        var data = await ExportValueOnlyData();

        const string updatedStringValue = "Update";
        const bool updateBoolValue = false;
        
        data.Clients.Single().Settings.First(a => a.Name == nameof(allSettings.StringSetting)).Value = updatedStringValue;
        data.Clients.Single().Settings.First(a => a.Name == nameof(allSettings.BoolSetting)).Value = updateBoolValue;

        var startTime = DateTime.UtcNow;
        await ImportValueOnlyData(data);
        var endTime = DateTime.UtcNow;
        
        var result = await GetEvents(startTime, endTime);
        
        var nonCheckPointEvents = result.Events.RemoveCheckPointEvents();
        Assert.That(nonCheckPointEvents.Count, Is.EqualTo(4));
        Assert.That(nonCheckPointEvents[0].EventType, Is.EqualTo(EventMessage.DataImported));
        Assert.That(nonCheckPointEvents[1].EventType, Is.EqualTo(EventMessage.SettingValueUpdated));
        Assert.That(nonCheckPointEvents[2].EventType, Is.EqualTo(EventMessage.SettingValueUpdated));
        Assert.That(nonCheckPointEvents[3].EventType, Is.EqualTo(EventMessage.DataImportStarted));
    }

    [Test]
    public async Task ShallLogDeferredValueOnlyImport()
    {
        var allSettings = await RegisterSettings<AllSettingsAndTypes>();

        var data = await ExportValueOnlyData();

        await DeleteClient(allSettings.ClientName);
        
        var startTime = DateTime.UtcNow;
        await ImportValueOnlyData(data);
        var endTime = DateTime.UtcNow;
        
        var result = await GetEvents(startTime, endTime);
        var nonCheckPointEvents = result.Events.RemoveCheckPointEvents();
        
        Assert.That(nonCheckPointEvents.Count, Is.EqualTo(2));
        Assert.That(nonCheckPointEvents.Last().EventType, Is.EqualTo(EventMessage.DataImportStarted));
        Assert.That(nonCheckPointEvents.First().EventType, Is.EqualTo(EventMessage.DeferredImportRegistered));
    }

    [Test]
    public async Task ShallLogWhenDeferredImportIsApplied()
    {
        var allSettings = await RegisterSettings<AllSettingsAndTypes>();

        var data = await ExportValueOnlyData();

        const string updatedStringValue = "Update";
        const bool updateBoolValue = false;
        
        data.Clients.Single().Settings.First(a => a.Name == nameof(allSettings.StringSetting)).Value = updatedStringValue;
        data.Clients.Single().Settings.First(a => a.Name == nameof(allSettings.BoolSetting)).Value = updateBoolValue;

        await DeleteClient(allSettings.ClientName);
        await ImportValueOnlyData(data);

        var startTime = DateTime.UtcNow;
        await RegisterSettings<AllSettingsAndTypes>();
        var endTime = DateTime.UtcNow;
        
        var result = await GetEvents(startTime, endTime);
        
        var events = result.Events.RemoveCheckPointEvents();
        
        Assert.That(events.Count, Is.EqualTo(4));
        Assert.That(events[0].EventType, Is.EqualTo(EventMessage.DeferredImportApplied));
        Assert.That(events[1].EventType, Is.EqualTo(EventMessage.SettingValueUpdated));
        Assert.That(events[2].EventType, Is.EqualTo(EventMessage.SettingValueUpdated));
        Assert.That(events[3].EventType, Is.EqualTo(EventMessage.InitialRegistration));
    }

    [Test]
    public async Task ShallLogValueOnlyExport()
    {
        await RegisterSettings<AllSettingsAndTypes>();

        var startTime = DateTime.UtcNow;
        await ExportValueOnlyData();
        var endTime = DateTime.UtcNow;
        
        var result = await GetEvents(startTime, endTime);

        VerifySingleEvent(result, EventMessage.DataExported, authenticatedUser: UserName);
    }

    [Test]
    [Retry(3)]
    public async Task ShallLogOnWebHookSent()
    {
        var startTime = DateTime.UtcNow;
        var client = await CreateTestWebHookClient(WebHookSecret);

        var webHook = new WebHookDataContract(null, client.Id!.Value, WebHookType.NewClientRegistration, ".*", ".*", 1);
        await CreateWebHook(webHook);

        await RegisterSettings<ClientA>();

        await WaitForCondition(async () => (await GetWebHookMessages(startTime)).Any(), TimeSpan.FromSeconds(2),
            () => "Waiting for a web hook message regarding the client registration");
        var endTime = DateTime.UtcNow.AddSeconds(1);

        var result = await GetEvents(startTime, endTime);
        var events = result.Events.RemoveCheckPointEvents();

        Assert.That(events.Count, Is.EqualTo(2));
        Assert.That(events[0].EventType, Is.EqualTo(EventMessage.WebHookSent));
    }

    [Test]
    public async Task ShallLogWhenClientSecretIsChanged()
    {
        var originalSecret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(originalSecret);

        var expiryTime = DateTime.UtcNow.AddMinutes(1);

        var updatedSecret = GetNewSecret();
        var startTime = DateTime.UtcNow;
        await ChangeClientSecret(settings.ClientName, updatedSecret, expiryTime);
        var endTime = DateTime.UtcNow;

        var events = (await GetEvents(startTime, endTime)).Events.RemoveCheckPointEvents();
        Assert.That(events.Count, Is.EqualTo(1));
        Assert.That(events[0].EventType, Is.EqualTo(EventMessage.ClientSecretChanged));
        Assert.That(events[0].Message!.Contains(expiryTime.ToString("u")));
        Assert.That(events[0].AuthenticatedUser, Is.EqualTo(UserName));
    }

    [Test]
    public async Task ShallFilterEventsForUser()
    {
        var user = NewUser(clientFilter: "ThreeSettings");
        await CreateUser(user);
        var loginResult = await Login(user.Username, user.Password ?? throw new InvalidOperationException("Password is null"));
        
        var startTime = DateTime.UtcNow;
        var settings = await RegisterSettings<ThreeSettings>();
        await RegisterSettings<ClientA>();
        await RegisterSettings<ClientXWithThreeSettings>();
        var endTime = DateTime.UtcNow;
        var result = await GetEvents(startTime, endTime, loginResult.Token);

        VerifySingleEvent(result, EventMessage.InitialRegistration, settings.ClientName);
    }

    [Test]
    public async Task ShallGetEventLogCount()
    {
        await RegisterSettings<ThreeSettings>();
        var result = await GetEventCount();

        Assert.That(result, Is.AtLeast(2));
    }

    [Test]
    public async Task ShallLogRestartRequest()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);

        var runSessionId = Guid.NewGuid();
        var clientStatus = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 5000, true, runSessionId: runSessionId);
        await GetStatus(settings.ClientName, secret, clientStatus);

        var startTime = DateTime.UtcNow;
        await RequestRestart(runSessionId);
        var endTime = DateTime.UtcNow;
        
        var result = await GetEvents(startTime, endTime);
        VerifySingleEvent(result, EventMessage.RestartRequested);
    }

    [Test]
    public async Task ShallLogLiveReloadRequest()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);

        var runSessionId = Guid.NewGuid();
        var clientStatus = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 5000, true, runSessionId: runSessionId);
        await GetStatus(settings.ClientName, secret, clientStatus);

        var startTime = DateTime.UtcNow;
        await SetLiveReload(false, runSessionId);
        var endTime = DateTime.UtcNow;
        
        var result = await GetEvents(startTime, endTime);
        VerifySingleEvent(result, EventMessage.LiveReloadChanged);
    }
    
    [Test]
    public async Task ShallLogWhenExternallyManagedSettingIsUpdated()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);

        var export = await ExportValueOnlyData();

        export.IsExternallyManaged = true;

        await ImportValueOnlyData(export);

        const string newValue = "Some new value";
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract(newValue))
        };
        
        var startTime = DateTime.UtcNow;
        await SetSettings(settings.ClientName, settingsToUpdate);
        var endTime = DateTime.UtcNow;
        
        var result = await GetEvents(startTime, endTime);
        var nonCheckPointEvents = result.Events.RemoveCheckPointEvents();
        
        Assert.That(nonCheckPointEvents.Count, Is.EqualTo(2));
        Assert.That(nonCheckPointEvents.Any(a => a.EventType == EventMessage.ExternallyManagedSettingUpdatedByUser));
    }

    [Test]
    public async Task ShallLogChangesScheduledEvents()
    {
        var secret = Guid.NewGuid().ToString();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        const string newValue = "some new value";
        const string message = "scheduled update";
        var executeAt = DateTime.UtcNow.AddMinutes(10);
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract(newValue))
        };
        
        var startTime = DateTime.UtcNow;
        await SetSettings(settings.ClientName, settingsToUpdate, message: message, applyAt: executeAt);
        var endTime = DateTime.UtcNow;
        
        var result = await GetEvents(startTime, endTime);

        var scheduledEvent = VerifySingleEvent(result, EventMessage.ChangesScheduled, settings.ClientName);
        Assert.That(scheduledEvent.AuthenticatedUser, Is.EqualTo(UserName));
        Assert.That(scheduledEvent.Message, Does.Contain("Changes to 1 setting(s) scheduled"));
        Assert.That(scheduledEvent.Message, Does.Contain(executeAt.ToString("u")));
    }

    [Test]
    public async Task ShallLogRescheduledChangesEvents()
    {
        var secret = Guid.NewGuid().ToString();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        const string newValue = "some new value";
        const string message = "scheduled update";
        var initialExecuteAt = DateTime.UtcNow.AddMinutes(10);
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract(newValue))
        };
        
        await SetSettings(settings.ClientName, settingsToUpdate, message: message, applyAt: initialExecuteAt);
        
        var scheduledChanges = await GetScheduledChanges();
        var change = scheduledChanges.Changes.First();
        var newExecuteAt = DateTime.UtcNow.AddMinutes(15);
        
        var startTime = DateTime.UtcNow;
        await RescheduleChange(change.Id, newExecuteAt);
        var endTime = DateTime.UtcNow;
        
        var result = await GetEvents(startTime, endTime);

        var rescheduledEvent = VerifySingleEvent(result, EventMessage.ChangesScheduled, settings.ClientName);
        Assert.That(rescheduledEvent.AuthenticatedUser, Is.EqualTo(UserName));
        Assert.That(rescheduledEvent.Message, Does.Contain("Changes to 1 setting(s) rescheduled"));
        Assert.That(rescheduledEvent.Message, Does.Contain(newExecuteAt.ToString("u")));
    }
    
    [Test]
    public async Task ShallLogScheduledRevertChangesEvents()
    {
        var secret = Guid.NewGuid().ToString();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        const string newValue = "some temporary value";
        const string message = "temporary change";
        // Use more generous timing to avoid race conditions
        var applyAt = DateTime.UtcNow.AddSeconds(3);
        var revertAt = DateTime.UtcNow.AddMinutes(5);
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract(newValue))
        };
        
        var startTime = DateTime.UtcNow;
        await SetSettings(settings.ClientName, settingsToUpdate, message: message, applyAt: applyAt, revertAt: revertAt);
        var endTime = DateTime.UtcNow;
        
        var result = await GetEvents(startTime, endTime);
        var nonCheckPointEvents = result.Events.RemoveCheckPointEvents();

        // We see just the apply event now.
        Assert.That(nonCheckPointEvents.Count(), Is.EqualTo(1));
        
        var applyEvent = nonCheckPointEvents.First(e => e.Message?.Contains("scheduled for") == true);
        Assert.That(applyEvent.EventType, Is.EqualTo(EventMessage.ChangesScheduled));
        Assert.That(applyEvent.ClientName, Is.EqualTo(settings.ClientName));
        Assert.That(applyEvent.Message, Does.Contain("scheduled for"));
        Assert.That(applyEvent.Message, Does.Contain(applyAt.ToString("u")));

        // Capture the time just before we expect the apply to happen
        var beforeApplyTime = DateTime.UtcNow;
        
        // Wait for the scheduled change to be applied
        await WaitForCondition(async () =>
        {
            var values = await GetSettingsForClient(settings.ClientName, secret);
            var match = values.FirstOrDefault(a => a.Name == nameof(settings.AStringSetting));
            return match?.Value?.GetValue()?.ToString() == newValue;
        }, TimeSpan.FromSeconds(10));
        
        // Wait for the revert change to be scheduled (should happen immediately after apply)
        await WaitForCondition(async () =>
        {
            var changes = await GetScheduledChanges();
            return changes.Changes.Count() == 1;
        }, TimeSpan.FromSeconds(10));
        
        // Get events from just before the apply happened to now
        var revertEventEndTime = DateTime.UtcNow.AddSeconds(1);
        
        var result2 = await GetEvents(beforeApplyTime, revertEventEndTime);
        var allNonCheckPointEvents = result2.Events.RemoveCheckPointEvents();
        
        // Find the revert event - should be scheduled after the apply event
        var revertEvents = allNonCheckPointEvents.Where(e => 
            e.EventType == EventMessage.ChangesScheduled && 
            e.Message != null && 
            (e.Message.Contains("to be reverted") || e.Message.Contains("revert")));
        
        Assert.That(revertEvents.Count(), Is.AtLeast(1), "Should have at least one revert-related scheduled event");
        
        var revertEvent = revertEvents.First();
        Assert.That(revertEvent.EventType, Is.EqualTo(EventMessage.ChangesScheduled));
        Assert.That(revertEvent.ClientName, Is.EqualTo(settings.ClientName));
        Assert.That(revertEvent.Message, Does.Contain(revertAt.ToString("u")));
    }
    
     [Test]
    public async Task ShallLogHealthStatusChangedEvent()
    {
        var secret = Guid.NewGuid().ToString();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        var clientName = settings.ClientName;
        var runSessionId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var startTime = now;

        // Initial status: Healthy
        var healthyStatus = new Contracts.Status.StatusRequestDataContract(
            runSessionId,
            now.AddSeconds(-10),
            now.AddSeconds(-10),
            1000,
            "1.0.0",
            "1.0.0",
            false,
            true,
            "user",
            1000,
            new HealthDataContract { Status = FigHealthStatus.Healthy, Components =
                [
                    new("component1", FigHealthStatus.Healthy, "ok")
                ]
            }
        );
        await GetStatus(clientName, secret, healthyStatus);

        // Change status: Unhealthy
        var message = "Configuration is invalid: [Value1]: Should have a value";
        var unhealthyStatus = new Contracts.Status.StatusRequestDataContract(
            runSessionId,
            now,
            now,
            1000,
            "1.0.0",
            "1.0.0",
            false,
            true,
            "user",
            1000,
            new HealthDataContract { Status = FigHealthStatus.Unhealthy, Components =
                [
                    new("component1", FigHealthStatus.Unhealthy, message)
                ]
            }
        );
        await GetStatus(clientName, secret, unhealthyStatus);
        var endTime = DateTime.UtcNow;

        var result = await GetEvents(startTime, endTime);
        var healthEvent = result.Events.FirstOrDefault(e => e.EventType == EventMessage.HealthStatusChanged && e.ClientName == clientName);
        Assert.That(healthEvent, Is.Not.Null, "HealthStatusChanged event should be logged");
        Assert.That(healthEvent!.OriginalValue, Is.EqualTo(nameof(FigHealthStatus.Healthy)));
        Assert.That(healthEvent.NewValue, Is.EqualTo(nameof(FigHealthStatus.Unhealthy)));
        Assert.That(healthEvent.Message, Does.Contain(message));
    }

    private async Task<EventLogCollectionDataContract> PerformImport(ImportType importType)
    {
        var secret = Guid.NewGuid().ToString();
        await RegisterSettings<ThreeSettings>(secret);

        var data = await ExportData();
        data.ImportType = importType;

        var startTime = DateTime.UtcNow;
        await ImportData(data);
        var endTime = DateTime.UtcNow;

        return await GetEvents(startTime, endTime);
    }

    private EventLogDataContract VerifySingleEvent(EventLogCollectionDataContract result, string eventType,
        string? clientName = null, string? authenticatedUser = null, bool checkPointEvent = false)
    {
        var eventCount = checkPointEvent ? 2 : 1;
        var events = checkPointEvent ? result.Events.ToList() : result.Events.RemoveCheckPointEvents();
        Assert.That(events.Count, Is.EqualTo(eventCount));
        var matchingEvent = events.First(a => a.EventType == eventType);
        Assert.That(matchingEvent.EventType, Is.EqualTo(eventType));
        if (clientName != null)
            Assert.That(matchingEvent.ClientName, Is.EqualTo(clientName));

        if (authenticatedUser != null)
            Assert.That(matchingEvent.AuthenticatedUser, Is.EqualTo(authenticatedUser));

        return matchingEvent;
    }

    private async Task<RegisterUserRequestDataContract> CreateUser()
    {
        var user = new RegisterUserRequestDataContract(TestUser,
            "First",
            "Last",
            Role.Administrator,
            "this is a long and complex password",
            ".*",
            Enum.GetValues(typeof(Classification)).Cast<Classification>().ToList());

        await CreateUser(user);

        return user;
    }

    [Test]
    public async Task ShallLogCustomActionAddedEvents()
    {
        var secret = Guid.NewGuid().ToString();
        var settings = await RegisterSettings<SettingsWithCustomAction>(secret);

        List<CustomActionDefinitionDataContract> actions =
        [
            new("Action1", "Run Test Action", "A simple test action.", "MySetting"),
            new("Action2", "Run DataGrid Action", "Action returning a datagrid.", "MyOtherSetting")
        ];

        var startTime = DateTime.UtcNow;
        await RegisterCustomActions(settings.ClientName, secret, actions);
        var endTime = DateTime.UtcNow;
        var result = await GetEvents(startTime, endTime);

        var nonCheckPointEvents = result.Events.RemoveCheckPointEvents();
        Assert.That(nonCheckPointEvents.Count, Is.EqualTo(2));
        
        var addedEvents = nonCheckPointEvents.Where(e => e.EventType == EventMessage.CustomActionAdded).OrderBy(e => e.NewValue).ToList();
        Assert.That(addedEvents.Count, Is.EqualTo(2));
        
        Assert.That(addedEvents[0].EventType, Is.EqualTo(EventMessage.CustomActionAdded));
        Assert.That(addedEvents[0].ClientName, Is.EqualTo(settings.ClientName));
        Assert.That(addedEvents[0].NewValue, Is.EqualTo("Action1"));
        
        Assert.That(addedEvents[1].EventType, Is.EqualTo(EventMessage.CustomActionAdded));
        Assert.That(addedEvents[1].ClientName, Is.EqualTo(settings.ClientName));
        Assert.That(addedEvents[1].NewValue, Is.EqualTo("Action2"));
    }

    [Test]
    public async Task ShallLogCustomActionUpdatedEvents()
    {
        var secret = Guid.NewGuid().ToString();
        var settings = await RegisterSettings<SettingsWithCustomAction>(secret);

        // Register initial actions
        List<CustomActionDefinitionDataContract> initialActions =
        [
            new("Action1", "Run Test Action", "A simple test action.", "MySetting")
        ];
        await RegisterCustomActions(settings.ClientName, secret, initialActions);

        // Update the action
        List<CustomActionDefinitionDataContract> updatedActions =
        [
            new("Action1", "Updated Test Action", "An updated test action.", "MySetting")
        ];

        var startTime = DateTime.UtcNow;
        await RegisterCustomActions(settings.ClientName, secret, updatedActions);
        var endTime = DateTime.UtcNow;
        var result = await GetEvents(startTime, endTime);

        var updatedEvent = VerifySingleEvent(result, EventMessage.CustomActionUpdated, settings.ClientName, checkPointEvent: false);
        Assert.That(updatedEvent.NewValue, Is.EqualTo("Action1"));
    }

    [Test]
    public async Task ShallLogCustomActionsRemovedEvents()
    {
        var secret = Guid.NewGuid().ToString();
        var settings = await RegisterSettings<SettingsWithCustomAction>(secret);

        // Register initial actions
        List<CustomActionDefinitionDataContract> initialActions =
        [
            new("Action1", "Run Test Action", "A simple test action.", "MySetting"),
            new("Action2", "Run DataGrid Action", "Action returning a datagrid.", "MyOtherSetting"),
            new("Action3", "Legacy Action", "This will be removed.", "MySetting")
        ];
        await RegisterCustomActions(settings.ClientName, secret, initialActions);

        // Remove Action3 by not including it in the new registration
        List<CustomActionDefinitionDataContract> updatedActions =
        [
            new("Action1", "Run Test Action", "A simple test action.", "MySetting"),
            new("Action2", "Run DataGrid Action", "Action returning a datagrid.", "MyOtherSetting")
        ];

        var startTime = DateTime.UtcNow;
        await RegisterCustomActions(settings.ClientName, secret, updatedActions);
        var endTime = DateTime.UtcNow;
        var result = await GetEvents(startTime, endTime);

        var removedEvent = result.Events.RemoveCheckPointEvents()
            .FirstOrDefault(e => e.EventType == EventMessage.CustomActionsRemoved);
        Assert.That(removedEvent, Is.Not.Null, "CustomActionsRemoved event should be logged");
        Assert.That(removedEvent!.ClientName, Is.EqualTo(settings.ClientName));
        Assert.That(removedEvent.Message, Does.Contain("Action3"));
    }

    [Test]
    public async Task ShallLogCustomActionExecutionRequestedEvents()
    {
        var secret = Guid.NewGuid().ToString();
        var settings = await RegisterSettings<SettingsWithCustomAction>(secret);

        List<CustomActionDefinitionDataContract> actions =
        [
            new("Action1", "Run Test Action", "A simple test action.", "MySetting")
        ];
        await RegisterCustomActions(settings.ClientName, secret, actions);

        var runSession = Guid.NewGuid();
        var clientStatus = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 5000, true, runSessionId: runSession);
        await GetStatus(settings.ClientName, secret, clientStatus);

        var startTime = DateTime.UtcNow;
        await ExecuteAction(settings.ClientName, actions[0], runSession);
        var endTime = DateTime.UtcNow;
        var result = await GetEvents(startTime, endTime);

        var executionEvent = VerifySingleEvent(result, EventMessage.CustomActionExecutionRequested, settings.ClientName, UserName, checkPointEvent: false);
        Assert.That(executionEvent.NewValue, Is.EqualTo("Action1"));
        Assert.That(executionEvent.AuthenticatedUser, Is.EqualTo(UserName));
        Assert.That(executionEvent.Message, Does.Contain(runSession.ToString()));
    }

    [Test]
    public async Task ShallLogCustomActionExecutionCompletedEvents()
    {
        var secret = Guid.NewGuid().ToString();
        var settings = await RegisterSettings<SettingsWithCustomAction>(secret);

        List<CustomActionDefinitionDataContract> actions =
        [
            new("Action1", "Run Test Action", "A simple test action.", "MySetting")
        ];
        await RegisterCustomActions(settings.ClientName, secret, actions);

        var runSession = Guid.NewGuid();
        var clientStatus = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 5000, true, runSessionId: runSession);
        await GetStatus(settings.ClientName, secret, clientStatus);

        // Execute the action
        await ExecuteAction(settings.ClientName, actions[0], runSession);
        var pollResponse = (await PollForExecutionRequests(settings.ClientName, runSession, secret)).ToList();
        
        var startTime = DateTime.UtcNow;
        // Complete the execution
        var executionResult = new CustomActionResultDataContract("test result", true) { TextResult = "Test Result" };
        await SubmitActionResult(settings.ClientName, secret,
            new CustomActionExecutionResultsDataContract(pollResponse[0].RequestId, [executionResult], true) { RunSessionId = runSession});
        var endTime = DateTime.UtcNow;
        
        var result = await GetEvents(startTime, endTime);
        var completedEvent = VerifySingleEvent(result, EventMessage.CustomActionExecutionCompleted, settings.ClientName, checkPointEvent: false);
        Assert.That(completedEvent.NewValue, Is.EqualTo("Action1"));
        Assert.That(completedEvent.Message, Does.Contain("successfully"));
    }

    [Test]
    public async Task ShallLogCustomActionExecutionCompletedEventsWithFailure()
    {
        var secret = Guid.NewGuid().ToString();
        var settings = await RegisterSettings<SettingsWithCustomAction>(secret);

        List<CustomActionDefinitionDataContract> actions =
        [
            new("Action1", "Run Test Action", "A simple test action.", "MySetting")
        ];
        await RegisterCustomActions(settings.ClientName, secret, actions);

        var runSession = Guid.NewGuid();
        var clientStatus = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 5000, true, runSessionId: runSession);
        await GetStatus(settings.ClientName, secret, clientStatus);

        // Execute the action
        await ExecuteAction(settings.ClientName, actions[0], runSession);
        var pollResponse = (await PollForExecutionRequests(settings.ClientName, runSession, secret)).ToList();
        
        var startTime = DateTime.UtcNow;
        // Complete the execution with failure
        var executionResult = new CustomActionResultDataContract("test result", false) { TextResult = "Test Result Failed" };
        await SubmitActionResult(settings.ClientName, secret,
            new CustomActionExecutionResultsDataContract(pollResponse[0].RequestId, [executionResult], false) { RunSessionId = runSession});
        var endTime = DateTime.UtcNow;
        
        var result = await GetEvents(startTime, endTime);
        var completedEvent = VerifySingleEvent(result, EventMessage.CustomActionExecutionCompleted, settings.ClientName, checkPointEvent: false);
        Assert.That(completedEvent.NewValue, Is.EqualTo("Action1"));
        Assert.That(completedEvent.Message, Does.Contain("with errors"));
    }
    
    [Test]
    public async Task ShallGetClientTimelineWithSettingChanges()
    {
        var secret = Guid.NewGuid().ToString();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        const string message = "Test change";
        
        // Make some setting changes
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract("newValue")),
            new(nameof(settings.ABoolSetting), new BoolSettingDataContract(false)),
        };
        
        await SetSettings(settings.ClientName, settingsToUpdate, message: message);
        
        // Get client timeline
        var result = await GetClientTimeline(settings.ClientName, null);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Events, Is.Not.Empty);
        
        var events = result.Events.ToList();
        
        // Should include initial registration and setting changes
        Assert.That(events.Any(e => e.EventType == EventMessage.InitialRegistration), Is.True);
        Assert.That(events.Any(e => e.EventType == EventMessage.SettingValueUpdated), Is.True);
        
        // Check setting change events
        var settingChanges = events.Where(e => e.EventType == EventMessage.SettingValueUpdated).ToList();
        Assert.That(settingChanges.Count, Is.AtLeast(2));
        
        var stringSettingChange = settingChanges.First(e => e.SettingName == nameof(settings.AStringSetting));
        Assert.That(stringSettingChange.NewValue, Is.EqualTo("newValue"));
        Assert.That(stringSettingChange.Message, Is.EqualTo(message));
        
        var boolSettingChange = settingChanges.First(e => e.SettingName == nameof(settings.ABoolSetting));
        Assert.That(boolSettingChange.NewValue, Is.EqualTo("False"));
        Assert.That(boolSettingChange.Message, Is.EqualTo(message));
    }
    
    [Test]
    public async Task ShallGetClientTimelineWithInstanceFilter()
    {
        var secret = Guid.NewGuid().ToString();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        const string instanceName = "TestInstance";
        const string message = "Instance test";
        
        // Make setting change for specific instance
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract("instanceValue"))
        };
        
        await SetSettings(settings.ClientName, settingsToUpdate, instanceName, message: message);
        
        // Get timeline for specific instance
        var result = await GetClientTimeline(settings.ClientName, instanceName);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Events, Is.Not.Empty);
        
        var events = result.Events.ToList();
        
        // Should include setting changes for the instance
        var settingChanges = events.Where(e => e.EventType == EventMessage.SettingValueUpdated).ToList();
        Assert.That(settingChanges.Count, Is.AtLeast(1), $"Changes were: {string.Join(", ", settingChanges.Select(c => c.SettingName + "=" + c.NewValue))}");
        
        var change = settingChanges.Last();
        Assert.That(change.Instance, Is.EqualTo(instanceName));
        Assert.That(change.NewValue, Is.EqualTo("instanceValue"));
        Assert.That(change.Message, Is.EqualTo(message));
    }
    
    [Test]
    public async Task ShallGetEmptyTimelineForNonexistentClient()
    {
        var result = await GetClientTimeline("NonexistentClient", null);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Events, Is.Empty);
    }
    
    [Test]
    public async Task ShallGetClientTimelineWithExternallyManagedSettings()
    {
        var secret = Guid.NewGuid().ToString();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        
        // Export and make externally managed
        var export = await ExportValueOnlyData();
        export.IsExternallyManaged = true;
        await ImportValueOnlyData(export);
        
        const string message = "External update";
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract("externalValue"))
        };
        
        await SetSettings(settings.ClientName, settingsToUpdate, message: message);
        
        // Get client timeline
        var result = await GetClientTimeline(settings.ClientName, null);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Events, Is.Not.Empty);
        
        var events = result.Events.ToList();
        
        // Should include externally managed setting update
        Assert.That(events.Any(e => e.EventType == EventMessage.ExternallyManagedSettingUpdatedByUser), Is.True);
        
        var externalUpdate = events.First(e => e.EventType == EventMessage.ExternallyManagedSettingUpdatedByUser);
        Assert.That(externalUpdate.SettingName, Is.EqualTo(nameof(settings.AStringSetting)));
        Assert.That(externalUpdate.NewValue, Is.EqualTo("externalValue"));
        Assert.That(externalUpdate.Message, Is.EqualTo(message));
    }
    
    [Test]
    public async Task ShallGetClientTimelineInDescendingOrder()
    {
        var secret = Guid.NewGuid().ToString();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        
        // Make multiple changes with delays to ensure different timestamps
        await SetSettings(settings.ClientName, new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract("first"))
        }, message: "First change");
        
        await Task.Delay(100); // Small delay to ensure different timestamps
        
        await SetSettings(settings.ClientName, new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract("second"))
        }, message: "Second change");
        
        // Get client timeline
        var result = await GetClientTimeline(settings.ClientName, null);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Events, Is.Not.Empty);
        
        var events = result.Events.ToList();
        
        // Events should be in descending order (newest first)
        for (int i = 0; i < events.Count - 1; i++)
        {
            Assert.That(events[i].Timestamp, Is.GreaterThanOrEqualTo(events[i + 1].Timestamp),
                "Events should be ordered by timestamp descending");
        }
        
        // The most recent setting change should be "second"
        var recentSettingChange = events.First(e => e.EventType == EventMessage.SettingValueUpdated);
        Assert.That(recentSettingChange.NewValue, Is.EqualTo("second"));
        Assert.That(recentSettingChange.Message, Is.EqualTo("Second change"));
    }
    
    [Test]
    public async Task ShallGetClientTimelineWithRegistrationEvents()
    {
        var secret = Guid.NewGuid().ToString();
        
        // Initial registration
        var settings = await RegisterSettings<ThreeSettings>(secret);
        
        // Updated registration (change settings definition)
        await RegisterSettings<ClientXWithThreeSettings>(secret);
        
        // Get client timeline
        var result = await GetClientTimeline(settings.ClientName, null);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Events, Is.Not.Empty);
        
        var events = result.Events.ToList();
        
        // Should include initial registration
        Assert.That(events.Any(e => e.EventType == EventMessage.InitialRegistration), Is.True);
        
        var initialReg = events.First(e => e.EventType == EventMessage.InitialRegistration);
        Assert.That(initialReg.ClientName, Is.EqualTo(settings.ClientName));
    }
    
    [Test]
    public async Task ShallGetClientTimelineWithClientDeletedEvents()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        
        var startTime = DateTime.UtcNow;
        await DeleteClient(settings.ClientName);
        var endTime = DateTime.UtcNow;
        
        var result = await GetClientTimeline(settings.ClientName, null);
        
        Assert.That(result.Events.Count, Is.AtLeast(1));
        var deletedEvent = result.Events.FirstOrDefault(e => e.EventType == EventMessage.ClientDeleted);
        Assert.That(deletedEvent, Is.Not.Null);
        Assert.That(deletedEvent!.ClientName, Is.EqualTo(settings.ClientName));
        Assert.That(deletedEvent.AuthenticatedUser, Is.EqualTo(UserName));
    }
    
    [Test]
    public async Task ShallIncludeUsernameInTimelineEvents()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract("Updated Value"))
        };
        
        var startTime = DateTime.UtcNow;
        await SetSettings(settings.ClientName, settingsToUpdate, "Test change");
        var endTime = DateTime.UtcNow;
        
        var result = await GetClientTimeline(settings.ClientName, null);
        
        Assert.That(result.Events.Count, Is.AtLeast(1));
        var settingUpdatedEvent = result.Events.FirstOrDefault(e => e.EventType == EventMessage.SettingValueUpdated);
        Assert.That(settingUpdatedEvent, Is.Not.Null);
        Assert.That(settingUpdatedEvent!.AuthenticatedUser, Is.EqualTo(UserName));
    }
    
    [Test]
    public async Task ShallFilterTimelineEventsByUserAccess()
    {
        // Create a user with limited access to only ThreeSettings client
        var limitedUser = NewUser(clientFilter: "ThreeSettings");
        await CreateUser(limitedUser);
        var loginResult = await Login(limitedUser.Username, limitedUser.Password ?? throw new InvalidOperationException("Password is null"));
        
        // Create two different clients
        var secret1 = GetNewSecret();
        var secret2 = GetNewSecret();
        var threeSettings = await RegisterSettings<ThreeSettings>(secret1);
        var clientA = await RegisterSettings<ClientA>(secret2);
        
        // Make changes to both clients
        var startTime = DateTime.UtcNow;
        await SetSettings(threeSettings.ClientName, new List<SettingDataContract>
        {
            new(nameof(threeSettings.AStringSetting), new StringSettingDataContract("Updated"))
        });
        await SetSettings(clientA.ClientName, new List<SettingDataContract>
        {
            new(nameof(clientA.WebsiteAddress), new StringSettingDataContract("Updated"))
        });
        var endTime = DateTime.UtcNow;
        
        // Test with admin user - should see events for both clients
        var adminResult = await GetClientTimeline(threeSettings.ClientName, null);
        Assert.That(adminResult.Events.Count, Is.AtLeast(1));
        
        // Test with limited user - should only see events for ThreeSettings
        var limitedResult = await GetClientTimeline(threeSettings.ClientName, null, loginResult.Token);
        Assert.That(limitedResult.Events.Count, Is.AtLeast(1));
        var allEventsForAllowedClient = limitedResult.Events.All(e => e.ClientName == threeSettings.ClientName || string.IsNullOrEmpty(e.ClientName));
        Assert.That(allEventsForAllowedClient, Is.True, "Limited user should only see events for clients they have access to");
    }
    
    [Test]
    public async Task ShallIncludeAllRelevantEventTypesInTimeline()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        
        var startTime = DateTime.UtcNow;
        
        // Create setting change event
        await SetSettings(settings.ClientName, new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract("Updated Value"))
        });
        
        // Create externally managed setting event
        var export = await ExportValueOnlyData();
        export.IsExternallyManaged = true;
        await ImportValueOnlyData(export);
        await SetSettings(settings.ClientName, new List<SettingDataContract>
        {
            new(nameof(settings.ABoolSetting), new BoolSettingDataContract(false))
        });
        
        // Create client deleted event
        await DeleteClient(settings.ClientName);
        var endTime = DateTime.UtcNow;
        
        var result = await GetClientTimeline(settings.ClientName, null);
        
        // Should contain various event types
        var eventTypes = result.Events.Select(e => e.EventType).Distinct().ToList();
        Assert.That(eventTypes, Contains.Item(EventMessage.SettingValueUpdated), "Should include setting value updated events");
        Assert.That(eventTypes, Contains.Item(EventMessage.ExternallyManagedSettingUpdatedByUser), "Should include externally managed setting events");
        Assert.That(eventTypes, Contains.Item(EventMessage.ClientDeleted), "Should include client deleted events");
        Assert.That(eventTypes, Contains.Item(EventMessage.InitialRegistration), "Should include registration events");
    }

    private async Task<EventLogCollectionDataContract> GetClientTimeline(string clientName, string? instance, string? token = null)
    {
        var uri = $"/events/client/{Uri.EscapeDataString(clientName)}/timeline";
        if (!string.IsNullOrEmpty(instance))
        {
            uri += $"?instance={Uri.EscapeDataString(instance)}";
        }
        
        var result = await ApiClient.Get<EventLogCollectionDataContract>(uri, tokenOverride: token);
        
        if (result == null)
            throw new ApplicationException($"Expected non null result for get for URI {uri}");
        
        return result;
    }
}