using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Fig.Contracts.Health;
using Fig.Contracts.Settings;
using Fig.Contracts.WebHook;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using Fig.WebHooks.Contracts;
using Microsoft.Extensions.DependencyInjection;
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

        var webHook = new WebHookDataContract(null, client.Id!.Value, WebHookType.NewClientRegistration, ".*", ".*", 1);
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

        var webHook = new WebHookDataContract(null, client.Id!.Value, WebHookType.UpdatedClientRegistration, ".*", ".*", 1);
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

        var webHook = new WebHookDataContract(null, client.Id!.Value, WebHookType.ClientStatusChanged, ".*", ".*", 1);
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

        var webHook = new WebHookDataContract(null, client.Id!.Value, WebHookType.ClientStatusChanged, ".*", ".*", 1);
        await CreateWebHook(webHook);
        
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);

        var clientStatus = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 10, true);
        await GetStatus("ThreeSettings", secret, clientStatus);

        // Wait for session to expire (pollInterval=10ms, grace period=2*10+50=70ms)
        await Task.Delay(100);
        
        // Manually trigger session cleanup to expire the session and trigger disconnected webhook
        using (var scope = GetServiceScope())
        {
            var sessionCleanupService = scope.ServiceProvider.GetRequiredService<Fig.Api.Services.ISessionCleanupService>();
            await sessionCleanupService.RemoveExpiredSessionsAsync();
        }
        
        // Wait for background worker to process the queued webhooks
        await WaitForCondition(async () => (await GetWebHookMessages(testStart)).Count() == 2, TimeSpan.FromSeconds(3));
        var webHookMessages = (await GetWebHookMessages(testStart)).ToList();

        var connectedContract = GetMessageOfType<ClientStatusChangedDataContract>(webHookMessages, 0);
        Assert.That(connectedContract.ClientName, Is.EqualTo(settings.ClientName));
        Assert.That(connectedContract.Instance, Is.Null);
        Assert.That(connectedContract.Link, Is.Not.Null);
        Assert.That(connectedContract.ConnectionEvent, Is.EqualTo(ConnectionEvent.Connected));
        
        var disconnectedContract = GetMessageOfType<ClientStatusChangedDataContract>(webHookMessages, 1);
        Assert.That(disconnectedContract.ClientName, Is.EqualTo(settings.ClientName));
        Assert.That(disconnectedContract.Instance, Is.Null);
        Assert.That(disconnectedContract.Link, Is.Not.Null);
        Assert.That(disconnectedContract.ConnectionEvent, Is.EqualTo(ConnectionEvent.Disconnected));
    }

    [Test]
    public async Task ShallSendSettingValueChangedWebHook()
    {
        var testStart = DateTime.UtcNow;
        var client = await CreateTestWebHookClient(WebHookSecret);

        var webHook = new WebHookDataContract(null, client.Id!.Value, WebHookType.SettingValueChanged, ".*", ".*", 1);
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
    [Retry(3)]
    public async Task ShallSendMinRunSessionsWebHook()
    {
        var testStart = DateTime.UtcNow;
        var client = await CreateTestWebHookClient(WebHookSecret);

        var webHook = new WebHookDataContract(null, client.Id!.Value, WebHookType.MinRunSessions, ".*", ".*", 2);
        await CreateWebHook(webHook);
        
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);

        var runSession1 = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 1000, true, memoryUsageBytes: 1);
        await GetStatus(settings.ClientName, secret, runSession1);

        var runSession2 = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 50, true, memoryUsageBytes: 1);
        await GetStatus(settings.ClientName, secret, runSession2);

        await WaitForCondition(async () => (await GetWebHookMessages(testStart)).Count() == 1, TimeSpan.FromSeconds(1), 
            () => "Minimum restored web hook should have been sent..");

        // Wait for session2 to expire (pollInterval=50ms, grace period=2*50+50=150ms)
        await Task.Delay(200);
        
        // Manually trigger session cleanup to expire session2 and trigger below minimum webhook
        using (var scope = GetServiceScope())
        {
            var sessionCleanupService = scope.ServiceProvider.GetRequiredService<Fig.Api.Services.ISessionCleanupService>();
            await sessionCleanupService.RemoveExpiredSessionsAsync();
        }
        
        // Wait for background worker to process the queued webhooks
        await WaitForCondition(async () => (await GetWebHookMessages(testStart)).Count() == 2, TimeSpan.FromSeconds(3), 
            () => $"Expected 2 web hook messages after cleanup, but was {GetWebHookMessages(testStart).Result.Count()}.");
        
        // Add new session which should trigger minimum restored webhook
        var runSession3 = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 1000, true);
        await GetStatus(settings.ClientName, secret, runSession3);
        
        await WaitForCondition(async () => (await GetAllStatuses()).SelectMany(a => a.RunSessions).Count() == 2, TimeSpan.FromSeconds(1), 
            () => $"First and third run sessions should remain. Was {GetAllStatuses().Result.SelectMany(a => a.RunSessions).Count()} run sessions");

        // Wait for background worker to process the final minimum restored webhook
        await WaitForCondition(async () => (await GetWebHookMessages(testStart)).Count() == 3, TimeSpan.FromSeconds(3), 
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
    public async Task ShallNotSendWebHookIfClientNameDoesNotMatchRegex()
    {
        var testStart = DateTime.UtcNow;
        var client = await CreateTestWebHookClient(WebHookSecret);

        var webHook = new WebHookDataContract(null, client.Id!.Value, WebHookType.NewClientRegistration, "ThreeSettings", ".*", 1);
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
        
        var webHook = new WebHookDataContract(null, client.Id!.Value, WebHookType.SettingValueChanged, ".*", nameof(settings.ABoolSetting), 1);
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

        var webHook = new WebHookDataContract(null, client.Id!.Value, WebHookType.NewClientRegistration, ".*", ".*", 1);
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
        
        var webHook = new WebHookDataContract(null, client.Id!.Value, WebHookType.SettingValueChanged, ".*", nameof(settings.ABoolSetting), 1);
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
        
        Assert.That(result!.ClientName, Is.EqualTo(client.Name));
        Assert.That(result.Results.Count, Is.EqualTo(Enum.GetNames(typeof(WebHookType)).Length));

        foreach (var webHookType in Enum.GetValues(typeof(WebHookType)).Cast<WebHookType>())
        {
            var match = result.Results.FirstOrDefault(a => a.WebHookType == webHookType);
            Assert.That(match!.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }
        
        var webHookMessages = (await GetWebHookMessages(testStart)).ToList();
        Assert.That(webHookMessages.Count, Is.EqualTo(Enum.GetNames(typeof(WebHookType)).Length));
        GetMessageOfType<ClientStatusChangedDataContract>(webHookMessages, 0);
        GetMessageOfType<SettingValueChangedDataContract>(webHookMessages, 1);
        var newRegistrationContract = GetMessageOfType<ClientRegistrationDataContract>(webHookMessages, 2);
        Assert.That(newRegistrationContract.RegistrationType, Is.EqualTo(RegistrationType.New));
        var updatedRegistrationContract = GetMessageOfType<ClientRegistrationDataContract>(webHookMessages, 3);
        Assert.That(updatedRegistrationContract.RegistrationType, Is.EqualTo(RegistrationType.Updated));
        GetMessageOfType<MinRunSessionsDataContract>(webHookMessages, 4);
        GetMessageOfType<ClientHealthChangedDataContract>(webHookMessages, 5);
        GetMessageOfType<SecurityEventDataContract>(webHookMessages, 6);
    }
    
    [Test]
    public async Task ShallSendHealthStatusChangedWebHook()
    {
        var testStart = DateTime.UtcNow;
        var client = await CreateTestWebHookClient(WebHookSecret);

        var webHook = new WebHookDataContract(null, client.Id!.Value, WebHookType.HealthStatusChanged, ".*", ".*", 1);
        await CreateWebHook(webHook);

        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);

        // Simulate a health status change
        var healthDetails = new HealthDataContract()
        {
            Status = FigHealthStatus.Degraded,
            Components = [new ComponentHealthDataContract("a", FigHealthStatus.Degraded, "CPU high")]
        };
        var status = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 1000, true, health: healthDetails);
        await GetStatus(settings.ClientName, secret, status);

        await WaitForCondition(async () => (await GetWebHookMessages(testStart)).Count() == 1, TimeSpan.FromSeconds(1));
        var webHookMessages = (await GetWebHookMessages(testStart)).ToList();

        var contract = GetMessageOfType<ClientHealthChangedDataContract>(webHookMessages, 0);
        Assert.That(contract.ClientName, Is.EqualTo(settings.ClientName));
        Assert.That(contract.Instance, Is.Null);
        Assert.That(contract.Link, Is.Not.Null);
        Assert.That(contract.Status, Is.EqualTo(HealthStatus.Degraded));
        Assert.That(contract.HealthDetails.Status, Is.EqualTo(HealthStatus.Degraded));
        Assert.That(contract.HealthDetails.Components[0].Message, Is.EqualTo("CPU high"));
    }

    [Test]
    public async Task ShallNotSendHealthStatusChangedWebHookIfClientNameDoesNotMatchRegex()
    {
        var testStart = DateTime.UtcNow;
        var client = await CreateTestWebHookClient(WebHookSecret);

        var webHook = new WebHookDataContract(null, client.Id!.Value, WebHookType.HealthStatusChanged, "ThreeSettings", ".*", 1);
        await CreateWebHook(webHook);

        var secret1 = GetNewSecret();
        var secret2 = GetNewSecret();
        var clientA = await RegisterSettings<ClientA>(secret1);
        var matchingSettings = await RegisterSettings<ThreeSettings>(secret2);

        // Simulate a health status change for ClientA (should not match)
        var healthDetails = new HealthDataContract()
        {
            Status = FigHealthStatus.Degraded,
            Components = [new ComponentHealthDataContract("a", FigHealthStatus.Degraded, "CPU High")]
        };
        var status = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 1000, true, health: healthDetails);
        await GetStatus(clientA.ClientName, secret1, status);

        // Simulate a health status change for ThreeSettings (should match)
        await GetStatus(matchingSettings.ClientName, secret2, status);

        await WaitForCondition(async () => (await GetWebHookMessages(testStart)).Count() == 1, TimeSpan.FromSeconds(1));
        var webHookMessages = (await GetWebHookMessages(testStart)).ToList();

        var contract = GetMessageOfType<ClientHealthChangedDataContract>(webHookMessages, 0);
        Assert.That(contract.ClientName, Is.EqualTo(matchingSettings.ClientName));
    }

    private async Task<WebHookClientTestResultsDataContract?> RunWebHookClientsTests(WebHookClientDataContract client)
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