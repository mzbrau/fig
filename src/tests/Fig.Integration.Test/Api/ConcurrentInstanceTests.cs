using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fig.Contracts.Settings;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

[TestFixture]
public class ConcurrentInstanceTests : IntegrationTestBase
{
    [Test]
    public async Task ShallNotCorruptSettingsWhenMultipleInstancesSyncStatusConcurrently()
    {
        // Arrange - Register a client with default settings
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        
        // Verify initial settings are set correctly
        var clientBefore = await GetClient(settings.ClientName);
        Assert.That(clientBefore.Settings.Count, Is.EqualTo(3));
        var stringSetting = clientBefore.Settings.First(a => a.Name == nameof(settings.AStringSetting));
        var intSetting = clientBefore.Settings.First(a => a.Name == nameof(settings.AnIntSetting));
        var boolSetting = clientBefore.Settings.First(a => a.Name == nameof(settings.ABoolSetting));
        
        var originalStringValue = stringSetting.Value?.GetValue();
        var originalIntValue = intSetting.Value?.GetValue();
        var originalBoolValue = boolSetting.Value?.GetValue();
        
        Assert.That(originalStringValue, Is.Not.Null);
        Assert.That(originalIntValue, Is.Not.Null);
        Assert.That(originalBoolValue, Is.Not.Null);
        
        // Act - Simulate two instances of the same service starting simultaneously and syncing status
        const string instance1 = "Instance1";
        const string instance2 = "Instance2";
        
        var runSessionId1 = Guid.NewGuid();
        var runSessionId2 = Guid.NewGuid();
        
        // Create status requests for both instances
        var status1 = CreateStatusRequest(DateTime.UtcNow.AddSeconds(-1), DateTime.MinValue, 5000, true, runSessionId: runSessionId1);
        var status2 = CreateStatusRequest(DateTime.UtcNow.AddSeconds(-1), DateTime.MinValue, 5000, true, runSessionId: runSessionId2);
        
        // Simulate multiple concurrent sync status calls from both instances
        var tasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(GetStatus(settings.ClientName, secret, status1, instance1));
            tasks.Add(GetStatus(settings.ClientName, secret, status2, instance2));
        }
        
        // Execute all sync status calls concurrently
        await Task.WhenAll(tasks);
        
        // Wait a bit to ensure all database operations complete
        await Task.Delay(500);
        
        // Assert - Verify settings are NOT corrupted (not set to null/empty)
        var clientAfter = await GetClient(settings.ClientName);
        Assert.That(clientAfter.Settings.Count, Is.EqualTo(3), "Settings count should remain the same");
        
        var stringSettingAfter = clientAfter.Settings.First(a => a.Name == nameof(settings.AStringSetting));
        var intSettingAfter = clientAfter.Settings.First(a => a.Name == nameof(settings.AnIntSetting));
        var boolSettingAfter = clientAfter.Settings.First(a => a.Name == nameof(settings.ABoolSetting));
        
        Assert.That(stringSettingAfter.Value?.GetValue(), Is.EqualTo(originalStringValue), 
            "String setting should not be cleared");
        Assert.That(intSettingAfter.Value?.GetValue(), Is.EqualTo(originalIntValue), 
            "Int setting should not be cleared");
        Assert.That(boolSettingAfter.Value?.GetValue(), Is.EqualTo(originalBoolValue), 
            "Bool setting should not be cleared");
    }
    
    [Test]
    public async Task ShallHandleSimultaneousInstanceStartupAndRestart()
    {
        // Arrange - Register a client
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        
        // Update settings to have specific values
        var updatedSettings = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract("SpecificValue")),
            new(nameof(settings.AnIntSetting), new IntSettingDataContract(42)),
            new(nameof(settings.ABoolSetting), new BoolSettingDataContract(false))
        };
        await SetSettings(settings.ClientName, updatedSettings);
        
        var clientBefore = await GetClient(settings.ClientName);
        var expectedStringValue = clientBefore.Settings.First(a => a.Name == nameof(settings.AStringSetting)).Value?.GetValue();
        var expectedIntValue = clientBefore.Settings.First(a => a.Name == nameof(settings.AnIntSetting)).Value?.GetValue();
        var expectedBoolValue = clientBefore.Settings.First(a => a.Name == nameof(settings.ABoolSetting)).Value?.GetValue();
        
        // Act - Simulate the exact bug scenario:
        // 1. Start two instances using default settings (no instance-specific settings created)
        const string instance1 = "Instance1";
        const string instance2 = "Instance2";
        
        var runSessionId1 = Guid.NewGuid();
        var runSessionId2 = Guid.NewGuid();
        
        var status1 = CreateStatusRequest(DateTime.UtcNow.AddSeconds(-1), DateTime.MinValue, 5000, true, runSessionId: runSessionId1);
        var status2 = CreateStatusRequest(DateTime.UtcNow.AddSeconds(-1), DateTime.MinValue, 5000, true, runSessionId: runSessionId2);
        
        // Initial sync from both instances
        await Task.WhenAll(
            GetStatus(settings.ClientName, secret, status1, instance1),
            GetStatus(settings.ClientName, secret, status2, instance2)
        );
        
        // 2. Simulate restart - both instances sync status again (simulating Fig server restart scenario)
        var status1Updated = CreateStatusRequest(DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow.AddSeconds(-2), 5000, true, runSessionId: runSessionId1);
        var status2Updated = CreateStatusRequest(DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow.AddSeconds(-2), 5000, true, runSessionId: runSessionId2);
        
        await Task.WhenAll(
            GetStatus(settings.ClientName, secret, status1Updated, instance1),
            GetStatus(settings.ClientName, secret, status2Updated, instance2)
        );
        
        // 3. Simulate multiple concurrent status syncs (as would happen with normal polling)
        var concurrentTasks = new List<Task>();
        for (int i = 0; i < 20; i++)
        {
            var statusA = CreateStatusRequest(DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow.AddSeconds(-2), 5000, true, runSessionId: runSessionId1);
            var statusB = CreateStatusRequest(DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow.AddSeconds(-2), 5000, true, runSessionId: runSessionId2);
            
            concurrentTasks.Add(GetStatus(settings.ClientName, secret, statusA, instance1));
            concurrentTasks.Add(GetStatus(settings.ClientName, secret, statusB, instance2));
        }
        
        await Task.WhenAll(concurrentTasks);
        
        await Task.Delay(1000); // Give time for all database operations to complete
        
        // Assert - Settings should NOT be corrupted
        var clientAfter = await GetClient(settings.ClientName);
        Assert.That(clientAfter.Settings.Count, Is.EqualTo(3), "Settings count should remain the same");
        
        var stringSettingAfter = clientAfter.Settings.First(a => a.Name == nameof(settings.AStringSetting));
        var intSettingAfter = clientAfter.Settings.First(a => a.Name == nameof(settings.AnIntSetting));
        var boolSettingAfter = clientAfter.Settings.First(a => a.Name == nameof(settings.ABoolSetting));
        
        Assert.That(stringSettingAfter.Value?.GetValue(), Is.EqualTo(expectedStringValue), 
            "String setting should not be corrupted after concurrent instance operations");
        Assert.That(intSettingAfter.Value?.GetValue(), Is.EqualTo(expectedIntValue), 
            "Int setting should not be corrupted after concurrent instance operations");
        Assert.That(boolSettingAfter.Value?.GetValue(), Is.EqualTo(expectedBoolValue), 
            "Bool setting should not be corrupted after concurrent instance operations");
    }
    
    [Test]
    public async Task ShallHandleConcurrentGetSettingsAndSyncStatusFromMultipleInstances()
    {
        // This test simulates instances both getting settings and syncing status concurrently
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        
        var updatedSettings = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract("TestValue")),
            new(nameof(settings.AnIntSetting), new IntSettingDataContract(99))
        };
        await SetSettings(settings.ClientName, updatedSettings);
        
        var clientBefore = await GetClient(settings.ClientName);
        var originalValues = clientBefore.Settings.ToDictionary(
            s => s.Name, 
            s => s.Value?.GetValue()
        );
        
        const string instance1 = "Instance1";
        const string instance2 = "Instance2";
        const string instance3 = "Instance3";
        
        var runSessionId1 = Guid.NewGuid();
        var runSessionId2 = Guid.NewGuid();
        var runSessionId3 = Guid.NewGuid();
        
        // Simulate heavy concurrent load: GetSettings and SyncStatus calls from multiple instances
        var tasks = new List<Task>();
        for (int i = 0; i < 15; i++)
        {
            // Each instance gets settings
            tasks.Add(GetSettingsForClient(settings.ClientName, secret, instance1));
            tasks.Add(GetSettingsForClient(settings.ClientName, secret, instance2));
            tasks.Add(GetSettingsForClient(settings.ClientName, secret, instance3));
            
            // Each instance syncs status
            var status1 = CreateStatusRequest(DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow.AddSeconds(-2), 5000, true, runSessionId: runSessionId1);
            var status2 = CreateStatusRequest(DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow.AddSeconds(-2), 5000, true, runSessionId: runSessionId2);
            var status3 = CreateStatusRequest(DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow.AddSeconds(-2), 5000, true, runSessionId: runSessionId3);
            
            tasks.Add(GetStatus(settings.ClientName, secret, status1, instance1));
            tasks.Add(GetStatus(settings.ClientName, secret, status2, instance2));
            tasks.Add(GetStatus(settings.ClientName, secret, status3, instance3));
        }
        
        await Task.WhenAll(tasks);
        
        await Task.Delay(1000);
        
        // Verify settings are not corrupted
        var clientAfter = await GetClient(settings.ClientName);
        Assert.That(clientAfter.Settings.Count, Is.EqualTo(3), "Settings count should remain the same");
        
        foreach (var setting in clientAfter.Settings)
        {
            var originalValue = originalValues[setting.Name];
            var currentValue = setting.Value?.GetValue();
            
            Assert.That(currentValue, Is.EqualTo(originalValue), 
                $"Setting '{setting.Name}' should not be corrupted. Expected: {originalValue}, Got: {currentValue}");
        }
    }
}
