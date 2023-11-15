using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Fig.Contracts.Settings;
using Fig.Contracts.WebHook;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using Fig.WebHooks.Contracts;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

[TestFixture]
public class WebHookIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task ShallSendNewClientRegistrationWebHook()
    {
        var testStart = DateTime.UtcNow;
        var client = await CreateTestWebHookClient(WebHookSecret);

        var webHook = new WebHookDataContract(null, client.Id.Value, WebHookType.NewClientRegistration, ".*", ".*", 1);
        await CreateWebHook(webHook);

        var settings = await RegisterSettings<ClientA>();
        
        await WaitForCondition(async () => (await GetWebHookMessages(testStart)).Count() == 1, TimeSpan.FromSeconds(1));
        var webHookMessages = (await GetWebHookMessages(testStart)).ToList();
        
        Assert.That(webHookMessages.Count, Is.EqualTo(1));

        var contract = GetMessageOfType<ClientRegistrationDataContract>(webHookMessages, 0);
        Assert.That(contract.ClientName, Is.EqualTo(settings.ClientName));
        Assert.That(contract.Instance, Is.Null);
        Assert.That(contract.Link, Is.Not.Null);
        Assert.That(contract.RegistrationType, Is.EqualTo(RegistrationType.New));
        Assert.That(string.Join(",", contract.Settings.Order()), Is.EqualTo($"{nameof(settings.AnotherAddress)},{nameof(settings.WebsiteAddress)}"));
    }

    [Test]
    public async Task ShallSendUpdatedClientRegistrationWebHook()
    {
        var testStart = DateTime.UtcNow;
        var client = await CreateTestWebHookClient(WebHookSecret);

        var webHook = new WebHookDataContract(null, client.Id.Value, WebHookType.UpdatedClientRegistration, ".*", ".*", 1);
        await CreateWebHook(webHook);

        var secret = GetNewSecret();
        await RegisterSettings<ClientXWithTwoSettings>(secret);
        var settings = await RegisterSettings<ClientXWithThreeSettings>(secret);
        
        await WaitForCondition(async () => (await GetWebHookMessages(testStart)).Count() == 1, TimeSpan.FromSeconds(1));
        var webHookMessages = (await GetWebHookMessages(testStart)).ToList();

        var contract = GetMessageOfType<ClientRegistrationDataContract>(webHookMessages, 0);
        Assert.That(contract.ClientName, Is.EqualTo(settings.ClientName));
        Assert.That(contract.Instance, Is.Null);
        Assert.That(contract.Link, Is.Not.Null);
        Assert.That(contract.RegistrationType, Is.EqualTo(RegistrationType.Updated));
        Assert.That(string.Join(",", contract.Settings.Order()), Is.EqualTo($"{nameof(settings.DateOfBirth)},{nameof(settings.IsCool)},{nameof(settings.SingleStringSetting)}"));
    }

    [Test]
    public async Task ShallSendClientStatusChangedWebHookWhenConnected()
    {
        var testStart = DateTime.UtcNow;
        var client = await CreateTestWebHookClient(WebHookSecret);

        var webHook = new WebHookDataContract(null, client.Id.Value, WebHookType.ClientStatusChanged, ".*", ".*", 1);
        await CreateWebHook(webHook);
        
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);

        var clientStatus = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 5000, true);
        await GetStatus("ThreeSettings", secret, clientStatus);
        
        await WaitForCondition(async () => (await GetWebHookMessages(testStart)).Count() == 1, TimeSpan.FromSeconds(1));
        var webHookMessages = (await GetWebHookMessages(testStart)).ToList();

        var contract = GetMessageOfType<ClientStatusChangedDataContract>(webHookMessages, 0);
        Assert.That(contract.ClientName, Is.EqualTo(settings.ClientName));
        Assert.That(contract.Instance, Is.Null);
        Assert.That(contract.Link, Is.Not.Null);
        Assert.That(contract.ConnectionEvent, Is.EqualTo(ConnectionEvent.Connected));
    }
    
    [Test]
    public async Task ShallSendClientStatusChangedWebHookWhenDisconnected()
    {
        var testStart = DateTime.UtcNow;
        var client = await CreateTestWebHookClient(WebHookSecret);

        var webHook = new WebHookDataContract(null, client.Id.Value, WebHookType.ClientStatusChanged, ".*", ".*", 1);
        await CreateWebHook(webHook);
        
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);

        var clientStatus = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 10, true);
        await GetStatus("ThreeSettings", secret, clientStatus);

        await Task.Delay(30);
        await GetStatus("ThreeSettings", secret, clientStatus);
        
        await WaitForCondition(async () => (await GetWebHookMessages(testStart)).Count() == 3, TimeSpan.FromSeconds(1));
        var webHookMessages = (await GetWebHookMessages(testStart)).ToList();

        var contract = GetMessageOfType<ClientStatusChangedDataContract>(webHookMessages, 1);
        Assert.That(contract.ClientName, Is.EqualTo(settings.ClientName));
        Assert.That(contract.Instance, Is.Null);
        Assert.That(contract.Link, Is.Not.Null);
        Assert.That(contract.ConnectionEvent, Is.EqualTo(ConnectionEvent.Disconnected));
    }

    [Test]
    public async Task ShallSendSettingValueChangedWebHook()
    {
        var testStart = DateTime.UtcNow;
        var client = await CreateTestWebHookClient(WebHookSecret);

        var webHook = new WebHookDataContract(null, client.Id.Value, WebHookType.SettingValueChanged, ".*", ".*", 1);
        await CreateWebHook(webHook);
        
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        
        const string newValue = "Some new value";
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract(newValue))
        };

        await SetSettings(settings.ClientName, settingsToUpdate);
        
        await WaitForCondition(async () => (await GetWebHookMessages(testStart)).Count() == 1, TimeSpan.FromSeconds(1));
        var webHookMessages = (await GetWebHookMessages(testStart)).ToList();

        var contract = GetMessageOfType<SettingValueChangedDataContract>(webHookMessages, 0);
        Assert.That(contract.ClientName, Is.EqualTo(settings.ClientName));
        Assert.That(contract.Instance, Is.Null);
        Assert.That(contract.Link, Is.Not.Null);
        Assert.That(contract.Username, Is.EqualTo(UserName));
        Assert.That(contract.UpdatedSettings.Single(), Is.EqualTo(nameof(settings.AStringSetting)));
    }

    [Test]
    public async Task ShallSendMemoryLeakDetectedWebHook()
    {
        await SetConfiguration(CreateConfiguration(analyzeMemoryUsage: true));
        var testStart = DateTime.UtcNow;
        var client = await CreateTestWebHookClient(WebHookSecret);

        var webHook = new WebHookDataContract(null, client.Id.Value, WebHookType.MemoryLeakDetected, ".*", ".*", 1);
        await CreateWebHook(webHook);
        
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);

        await SetConfiguration(CreateConfiguration(delayBeforeMemoryLeakMeasurementsMs: 0, intervalBetweenMemoryLeakChecksMs: 200, minimumDataPointsForMemoryLeakCheck: 15, analyzeMemoryUsage: true));

        var startTime = DateTime.UtcNow;
        var initialStatus = CreateStatusRequest(startTime, DateTime.UtcNow, 5000, true, memoryUsageBytes: 1);
        await GetStatus(settings.ClientName, secret, initialStatus);
        
        for (var i = 0; i < 15; i++)
        {
            var clientStatus = CreateStatusRequest(startTime, DateTime.UtcNow, 5000, true, runSessionId: initialStatus.RunSessionId, memoryUsageBytes: (i + 5) * i);
            await GetStatus(settings.ClientName, secret, clientStatus);
        }

        await WaitForCondition(async () => (await GetWebHookMessages(testStart)).Count() == 1, TimeSpan.FromSeconds(5));
        var webHookMessages = (await GetWebHookMessages(testStart)).ToList();

        var contract = GetMessageOfType<MemoryLeakDetectedDataContract>(webHookMessages, 0);
        Assert.That(contract.ClientName, Is.EqualTo(settings.ClientName));
        Assert.That(contract.Instance, Is.Null);
        Assert.That(contract.Link, Is.Not.Null);
        Assert.That(contract.DataPointsAnalyzed, Is.EqualTo(15));
    }

    [Test]
    [Retry(3)]
    public async Task ShallSendMinRunSessionsWebHook()
    {
        var testStart = DateTime.UtcNow;
        var client = await CreateTestWebHookClient(WebHookSecret);

        var webHook = new WebHookDataContract(null, client.Id.Value, WebHookType.MinRunSessions, ".*", ".*", 2);
        await CreateWebHook(webHook);
        
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);

        await SetConfiguration(CreateConfiguration(delayBeforeMemoryLeakMeasurementsMs: 0, intervalBetweenMemoryLeakChecksMs: 200, minimumDataPointsForMemoryLeakCheck: 15));

        var runSession1 = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 1000, true, memoryUsageBytes: 1);
        await GetStatus(settings.ClientName, secret, runSession1);

        var runSession2 = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 50, true, memoryUsageBytes: 1);
        await GetStatus(settings.ClientName, secret, runSession2);

        await WaitForCondition(async () => (await GetWebHookMessages(testStart)).Count() == 1, TimeSpan.FromSeconds(1), 
            () => "Minimum restored web hook should have been sent..");

        // When getting status for this new run session, the original sessions will be removed.
        // Note new session status update is required. It won't work with a single session only running.
        var runSession3 = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 1000, true);
        await GetStatus(settings.ClientName, secret, runSession3);
        
        await WaitForCondition(async () => (await GetAllStatuses()).SelectMany(a => a.RunSessions).Count() == 2, TimeSpan.FromSeconds(1), 
            () => $"First and third run sessions should remain. Was {GetAllStatuses().Result.SelectMany(a => a.RunSessions).Count()} run sessions");

        await WaitForCondition(async () => (await GetWebHookMessages(testStart)).Count() == 3, TimeSpan.FromSeconds(1), 
            () => $"Expected 3 web hook messages, but was {GetWebHookMessages(testStart).Result.Count()}.");
        
        var webHookMessages = (await GetWebHookMessages(testStart)).ToList();

        var minRestoredContract = GetMessageOfType<MinRunSessionsDataContract>(webHookMessages, 0);
        Assert.That(minRestoredContract.ClientName, Is.EqualTo(settings.ClientName));
        Assert.That(minRestoredContract.Instance, Is.Null);
        Assert.That(minRestoredContract.Link, Is.Not.Null);
        Assert.That(minRestoredContract.RunSessions, Is.EqualTo(2));
        Assert.That(minRestoredContract.RunSessionsEvent, Is.EqualTo(RunSessionsEvent.MinimumRestored));
        
        var belowMinimumContract = GetMessageOfType<MinRunSessionsDataContract>(webHookMessages, 1);
        Assert.That(belowMinimumContract.ClientName, Is.EqualTo(settings.ClientName));
        Assert.That(belowMinimumContract.Instance, Is.Null);
        Assert.That(belowMinimumContract.Link, Is.Not.Null);
        Assert.That(belowMinimumContract.RunSessions, Is.EqualTo(1));
        Assert.That(belowMinimumContract.RunSessionsEvent, Is.EqualTo(RunSessionsEvent.BelowMinimum));
        
        var minRestoredContract2 = GetMessageOfType<MinRunSessionsDataContract>(webHookMessages, 2);
        Assert.That(minRestoredContract2.ClientName, Is.EqualTo(settings.ClientName));
        Assert.That(minRestoredContract2.Instance, Is.Null);
        Assert.That(minRestoredContract2.Link, Is.Not.Null);
        Assert.That(minRestoredContract2.RunSessions, Is.EqualTo(2));
        Assert.That(minRestoredContract2.RunSessionsEvent, Is.EqualTo(RunSessionsEvent.MinimumRestored));
    }

    [Test]
    public async Task ShallSendConfigurationErrorWebHook()
    {
        var testStart = DateTime.UtcNow;
        const string errorMessage = "BadSetting";
        const string appVersion = "v3";
        const string figVersion = "v4";
        var client = await CreateTestWebHookClient(WebHookSecret);

        var webHook = new WebHookDataContract(null, client.Id.Value, WebHookType.ConfigurationError, ".*", ".*", 2);
        await CreateWebHook(webHook);
        
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        
        var status = CreateStatusRequest(FiveHundredMillisecondsAgo(),
            DateTime.UtcNow,
            10000,
            true,
            hasConfigurationError: true,
            configurationErrors: new List<string>()
            {
                errorMessage
            },
            appVersion: appVersion,
            figVersion: figVersion);
        await GetStatus(settings.ClientName, secret, status);
        
        await WaitForCondition(async () => (await GetWebHookMessages(testStart)).Count() == 1, TimeSpan.FromSeconds(1));
        
        var noErrorStatus = CreateStatusRequest(FiveHundredMillisecondsAgo(),
            DateTime.UtcNow,
            10000,
            true,
            hasConfigurationError: false,
            runSessionId: status.RunSessionId);
        await GetStatus(settings.ClientName, secret, noErrorStatus);
        
        await WaitForCondition(async () => (await GetWebHookMessages(testStart)).Count() == 2, TimeSpan.FromSeconds(1));
        
        var webHookMessages = (await GetWebHookMessages(testStart)).ToList();
        var configErrorMessage = GetMessageOfType<ClientConfigurationErrorDataContract>(webHookMessages, 0);
        Assert.That(configErrorMessage.ClientName, Is.EqualTo(settings.ClientName));
        Assert.That(configErrorMessage.Instance, Is.EqualTo(null));
        Assert.That(configErrorMessage.Link, Is.Not.Null);
        Assert.That(configErrorMessage.Status, Is.EqualTo(ConfigurationErrorStatus.Error));
        Assert.That(configErrorMessage.ConfigurationErrors.Single(), Is.EqualTo(errorMessage));
        Assert.That(configErrorMessage.ApplicationVersion, Is.EqualTo(appVersion));
        Assert.That(configErrorMessage.FigVersion, Is.EqualTo(figVersion));
        
        var resolvedMessage = GetMessageOfType<ClientConfigurationErrorDataContract>(webHookMessages, 1);
        Assert.That(resolvedMessage.Status, Is.EqualTo(ConfigurationErrorStatus.Resolved));
        Assert.That(resolvedMessage.ConfigurationErrors.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task ShallNotSendWebHookIfClientNameDoesNotMatchRegex()
    {
        var testStart = DateTime.UtcNow;
        var client = await CreateTestWebHookClient(WebHookSecret);

        var webHook = new WebHookDataContract(null, client.Id.Value, WebHookType.NewClientRegistration, "ThreeSettings", ".*", 1);
        await CreateWebHook(webHook);

        await RegisterSettings<ClientA>();
        var matchingSettings = await RegisterSettings<ThreeSettings>();

        await WaitForCondition(async () => (await GetWebHookMessages(testStart)).Count() == 1, TimeSpan.FromSeconds(1));

        var webHookMessages = (await GetWebHookMessages(testStart)).ToList();
        
        var contract = GetMessageOfType<ClientRegistrationDataContract>(webHookMessages, 0);
        Assert.That(contract.ClientName, Is.EqualTo(matchingSettings.ClientName));
    }

    [Test]
    public async Task ShallNotSendSettingValueChangedWebHookIfClientMatchesButAllChangedSettingsDoNot()
    {
        var testStart = DateTime.UtcNow;
        var client = await CreateTestWebHookClient(WebHookSecret);

        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        
        var webHook = new WebHookDataContract(null, client.Id.Value, WebHookType.SettingValueChanged, ".*", nameof(settings.ABoolSetting), 1);
        await CreateWebHook(webHook);
        
        var nonMatchingUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract("Some new value"))
        };

        await SetSettings(settings.ClientName, nonMatchingUpdate);
        
        var matchingUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.ABoolSetting), new BoolSettingDataContract(false))
        };

        await SetSettings(settings.ClientName, matchingUpdate);
        
        await WaitForCondition(async () => (await GetWebHookMessages(testStart)).Count() == 1, TimeSpan.FromSeconds(1));
        var webHookMessages = (await GetWebHookMessages(testStart)).ToList();

        var contract = GetMessageOfType<SettingValueChangedDataContract>(webHookMessages, 0);
        Assert.That(contract.ClientName, Is.EqualTo(settings.ClientName));
        Assert.That(contract.UpdatedSettings.Single(), Is.EqualTo(nameof(settings.ABoolSetting)));
    }

    [Test]
    public async Task ShallNotSendWebHookForNonUpdatedClientRegistration()
    {
        var testStart = DateTime.UtcNow;
        
        var client = await CreateTestWebHookClient(WebHookSecret);

        var webHook = new WebHookDataContract(null, client.Id.Value, WebHookType.NewClientRegistration, ".*", ".*", 1);
        await CreateWebHook(webHook);

        var secret = GetNewSecret();
        await RegisterSettings<ClientA>(secret);

        // Second, unchanged registration
        await RegisterSettings<ClientA>(secret);

        await Task.Delay(50);

        var webHookMessages = (await GetWebHookMessages(testStart)).ToList();
        
        Assert.That(webHookMessages.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task ShallOnlyReturnMatchingSettingsIfMoreThanOneSettingUpdated()
    {
        var testStart = DateTime.UtcNow;
        var client = await CreateTestWebHookClient(WebHookSecret);

        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        
        var webHook = new WebHookDataContract(null, client.Id.Value, WebHookType.SettingValueChanged, ".*", nameof(settings.ABoolSetting), 1);
        await CreateWebHook(webHook);
        
        var settingUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract("Some new value")),
            new(nameof(settings.ABoolSetting), new BoolSettingDataContract(false))
        };

        await SetSettings(settings.ClientName, settingUpdate);

        await WaitForCondition(async () => (await GetWebHookMessages(testStart)).Count() == 1, TimeSpan.FromSeconds(1));
        var webHookMessages = (await GetWebHookMessages(testStart)).ToList();

        var contract = GetMessageOfType<SettingValueChangedDataContract>(webHookMessages, 0);
        Assert.That(contract.ClientName, Is.EqualTo(settings.ClientName));
        Assert.That(contract.UpdatedSettings.Count, Is.EqualTo(1));
        Assert.That(contract.UpdatedSettings.Single(), Is.EqualTo(nameof(settings.ABoolSetting)));
    }

    [Test]
    public async Task ShallTestAllWebHookTypes()
    {
        var testStart = DateTime.UtcNow;
        var client = await CreateTestWebHookClient(WebHookSecret);

        var result = await RunWebHookClientsTests(client);
        
        Assert.That(result.ClientName, Is.EqualTo(client.Name));
        Assert.That(result.Results.Count, Is.EqualTo(7));

        foreach (var webHookType in Enum.GetValues(typeof(WebHookType)).Cast<WebHookType>())
        {
            var match = result.Results.FirstOrDefault(a => a.WebHookType == webHookType);
            Assert.That(match.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }
        
        var webHookMessages = (await GetWebHookMessages(testStart)).ToList();
        Assert.That(webHookMessages.Count, Is.EqualTo(7));
        GetMessageOfType<ClientStatusChangedDataContract>(webHookMessages, 0);
        GetMessageOfType<SettingValueChangedDataContract>(webHookMessages, 1);
        GetMessageOfType<MemoryLeakDetectedDataContract>(webHookMessages, 2);
        var newRegistrationContract = GetMessageOfType<ClientRegistrationDataContract>(webHookMessages, 3);
        Assert.That(newRegistrationContract.RegistrationType, Is.EqualTo(RegistrationType.New));
        var updatedRegistrationContract = GetMessageOfType<ClientRegistrationDataContract>(webHookMessages, 4);
        Assert.That(updatedRegistrationContract.RegistrationType, Is.EqualTo(RegistrationType.Updated));
        GetMessageOfType<MinRunSessionsDataContract>(webHookMessages, 5);
        GetMessageOfType<ClientConfigurationErrorDataContract>(webHookMessages, 6);
    }

    private async Task<WebHookClientTestResultsDataContract> RunWebHookClientsTests(WebHookClientDataContract client)
    {
        string uri = $"/webhookclient/{client.Id}/test";
        return await ApiClient.Put<WebHookClientTestResultsDataContract>(uri, null, authenticate: true);
    }

    private T GetMessageOfType<T>(IEnumerable<object> messages, int index)
    {
        var message = messages.ToList()[index];
        Assert.That(message, Is.Not.Null);
        var result = JsonConvert.DeserializeObject<T>(message.ToString()!);
        Assert.That(result, Is.Not.Null);
        return result!;
    }
}