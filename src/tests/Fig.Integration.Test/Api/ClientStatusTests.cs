using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Fig.Contracts.Settings;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

public class ClientStatusTests : IntegrationTestBase
{
    [Test]
    public async Task ShallSetDefaultValues()
    {
        var secret = GetNewSecret();
        await RegisterSettings<ThreeSettings>(secret);

        var clientStatus = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 5000, true);
        var status = await GetStatus("ThreeSettings", secret, clientStatus);
        
        Assert.That(status.PollIntervalMs, Is.EqualTo(clientStatus.PollIntervalMs));
        Assert.That(status.SettingUpdateAvailable, Is.False);
    }

    [Test]
    public async Task ShallIdentifyWhenValuesAreOutdated()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        var lastUpdate = DateTime.UtcNow;

        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract("aNewValue"))
        };

        await SetSettings(settings.ClientName, settingsToUpdate);

        var clientStatus = CreateStatusRequest(FiveHundredMillisecondsAgo(), lastUpdate, 5000, true);

        var status = await GetStatus(settings.ClientName, secret, clientStatus);

        Assert.That(status.SettingUpdateAvailable, Is.True);
    }
    
    [Test]
    public async Task ShallNotMakeUpdateAvailableIfLiveUpdateIsOff()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        var lastUpdate = DateTime.UtcNow;

        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract("aNewValue"))
        };

        var sessionId = Guid.NewGuid();
        var clientStatus = CreateStatusRequest(FiveHundredMillisecondsAgo(), lastUpdate, 5000, true, runSessionId: sessionId);

        await GetStatus(settings.ClientName, secret, clientStatus);
        
        await SetLiveReload(false, sessionId);
        
        await SetSettings(settings.ClientName, settingsToUpdate);

        var status = await GetStatus(settings.ClientName, secret, clientStatus);

        Assert.That(status.SettingUpdateAvailable, Is.False);
    }

    [Test]
    public async Task ShallEnableAndDisableLiveUpdate()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        var lastUpdate = DateTime.UtcNow;
        
        var sessionId = Guid.NewGuid();
        var clientStatus = CreateStatusRequest(FiveHundredMillisecondsAgo(), lastUpdate, 5000, true, runSessionId: sessionId);

        await GetStatus(settings.ClientName, secret, clientStatus);

        var statuses1 = await GetAllStatuses();
        Assert.That(statuses1.Single().RunSessions.Single().LiveReload, Is.True);
        
        await SetLiveReload(false, sessionId);
        
        var statuses2 = await GetAllStatuses();
        Assert.That(statuses2.Single().RunSessions.Single().LiveReload, Is.False);
        
        await SetLiveReload(true, sessionId);
        
        var statuses3 = await GetAllStatuses();
        Assert.That(statuses3.Single().RunSessions.Single().LiveReload, Is.True);
    }

    [Test]
    public async Task ShallIdentifyWhichSettingsChangedWhenValuesAreOutdated()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        var lastUpdate = DateTime.UtcNow;

        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract("aNewValue")),
            new(nameof(settings.AnIntSetting), new IntSettingDataContract(99))
        };

        await SetSettings(settings.ClientName, settingsToUpdate);

        var clientStatus = CreateStatusRequest(FiveHundredMillisecondsAgo(), lastUpdate, 5000, true);

        var status = await GetStatus(settings.ClientName, secret, clientStatus);

        var events = await GetEvents(lastUpdate, DateTime.UtcNow);

        Assert.That(string.Join(",", status.ChangedSettings ?? new List<string>()), Is.EqualTo($"{nameof(settings.AStringSetting)},{nameof(settings.AnIntSetting)}"));
    }

    [Test]
    public async Task ShallGetAllInstances()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);

        var clientStatus1 = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 3000, true);

        await GetStatus(settings.ClientName, secret, clientStatus1);

        var clientStatus2 = CreateStatusRequest(DateTime.UtcNow - TimeSpan.FromMilliseconds(600), DateTime.UtcNow, 3000, true);

        await GetStatus(settings.ClientName, secret, clientStatus2);

        var statuses = (await GetAllStatuses()).ToList();

        Assert.That(statuses.Count, Is.EqualTo(1));
        Assert.That(statuses.Single().RunSessions.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task ShallRemoveExpiredSessions()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);

        var clientStatus1 = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 50, true);

        await GetStatus(settings.ClientName, secret, clientStatus1);

        await Task.Delay(TimeSpan.FromMilliseconds(200));

        var clientStatus2 = CreateStatusRequest(DateTime.UtcNow - TimeSpan.FromMilliseconds(600), DateTime.UtcNow, 30000, true);

        await GetStatus(settings.ClientName, secret, clientStatus2);

        var statuses = (await GetAllStatuses()).ToList();

        Assert.That(statuses.Count, Is.EqualTo(1));
        Assert.That(statuses.Single().RunSessions.Count, Is.EqualTo(1));
    }
    
    [Test]
    public async Task ShallAcceptBothCurrentAndPreviousSecretsDuringChangePeriod()
    {
        var originalSecret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(originalSecret);

        var updatedSecret = GetNewSecret();
        await ChangeClientSecret(settings.ClientName, updatedSecret, DateTime.UtcNow.AddMinutes(1));
        
        var clientStatus = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 5000, true);
        await GetStatus("ThreeSettings", originalSecret, clientStatus);
        await GetStatus("ThreeSettings", updatedSecret, clientStatus);
    }

    [Test]
    public async Task ShallNotAcceptOldSecretAfterChangePeriodExpiry()
    {
        var originalSecret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(originalSecret);

        var updatedSecret = GetNewSecret();
        await ChangeClientSecret(settings.ClientName, updatedSecret, DateTime.UtcNow);
        
        var clientStatus = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 5000, true);
        var json = JsonConvert.SerializeObject(clientStatus);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Add("clientSecret", originalSecret);
        var response = await httpClient.PutAsync($"statuses/{settings.ClientName}", data);
        
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task ShallFilterClientsForUser()
    {
        var secret = GetNewSecret();
        var threeSettings = await RegisterSettings<ThreeSettings>(secret);
        var clientASettings = await RegisterSettings<ClientA>(secret);
        var clientXSettings = await RegisterSettings<ClientXWithThreeSettings>(secret);
        
        var clientStatus1 = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 800, true);
        await GetStatus(threeSettings.ClientName, secret, clientStatus1);
        await GetStatus(clientASettings.ClientName, secret, clientStatus1);
        await GetStatus(clientXSettings.ClientName, secret, clientStatus1);
        
        var user = NewUser(clientFilter: "ClientA");
        await CreateUser(user);
        var loginResult = await Login(user.Username, user.Password ?? throw new InvalidOperationException("Password is null"));
        
        var statuses = (await GetAllStatuses(loginResult.Token)).ToList();

        Assert.That(statuses.Count, Is.EqualTo(1));
        Assert.That(statuses.Single().Name, Is.EqualTo(clientASettings.ClientName));
    }
    
    [Test]
    public async Task ShallSetRestartRequiredFlagWhenNonDynamicallyUpdatedSettingIsUpdated()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AnIntSetting), new IntSettingDataContract(105))
        };

        var clientStatus = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 5000, true);

        await GetStatus(settings.ClientName, secret, clientStatus);
        
        await SetSettings(settings.ClientName, settingsToUpdate);

        var statuses = (await GetAllStatuses()).ToList();
        
        Assert.That(statuses.Count, Is.EqualTo(1));
        Assert.That(statuses[0].RunSessions.Count, Is.EqualTo(1));
        Assert.That(statuses[0].RunSessions.Single().RestartRequiredToApplySettings, Is.EqualTo(true));
    }
    
    [Test]
    public async Task ShallNotSetRestartRequiredFlagWhenDynamicallyUpdatedSettingIsUpdated()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract("105"))
        };

        var clientStatus = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 5000, true);

        await GetStatus(settings.ClientName, secret, clientStatus);
        
        await SetSettings(settings.ClientName, settingsToUpdate);

        var statuses = (await GetAllStatuses()).ToList();
        
        Assert.That(statuses.Count, Is.EqualTo(1));
        Assert.That(statuses[0].RunSessions.Count, Is.EqualTo(1));
        Assert.That(statuses[0].RunSessions.Single().RestartRequiredToApplySettings, Is.EqualTo(false));
    }

    [Test]
    public async Task ShallSendRestartToClient()
    {
        await SetConfiguration(CreateConfiguration(pollIntervalOverrideMs: 1000));
        var secret = GetNewSecret();
        var (settings, _) = InitializeConfigurationProvider<ThreeSettings>(secret);

        var statuses = await GetAllStatuses();
        Assert.That(settings.CurrentValue.RestartRequested, Is.False);
        
        await RequestRestart(statuses.Single().RunSessions.Single().RunSessionId);
        
        await WaitForCondition(() => Task.FromResult(settings.CurrentValue.RestartRequested), TimeSpan.FromSeconds(10));
        Assert.That(settings.CurrentValue.RestartRequested, Is.True);
    }
    
    [Test]
    public async Task ShallNotInitializeTwoProvidersWithTheSameName()
    {
        await SetConfiguration(CreateConfiguration(pollIntervalOverrideMs: 1000));
        var secret = GetNewSecret();
        var (settings, _) = InitializeConfigurationProvider<ThreeSettings>(secret);
        var (settings2, _) = InitializeConfigurationProvider<ThreeSettings>(secret);

        var statuses = (await GetAllStatuses()).ToList();
        Assert.That(statuses.Count, Is.EqualTo(1));
        var runSessions = statuses.SelectMany(a => a.RunSessions).Count();
        Assert.That(runSessions, Is.EqualTo(1));
    }
    
    [Test]
    public async Task ShallInitializeTwoProviderWithDifferentNames()
    {
        await SetConfiguration(CreateConfiguration(pollIntervalOverrideMs: 1000));
        var secret = GetNewSecret();
        var (settings, _) = InitializeConfigurationProvider<ThreeSettings>(secret);
        var (settings2, _) = InitializeConfigurationProvider<ClientA>(secret);

        var statuses = await GetAllStatuses();

        var runSessions = statuses.SelectMany(a => a.RunSessions).Count();
        Assert.That(runSessions,Is.EqualTo(2));
    }
    
    [Test]
    public async Task ShallSupportConcurrentRequests()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        var lastUpdate = DateTime.UtcNow;

        var clientStatus = CreateStatusRequest(FiveHundredMillisecondsAgo(), lastUpdate, 5000, true);

        List<Task> tasks = new();
        for (var i = 0; i < 5; i++)
        {
            Task task = Task.Run(async () => await GetStatus(settings.ClientName, secret, clientStatus));
            tasks.Add(task);
        }

        await Task.WhenAll(tasks.ToArray());
    }
    
    [Test]
    public async Task ShallSetLastRunSessionDisconnectedWhenLastSessionExpires()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        
        // Create a session that will expire quickly
        var clientStatus = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 50, true);
        await GetStatus(settings.ClientName, secret, clientStatus);
        
        // Wait for the session to expire
        await Task.Delay(TimeSpan.FromMilliseconds(200));
        
        // Trigger expiration by getting status with a different session
        var newClientStatus = CreateStatusRequest(DateTime.UtcNow - TimeSpan.FromMilliseconds(600), DateTime.UtcNow, 30000, true);
        await GetStatus(settings.ClientName, secret, newClientStatus);
        
        // Get all statuses
        var statuses = (await GetAllStatuses()).ToList();
        var clientStatus1 = statuses.FirstOrDefault(a => a.Name == settings.ClientName);
        
        Assert.That(clientStatus1, Is.Not.Null);
        Assert.That(clientStatus1!.RunSessions.Count, Is.EqualTo(1));
        
        // Now expire the last session
        await Task.Delay(TimeSpan.FromMilliseconds(200));
        
        // Trigger another check - this time with no sessions running
        var thirdClientStatus = CreateStatusRequest(DateTime.UtcNow, DateTime.UtcNow, 30000, true);
        await GetStatus(settings.ClientName, secret, thirdClientStatus);
        
        // Get the client status again
        var updatedStatuses = (await GetAllStatuses()).ToList();
        var updatedClientStatus = updatedStatuses.FirstOrDefault(a => a.Name == settings.ClientName);
        
        Assert.That(updatedClientStatus, Is.Not.Null);
        Assert.That(updatedClientStatus!.LastRunSessionDisconnected, Is.Not.Null, "LastRunSessionDisconnected should be set when last session expires");
        Assert.That(updatedClientStatus.LastRunSessionDisconnected!.Value, Is.GreaterThan(DateTime.UtcNow.AddSeconds(-5)), "LastRunSessionDisconnected should be recent");
        Assert.That(updatedClientStatus.LastRunSessionMachineName, Is.Not.Null.And.Not.Empty, "LastRunSessionMachineName should be set");
    }
    
    [Test]
    public async Task ShallNotSetLastRunSessionDisconnectedIfSessionsStillRunning()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        
        // Create two sessions
        var clientStatus1 = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 5000, true);
        await GetStatus(settings.ClientName, secret, clientStatus1);
        
        var clientStatus2 = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 5000, true);
        await GetStatus(settings.ClientName, secret, clientStatus2);
        
        // Get statuses
        var statuses = (await GetAllStatuses()).ToList();
        var clientStatus = statuses.FirstOrDefault(a => a.Name == settings.ClientName);
        
        Assert.That(clientStatus, Is.Not.Null);
        Assert.That(clientStatus!.RunSessions.Count, Is.EqualTo(2));
        Assert.That(clientStatus.LastRunSessionDisconnected, Is.Null, "LastRunSessionDisconnected should not be set while sessions are still running");
    }
    
    [Test]
    public async Task ShallUpdateLastRunSessionDisconnectedWhenLastSessionDisconnects()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        
        // Create a session with short poll interval
        var clientStatus = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 50, true);
        await GetStatus(settings.ClientName, secret, clientStatus);
        
        // Wait for session to expire
        await Task.Delay(TimeSpan.FromMilliseconds(200));
        
        // Create another session to trigger expiration check
        var newStatus = CreateStatusRequest(DateTime.UtcNow, DateTime.UtcNow, 30000, true);
        await GetStatus(settings.ClientName, secret, newStatus);
        
        // Get statuses
        var statuses = (await GetAllStatuses()).ToList();
        var clientStatusBeforeExpire = statuses.FirstOrDefault(a => a.Name == settings.ClientName);
        
        Assert.That(clientStatusBeforeExpire, Is.Not.Null);
        Assert.That(clientStatusBeforeExpire!.RunSessions.Count, Is.EqualTo(1));
        
        // Now wait for the second session to expire
        await Task.Delay(TimeSpan.FromMilliseconds(200));
        
        // Create a final session to trigger the expiration check
        var finalStatus = CreateStatusRequest(DateTime.UtcNow, DateTime.UtcNow, 30000, true);
        await GetStatus(settings.ClientName, secret, finalStatus);
        
        // Get updated statuses
        var updatedStatuses = (await GetAllStatuses()).ToList();
        var updatedClientStatus = updatedStatuses.FirstOrDefault(a => a.Name == settings.ClientName);
        
        Assert.That(updatedClientStatus, Is.Not.Null);
        Assert.That(updatedClientStatus!.LastRunSessionDisconnected, Is.Not.Null, "LastRunSessionDisconnected should be set after last session disconnects");
        Assert.That(updatedClientStatus.LastRunSessionMachineName, Is.Not.Null.And.Not.Empty, "LastRunSessionMachineName should be set and not be empty");
    }
}