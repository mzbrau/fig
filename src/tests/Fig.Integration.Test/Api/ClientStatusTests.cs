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
    public async Task ShallHandleConcurrentInstanceRegistrationAndStatusSync()
    {
        // This test reproduces the reported bug: two DIFFERENT instances starting simultaneously
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        
        // Update settings to non-default values
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract("CustomValue")),
            new(nameof(settings.AnIntSetting), new IntSettingDataContract(42)),
            new(nameof(settings.ABoolSetting), new BoolSettingDataContract(false))
        };
        await SetSettings(settings.ClientName, settingsToUpdate);
        
        var lastUpdate = DateTime.UtcNow;
        
        // Create multiple concurrent status sync requests from DIFFERENT instances
        // This simulates docker-compose starting multiple containers simultaneously
        var tasks = new List<Task>();
        for (var i = 0; i < 10; i++)
        {
            var instance = $"instance{i}";
            var clientStatus = CreateStatusRequest(FiveHundredMillisecondsAgo(), lastUpdate, 5000, true);
            tasks.Add(Task.Run(async () => await GetStatus(settings.ClientName, secret, clientStatus, instance)));
        }

        await Task.WhenAll(tasks);

        // Verify base client settings are still intact
        var retrievedSettings = await GetSettingsForClient(settings.ClientName, secret);
        Assert.That(retrievedSettings.Count, Is.EqualTo(3), "Settings should not be deleted");
        Assert.That(retrievedSettings.Single(s => s.Name == nameof(settings.AStringSetting)).Value?.GetValue() as string, 
            Is.EqualTo("CustomValue"), "String setting should retain its value");
        Assert.That(retrievedSettings.Single(s => s.Name == nameof(settings.AnIntSetting)).Value?.GetValue(), 
            Is.EqualTo(42), "Int setting should retain its value");
        Assert.That(retrievedSettings.Single(s => s.Name == nameof(settings.ABoolSetting)).Value?.GetValue(), 
            Is.EqualTo(false), "Bool setting should retain its value");
    }
    
    [Test]
    public async Task ShallPreserveSettingsWhenInstancesRegisterConcurrently()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        
        // Set custom values for base client
        var customSettings = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract("PreserveMe")),
            new(nameof(settings.AnIntSetting), new IntSettingDataContract(999))
        };
        await SetSettings(settings.ClientName, customSettings);
        
        // Create two instances with overrides
        var instance1Settings = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract("Instance1Value"))
        };
        var instance2Settings = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract("Instance2Value"))
        };
        
        // Set instance-specific settings
        await SetSettings(settings.ClientName, instance1Settings, instance: "instance1");
        await SetSettings(settings.ClientName, instance2Settings, instance: "instance2");
        
        var lastUpdate = DateTime.UtcNow;
        
        // Simulate MANY concurrent status syncs from different instances
        // This increases likelihood of race condition
        var tasks = new List<Task>();
        for (var i = 0; i < 20; i++)
        {
            var instance = i % 2 == 0 ? "instance1" : "instance2";
            var status = CreateStatusRequest(FiveHundredMillisecondsAgo(), lastUpdate, 5000, true);
            tasks.Add(Task.Run(async () => await GetStatus(settings.ClientName, secret, status, instance)));
        }
        
        await Task.WhenAll(tasks);
        
        // Verify base client settings are preserved
        var baseSettings = await GetSettingsForClient(settings.ClientName, secret);
        Assert.That(baseSettings.Count, Is.EqualTo(3), "Base client should have all settings");
        Assert.That(baseSettings.Single(s => s.Name == nameof(settings.AStringSetting)).Value?.GetValue() as string, 
            Is.EqualTo("PreserveMe"), "Base client setting should be preserved");
        Assert.That(baseSettings.Single(s => s.Name == nameof(settings.AnIntSetting)).Value?.GetValue(), 
            Is.EqualTo(999), "Base client int setting should be preserved");
        
        // Verify instance-specific settings are preserved
        var instance1ClientSettings = await GetSettingsForClient(settings.ClientName, secret, instance: "instance1");
        Assert.That(instance1ClientSettings.Count, Is.EqualTo(3), "Instance1 should have all settings");
        Assert.That(instance1ClientSettings.Single(s => s.Name == nameof(settings.AStringSetting)).Value?.GetValue() as string, 
            Is.EqualTo("Instance1Value"), "Instance1 override should be preserved");
        
        var instance2ClientSettings = await GetSettingsForClient(settings.ClientName, secret, instance: "instance2");
        Assert.That(instance2ClientSettings.Count, Is.EqualTo(3), "Instance2 should have all settings");
        Assert.That(instance2ClientSettings.Single(s => s.Name == nameof(settings.AStringSetting)).Value?.GetValue() as string, 
            Is.EqualTo("Instance2Value"), "Instance2 override should be preserved");
    }
    
    [Test]
    public async Task ShallHandleMultipleInstancesConcurrentlyUpdatingSettings()
    {
        // Test the edge case where status sync and settings update happen concurrently
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        
        var lastUpdate = DateTime.UtcNow;
        
        // Start multiple DIFFERENT instances syncing status concurrently
        var statusTasks = new List<Task>();
        for (var i = 0; i < 15; i++)
        {
            var instance = $"instance{i}";
            var status = CreateStatusRequest(FiveHundredMillisecondsAgo(), lastUpdate, 5000, true);
            statusTasks.Add(Task.Run(async () => await GetStatus(settings.ClientName, secret, status, instance)));
        }
        
        // While instances are syncing, update settings
        var settingsUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract("ConcurrentUpdate")),
            new(nameof(settings.AnIntSetting), new IntSettingDataContract(777))
        };
        var updateTask = SetSettings(settings.ClientName, settingsUpdate);
        
        // Wait for all operations to complete
        await Task.WhenAll(statusTasks.Cast<Task>().Concat(new[] { updateTask }));
        
        // Verify settings update succeeded and values are correct
        var retrievedSettings = await GetSettingsForClient(settings.ClientName, secret);
        Assert.That(retrievedSettings.Count, Is.EqualTo(3), "All settings should exist");
        Assert.That(retrievedSettings.Single(s => s.Name == nameof(settings.AStringSetting)).Value?.GetValue() as string, 
            Is.EqualTo("ConcurrentUpdate"), "Updated setting should be preserved");
        Assert.That(retrievedSettings.Single(s => s.Name == nameof(settings.AnIntSetting)).Value?.GetValue(), 
            Is.EqualTo(777), "Updated int setting should be preserved");
        
        // Verify no StaleStateException occurred by checking all settings exist
        Assert.That(retrievedSettings.All(s => s.Value != null), Is.True, "No settings should be null");
    }
    
    [Test]
    public async Task ShallNotOrphanSettingsWhenBothEntitiesLoadedSimultaneously()
    {
        // This specifically tests the ClientStatusBusinessEntity / SettingClientBusinessEntity conflict
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        
        // Set unique values we can verify later
        var uniqueValue = Guid.NewGuid().ToString();
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract(uniqueValue)),
            new(nameof(settings.AnIntSetting), new IntSettingDataContract(12345))
        };
        await SetSettings(settings.ClientName, settingsToUpdate);
        
        var lastUpdate = DateTime.UtcNow;
        
        // Force both entity types to load by doing status sync with DIFFERENT instances
        // and settings retrieval concurrently - this maximizes the race condition
        var tasks = new List<Task>();
        
        // Create many status sync requests from different instances (loads ClientStatusBusinessEntity)
        for (var i = 0; i < 8; i++)
        {
            var instance = $"instance{i}";
            var statusRequest = CreateStatusRequest(FiveHundredMillisecondsAgo(), lastUpdate, 5000, true);
            tasks.Add(Task.Run(async () => await GetStatus(settings.ClientName, secret, statusRequest, instance)));
        }
        
        // Concurrently retrieve settings (loads SettingClientBusinessEntity)
        for (var i = 0; i < 3; i++)
        {
            tasks.Add(Task.Run(async () => await GetSettingsForClient(settings.ClientName, secret)));
            tasks.Add(Task.Run(async () => await GetAllClients()));
        }
        
        await Task.WhenAll(tasks);
        
        // Settings should still be intact with correct values
        var finalSettings = await GetSettingsForClient(settings.ClientName, secret);
        Assert.That(finalSettings.Count, Is.EqualTo(3), "Settings should not be orphaned");
        Assert.That(finalSettings.Single(s => s.Name == nameof(settings.AStringSetting)).Value?.GetValue() as string, 
            Is.EqualTo(uniqueValue), "Setting values should be preserved");
        Assert.That(finalSettings.Single(s => s.Name == nameof(settings.AnIntSetting)).Value?.GetValue(), 
            Is.EqualTo(12345), "Int setting should be preserved");
    }
}