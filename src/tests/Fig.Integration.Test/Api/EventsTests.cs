using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fig.Api.Constants;
using Fig.Common.Constants;
using Fig.Common.NetStandard.Data;
using Fig.Contracts.Authentication;
using Fig.Contracts.EventHistory;
using Fig.Contracts.ImportExport;
using Fig.Contracts.Settings;
using Fig.Contracts.WebHook;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using Microsoft.Extensions.Logging;
using Moq;
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
        
        Assert.That(result.Events.Count(), Is.EqualTo(3));
        var events = result.Events.OrderBy(a => a.SettingName).ToList();
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
        
        Assert.That(result.Events.Count(), Is.EqualTo(0));
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

        Assert.That(result.Events.Count(), Is.EqualTo(2));
        var firstEvent = result.Events.First(a => a.EventType == EventMessage.ClientInstanceCreated);
        Assert.That(firstEvent, Is.Not.Null, "Instance creation should be logged");
        Assert.That(firstEvent.ClientName, Is.EqualTo(settings.ClientName));
        Assert.That(firstEvent.Instance, Is.EqualTo(instanceName));
    }

    [Test]
    public async Task ShallLogSettingVerificationRunEvents()
    {
        var secret = Guid.NewGuid().ToString();
        var settings = await RegisterSettings<SettingsWithVerification>(secret);
        var client = await GetClient(settings);

        var verification = client.Verifications.Single();
        var startTime = DateTime.UtcNow;
        await RunVerification(settings.ClientName, verification.Name);
        var endTime = DateTime.UtcNow;

        var result = await GetEvents(startTime, endTime);
        VerifySingleEvent(result, EventMessage.SettingVerificationRun, settings.ClientName);
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

        Assert.That(result.Events.Count(), Is.EqualTo(1));
        var lastEvent = result.Events.Last();
        Assert.That(lastEvent.ClientName, Is.EqualTo(settings.ClientName));
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

        Assert.That(result.Events.Count(), Is.EqualTo(1));
        var firstEvent = result.Events.First();
        Assert.That(firstEvent.EventType, Is.EqualTo(EventMessage.NewSession));
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

        Assert.That(result.Events.Count(), Is.EqualTo(2));
        var expiredSessionEvent = result.Events.FirstOrDefault(a => a.EventType == EventMessage.ExpiredSession);
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

        Assert.That(result.Events.Count(), Is.EqualTo(1));
        var dataExportedEvent = result.Events.FirstOrDefault(a => a.EventType == EventMessage.DataExported);
        Assert.That(dataExportedEvent, Is.Not.Null);
    }

    [Test]
    public async Task ShallLogDataImportedEventLog()
    {
        var result = await PerformImport(ImportType.ClearAndImport);

        Assert.That(result.Events.Count(), Is.EqualTo(4));
        var dataImportedEvent = result.Events.FirstOrDefault(a => a.EventType == EventMessage.DataImported);
        Assert.That(dataImportedEvent, Is.Not.Null);
    }

    [Test]
    public async Task ShallNotLogDataImportedEventLogIfNoClientsWereImported()
    {
        var result = await PerformImport(ImportType.AddNew);

        Assert.That(result.Events.Count(), Is.EqualTo(1));
        var dataImportedEvent = result.Events.FirstOrDefault(a => a.EventType == EventMessage.DataImported);
        Assert.That(dataImportedEvent, Is.Null);
    }

    [Test]
    public async Task ShallLogDataImportStartedEventLog()
    {
        var result = await PerformImport(ImportType.AddNew);

        Assert.That(result.Events.Count(), Is.EqualTo(1));
        var dataImportStartingEvent = result.Events.FirstOrDefault(a => a.EventType == EventMessage.DataImportStarted);
        Assert.That(dataImportStartingEvent, Is.Not.Null);
    }

    [Test]
    public async Task ShallLogClientImportedEventLog()
    {
        var result = await PerformImport(ImportType.ReplaceExisting);

        Assert.That(result.Events.Count(), Is.EqualTo(4));
        var dataImportStartingEvent = result.Events.FirstOrDefault(a => a.EventType == EventMessage.DataImportStarted);
        Assert.That(dataImportStartingEvent, Is.Not.Null);
    }

    [Test]
    public async Task ShallLogConfigurationChangedEventLog()
    {
        var startTime = DateTime.UtcNow;
        await SetConfiguration(CreateConfiguration(false));
        var endTime = DateTime.UtcNow;

        var result = await GetEvents(startTime, endTime);
        Assert.That(result.Events.Count(), Is.EqualTo(1));
        var configurationChangedEvent = result.Events.Single();
        Assert.That(configurationChangedEvent.EventType, Is.EqualTo(EventMessage.ConfigurationChanged));
    }

    [Test]
    public async Task ShallLogConfigErrorOnInitialPoll()
    {
        var secret = Guid.NewGuid().ToString();
        var settings = await RegisterSettings<ThreeSettings>(secret);

        var clientStatus = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 5000, true, true);

        var startTime = DateTime.UtcNow;
        await GetStatus(settings.ClientName, secret, clientStatus);
        var endTime = DateTime.UtcNow;
        var result = await GetEvents(startTime, endTime);

        Assert.That(result.Events.Count(), Is.EqualTo(2));
        var configErrorEvent = result.Events.FirstOrDefault(a => a.EventType == EventMessage.HasConfigurationError);
        Assert.That(configErrorEvent, Is.Not.Null);
    }
    
    [Test]
    public async Task ShallLogConfigErrorOnInitialPollWithRealClient()
    {
        await SetConfiguration(CreateConfiguration(pollIntervalOverrideMs: 200));
        var secret = GetNewSecret();
        var startTime = DateTime.UtcNow;
        var (settings, _) = InitializeConfigurationProvider<SettingsWithConfigError>(secret);
        settings.CurrentValue.Validate(Mock.Of<ILogger>());
        
        await WaitForCondition(async () =>
        {
            var result = await GetEvents(startTime, DateTime.UtcNow);
            return result.Events.Any(a => a.EventType == EventMessage.HasConfigurationError);
        }, TimeSpan.FromSeconds(3));

        var endTime = DateTime.UtcNow;
        var result = await GetEvents(startTime, endTime);
        
        var configErrorEvent = result.Events.FirstOrDefault(a => a.EventType == EventMessage.HasConfigurationError);
        Assert.That(configErrorEvent, Is.Not.Null);
        var configErrorMessage = result.Events.FirstOrDefault(a => a.EventType == EventMessage.ConfigurationError);
        Assert.That(configErrorMessage?.Message, Is.EqualTo("A config error"));
    }

    [Test]
    public async Task ShallLogConfigErrorSet()
    {
        var secret = Guid.NewGuid().ToString();
        var settings = await RegisterSettings<ThreeSettings>(secret);

        var clientStatus = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 5000, true);
        await GetStatus(settings.ClientName, secret, clientStatus);

        var clientStatus2 = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 5000, true, true,
            runSessionId: clientStatus.RunSessionId);
        
        var startTime = DateTime.UtcNow;
        await GetStatus(settings.ClientName, secret, clientStatus2);
        var endTime = DateTime.UtcNow;
        var result = await GetEvents(startTime, endTime);

        Assert.That(result.Events.Count(), Is.EqualTo(1));
        var configErrorEvent = result.Events.First();
        Assert.That(configErrorEvent.EventType, Is.EqualTo(EventMessage.HasConfigurationError));
    }
    
    [Test]
    public async Task ShallLogConfigErrorCleared()
    {
        var secret = Guid.NewGuid().ToString();
        var settings = await RegisterSettings<ThreeSettings>(secret);

        var clientStatus = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 5000, true, true);
        await GetStatus(settings.ClientName, secret, clientStatus);

        var clientStatus2 =
            CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 5000, true, runSessionId: clientStatus.RunSessionId);
        
        var startTime = DateTime.UtcNow;
        await GetStatus(settings.ClientName, secret, clientStatus2);
        var endTime = DateTime.UtcNow;
        var result = await GetEvents(startTime, endTime);

        Assert.That(result.Events.Count(), Is.EqualTo(1));
        var configErrorEvent = result.Events.First();
        Assert.That(configErrorEvent.EventType, Is.EqualTo(EventMessage.ConfigurationErrorCleared));
    }

    [Test]
    public async Task ShallLogConfigErrorsPassedByClient()
    {
        var secret = Guid.NewGuid().ToString();
        var settings = await RegisterSettings<ThreeSettings>(secret);

        const string error1 = "Name was wrong";
        const string error2 = "Address was wrong";
        var errors = new List<string>() { error1, error2 };
        
        var clientStatus = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 5000, true, true, errors);

        var startTime = DateTime.UtcNow;
        await GetStatus(settings.ClientName, secret, clientStatus);
        var endTime = DateTime.UtcNow;
        var result = await GetEvents(startTime, endTime);

        Assert.That(result.Events.Count(), Is.EqualTo(4));
        var configErrorEvents = result.Events.Where(a => a.EventType == EventMessage.ConfigurationError).ToList();
        Assert.That(configErrorEvents.Count, Is.EqualTo(2));
        var messages = configErrorEvents.Select(a => a.Message).ToList();
        Assert.That(messages.Contains(error1));
        Assert.That(messages.Contains(error2));
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
        var events = result.Events.ToList();
        
        Assert.That(events.Count, Is.EqualTo(4));
        Assert.That(events[0].EventType, Is.EqualTo(EventMessage.DataImported));
        Assert.That(events[1].EventType, Is.EqualTo(EventMessage.SettingValueUpdated));
        Assert.That(events[2].EventType, Is.EqualTo(EventMessage.SettingValueUpdated));
        Assert.That(events[3].EventType, Is.EqualTo(EventMessage.DataImportStarted));
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

        Assert.That(result.Events.Count(), Is.EqualTo(2));
        Assert.That(result.Events.Last().EventType, Is.EqualTo(EventMessage.DataImportStarted));
        Assert.That(result.Events.First().EventType, Is.EqualTo(EventMessage.DeferredImportRegistered));
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
        
        var events = result.Events.ToList();
        
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

        Assert.That(result.Events.Count(), Is.EqualTo(1));
        Assert.That(result.Events.Single().EventType, Is.EqualTo(EventMessage.DataExported));
    }

    [Test]
    [Retry(3)]
    public async Task ShallLogOnWebHookSent()
    {
        var startTime = DateTime.UtcNow;
        var client = await CreateTestWebHookClient(WebHookSecret);

        var webHook = new WebHookDataContract(null, client.Id.Value, WebHookType.NewClientRegistration, ".*", ".*", 1);
        await CreateWebHook(webHook);

        await RegisterSettings<ClientA>();

        await WaitForCondition(async () => (await GetWebHookMessages(startTime)).Any(), TimeSpan.FromSeconds(2),
            () => "Waiting for a web hook message regarding the client registration");
        var endTime = DateTime.UtcNow.AddSeconds(1);

        var result = await GetEvents(startTime, endTime);
        var events = result.Events.ToList();

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

        var events = (await GetEvents(startTime, endTime)).Events.ToList();
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
        var loginResult = await Login(user.Username, user.Password);
        
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
        Assert.That(result.Events.Count(), Is.EqualTo(2));
        Assert.That(result.Events.Any(a => a.EventType == EventMessage.ExternallyManagedSettingUpdatedByUser));
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
        var applyAt = DateTime.UtcNow.AddSeconds(1);
        var revertAt = DateTime.UtcNow.AddMinutes(5);
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract(newValue))
        };
        
        var startTime = DateTime.UtcNow;
        await SetSettings(settings.ClientName, settingsToUpdate, message: message, applyAt: applyAt, revertAt: revertAt);
        var endTime = DateTime.UtcNow;
        
        var result = await GetEvents(startTime, endTime);

        // We see just the apply event now.
        Assert.That(result.Events.Count(), Is.EqualTo(1));
        
        var applyEvent = result.Events.First(e => e.Message.Contains("scheduled for"));
        Assert.That(applyEvent.EventType, Is.EqualTo(EventMessage.ChangesScheduled));
        Assert.That(applyEvent.ClientName, Is.EqualTo(settings.ClientName));
        Assert.That(applyEvent.Message, Does.Contain("scheduled for"));
        Assert.That(applyEvent.Message, Does.Contain(applyAt.ToString("u")));

        var startTime2 = DateTime.UtcNow;
        await WaitForCondition(async () =>
        {
            var values = await GetSettingsForClient(settings.ClientName, secret);
            var match = values.FirstOrDefault(a => a.Name == nameof(settings.AStringSetting));
            return match?.Value?.GetValue()?.ToString() == newValue;
        }, TimeSpan.FromSeconds(5));
        
        await WaitForCondition(async () =>
        {
            var changes = await GetScheduledChanges();
            return changes.Changes.Count() == 1;
        }, TimeSpan.FromSeconds(5));
        
        var endTime2 = DateTime.UtcNow;
        
        var result2 = await GetEvents(startTime2, endTime2);
        
        // Revert event
        Assert.That(result2.Events.Count(), Is.AtLeast(1));
        
        var revertEvent = result2.Events.First(e => e.Message?.Contains("to be reverted") == true);
        Assert.That(revertEvent.EventType, Is.EqualTo(EventMessage.ChangesScheduled));
        Assert.That(revertEvent.ClientName, Is.EqualTo(settings.ClientName));
        Assert.That(revertEvent.Message, Does.Contain("to be reverted"));
        Assert.That(revertEvent.Message, Does.Contain(revertAt.ToString("u")));
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
        Assert.That(result.Events.Count(), Is.EqualTo(eventCount));
        var matchingEvent = result.Events.First(a => a.EventType == eventType);
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
}