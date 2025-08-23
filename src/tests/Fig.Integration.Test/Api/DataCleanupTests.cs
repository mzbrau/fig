using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fig.Api.Services;
using Fig.Contracts.Settings;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

[TestFixture]
public class DataCleanupTests : IntegrationTestBase
{
    [Test]
    public async Task ShallAcceptDataCleanupConfigurationWithAllOptionsEnabled()
    {
        // Arrange & Act
        var configuration = CreateConfiguration(
            timeMachineCleanupDays: 90,
            eventLogsCleanupDays: 60,
            apiStatusCleanupDays: 30,
            settingHistoryCleanupDays: 120);
        
        var result = await SetConfiguration(configuration);
        
        // Assert
        Assert.That(result.IsSuccessStatusCode, Is.True, 
            "Should accept data cleanup configuration with all options enabled");
    }

    [Test]
    public async Task ShallAcceptDataCleanupConfigurationWithSomeOptionsDisabled()
    {
        // Arrange & Act
        var configuration = CreateConfiguration(
            timeMachineCleanupDays: 90,
            eventLogsCleanupDays: null,  // Disabled
            apiStatusCleanupDays: 30,
            settingHistoryCleanupDays: null);  // Disabled
        
        var result = await SetConfiguration(configuration);
        
        // Assert
        Assert.That(result.IsSuccessStatusCode, Is.True, 
            "Should accept data cleanup configuration with some options disabled");
    }

    [Test]
    public async Task ShallAcceptDataCleanupConfigurationWhenAllDisabled()
    {
        // Arrange & Act - all cleanup types disabled (null values)
        var configuration = CreateConfiguration(
            timeMachineCleanupDays: null,
            eventLogsCleanupDays: null,
            apiStatusCleanupDays: null,
            settingHistoryCleanupDays: null);
        
        var result = await SetConfiguration(configuration);
        
        // Assert
        Assert.That(result.IsSuccessStatusCode, Is.True, 
            "Should accept data cleanup configuration when all cleanup types are disabled");
    }

    [Test]
    public async Task ShallAcceptDefaultDataCleanupConfiguration()
    {
        // Arrange & Act - use defaults (time machine and api status enabled at 90 days)
        var configuration = CreateConfiguration();
        
        var result = await SetConfiguration(configuration);
        
        // Assert
        Assert.That(result.IsSuccessStatusCode, Is.True, 
            "Should accept default data cleanup configuration");
    }

    [Test]
    public async Task ShallAllowDataCleanupConfigurationUpdates()
    {
        // Arrange - first set with cleanup disabled
        await SetConfiguration(CreateConfiguration(
            timeMachineCleanupDays: null,
            eventLogsCleanupDays: null,
            apiStatusCleanupDays: null,
            settingHistoryCleanupDays: null));
        
        // Act - update to enable cleanup
        var updatedConfig = CreateConfiguration(
            timeMachineCleanupDays: 60,
            eventLogsCleanupDays: 30);
        
        var result = await SetConfiguration(updatedConfig);
        
        // Assert
        Assert.That(result.IsSuccessStatusCode, Is.True, 
            "Should allow updating data cleanup configuration");
    }
    
    [Test]
    public async Task ShallNotDeleteRecentEventLogsWhenConfigured()
    {
        // Arrange - Configure cleanup for event logs older than 7 days
        await SetConfiguration(CreateConfiguration(eventLogsCleanupDays: 7));
        
        var startTime = DateTime.UtcNow;
        
        // Create some test data by making setting changes
        var settings = await RegisterSettings<ThreeSettings>();
        
        // Make several setting changes to generate event logs
        for (int i = 0; i < 5; i++)
        {
            await SetSettings(settings.ClientName, new List<SettingDataContract>
            {
                new(nameof(settings.AStringSetting), new StringSettingDataContract($"Value{i}"))
            });
        }
        
        // Wait for events to be created and stabilize (at least 5 events)
        int lastCount = 0;
        await WaitForCondition(
            async () => {
                lastCount = (await GetEvents(startTime, DateTime.UtcNow)).Events.Count();
                return lastCount >= 5;
            },
            TimeSpan.FromSeconds(5),
            () => $"Expected at least 5 events but was actually {lastCount}");
        
        // Get the count before cleanup
        var eventCountBefore = await GetEventCount();
        Assert.That(eventCountBefore, Is.GreaterThanOrEqualTo(5), "Should have created at least 5 event logs");
        
        // Act - Run cleanup service (should not delete anything since events are recent)
        int deletedRecent;
        using (var scope = GetServiceScope())
        {
            var cleanupService = scope.ServiceProvider.GetRequiredService<IDataCleanupService>();
            deletedRecent = await cleanupService.PerformCleanupAsync();
        }
        
        // Assert - No recent events should be deleted
        Assert.That(deletedRecent, Is.EqualTo(0), "Should not delete recent event logs");
        
        var eventCountAfterFirstCleanup = await GetEventCount();
        Assert.That(eventCountAfterFirstCleanup, Is.EqualTo(eventCountBefore), 
            "Event count should remain the same after cleaning recent data");
    }
    
    [Test]
    public async Task ShallNotDeleteRecentCheckpointsWhenConfigured()
    {
        // Arrange - Configure cleanup for checkpoints older than 7 days  
        await SetConfiguration(CreateConfiguration(timeMachineCleanupDays: 7));
        
        var startTime = DateTime.UtcNow;
        
        // Register a client and wait for checkpoint to be created
        await RegisterClientAndWaitForCheckpoint<ThreeSettings>();
        
        // Verify we have checkpoints
        var checkpointsBefore = await GetCheckpoints(startTime, DateTime.UtcNow);
        Assert.That(checkpointsBefore.CheckPoints.Count(), Is.GreaterThan(0), 
            "Should have created checkpoints");
        
        // Act - Run cleanup service (should not delete anything since checkpoints are recent)
        using (var scope = GetServiceScope())
        {
            var cleanupService = scope.ServiceProvider.GetRequiredService<IDataCleanupService>();
            var deleted = await cleanupService.PerformCleanupAsync();
            
            // Assert - No recent checkpoints should be deleted
            Assert.That(deleted, Is.EqualTo(0), "Should not delete recent checkpoints");
        }
        
        var checkpointsAfter = await GetCheckpoints(startTime, DateTime.UtcNow);
        Assert.That(checkpointsAfter.CheckPoints.Count(), Is.EqualTo(checkpointsBefore.CheckPoints.Count()), 
            "Checkpoint count should remain the same");
    }
    
    [Test]
    public async Task ShallDeleteOldCheckpointsWhenConfigured()
    {
        // Arrange - Configure cleanup for checkpoints older than 7 days  
        await SetConfiguration(CreateConfiguration(timeMachineCleanupDays: 7));
        
        var startTime = DateTime.UtcNow;
        
        // Register a client and wait for checkpoint to be created
        await RegisterClientAndWaitForCheckpoint<ThreeSettings>();
        
        // Verify we have the recent checkpoint
        var checkpointsBefore = await GetCheckpoints(startTime, DateTime.UtcNow);
        Assert.That(checkpointsBefore.CheckPoints.Count(), Is.EqualTo(1), 
            "Should have created one checkpoint");
        
        var recentCheckpoint = checkpointsBefore.CheckPoints.First();
        
        // Backdate the checkpoint to make it older than the retention period (8 days old)
        var oldTimestamp = DateTime.UtcNow.AddDays(-8);
        await BackdateCheckpoint(recentCheckpoint.Id, oldTimestamp);
        
        // Verify the checkpoint now appears in the old date range
        var oldCheckpoints = await GetCheckpoints(oldTimestamp.AddMinutes(-1), oldTimestamp.AddMinutes(1));
        Assert.That(oldCheckpoints.CheckPoints.Count(), Is.EqualTo(1), 
            "Should have one backdated checkpoint");
        
        // Act - Run cleanup service (should delete the old checkpoint)
        int deletedCount;
        using (var scope = GetServiceScope())
        {
            var cleanupService = scope.ServiceProvider.GetRequiredService<IDataCleanupService>();
            deletedCount = await cleanupService.PerformCleanupAsync();
        }
        
        // Assert - The old checkpoint should be deleted
        Assert.That(deletedCount, Is.GreaterThan(0), "Should have deleted old checkpoint");
        
        var checkpointsAfter = await GetCheckpoints(oldTimestamp.AddMinutes(-1), oldTimestamp.AddMinutes(1));
        Assert.That(checkpointsAfter.CheckPoints.Count(), Is.EqualTo(0), 
            "Old checkpoint should no longer exist");
    }
    
    [Test]
    public async Task ShallNotDeleteDataWhenCleanupDisabled()
    {
        // Arrange - Disable all cleanup
        await SetConfiguration(CreateConfiguration(
            timeMachineCleanupDays: null,
            eventLogsCleanupDays: null,
            apiStatusCleanupDays: null,
            settingHistoryCleanupDays: null));
        
        var startTime = DateTime.UtcNow;
        
        // Create test data
        var settings = await RegisterSettings<ThreeSettings>();
        await SetSettings(settings.ClientName, new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract("TestValue"))
        });
        
        // Wait for events to be created
        await WaitForCondition(
            async () => (await GetEvents(startTime, DateTime.UtcNow)).Events.Any(),
            TimeSpan.FromSeconds(5));
        
        var eventsBefore = await GetEvents(startTime, DateTime.UtcNow);
        var eventCountBefore = eventsBefore.Events.Count();
        
        // Act - Run cleanup service
        using (var scope = GetServiceScope())
        {
            var cleanupService = scope.ServiceProvider.GetRequiredService<IDataCleanupService>();
            var deleted = await cleanupService.PerformCleanupAsync();
            
            // Assert - Nothing should be deleted
            Assert.That(deleted, Is.EqualTo(0), "Should not delete any data when cleanup is disabled");
        }
        
        var eventsAfter = await GetEvents(startTime, DateTime.UtcNow);
        Assert.That(eventsAfter.Events.Count(), Is.EqualTo(eventCountBefore), 
            "Event count should remain unchanged");
    }
    
    [Test]
    public async Task ShallReturnZeroWhenNoOldDataExists()
    {
        // Arrange - Configure aggressive cleanup (1 day retention)
        await SetConfiguration(CreateConfiguration(
            timeMachineCleanupDays: 1,
            eventLogsCleanupDays: 1,
            apiStatusCleanupDays: 1,
            settingHistoryCleanupDays: 1));
        
        // Clean database first
        await DeleteAllClients();
        await DeleteAllCheckPointTriggers();
        
        // Act - Run cleanup on empty/recent data
        using (var scope = GetServiceScope())
        {
            var cleanupService = scope.ServiceProvider.GetRequiredService<IDataCleanupService>();
            var deleted = await cleanupService.PerformCleanupAsync();
            
            // Assert
            Assert.That(deleted, Is.EqualTo(0), 
                "Should return 0 when no old data exists to delete");
        }
    }
    
    [Test]
    public async Task ShallOnlyDeleteDataOlderThanConfiguredDays()
    {
        // Arrange - Configure cleanup for 30 days
        await SetConfiguration(CreateConfiguration(eventLogsCleanupDays: 30));
        
        var startTime = DateTime.UtcNow;
        
        // Create recent test data
        var settings = await RegisterSettings<ThreeSettings>();
        await SetSettings(settings.ClientName, new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract("RecentValue"))
        });
        
        // Wait for events to be created
        await WaitForCondition(
            async () => (await GetEvents(startTime, DateTime.UtcNow)).Events.Any(),
            TimeSpan.FromSeconds(5));
        
        var eventsBefore = await GetEvents(startTime, DateTime.UtcNow);
        var recentEventCount = eventsBefore.Events.Count();
        
        // Act - Run cleanup
        using (var scope = GetServiceScope())
        {
            var cleanupService = scope.ServiceProvider.GetRequiredService<IDataCleanupService>();
            var deleted = await cleanupService.PerformCleanupAsync();
            
            // Assert - No recent data (< 30 days) should be deleted
            Assert.That(deleted, Is.EqualTo(0), "Should not delete data newer than 30 days");
        }
        
        var eventsAfter = await GetEvents(startTime, DateTime.UtcNow);
        Assert.That(eventsAfter.Events.Count(), Is.AtLeast(recentEventCount), 
            "Recent events should still exist");
    }
    
    [Test]
    public async Task ShallDeleteOldEventLogs()
    {
        // Arrange - Configure cleanup for event logs older than 7 days
        await SetConfiguration(CreateConfiguration(eventLogsCleanupDays: 7));
        
        var startTime = DateTime.UtcNow;
        
        // Create some test data by making setting changes
        var settings = await RegisterSettings<ThreeSettings>();
        
        // Make several setting changes to generate event logs
        for (int i = 0; i < 3; i++)
        {
            await SetSettings(settings.ClientName, new List<SettingDataContract>
            {
                new(nameof(settings.AStringSetting), new StringSettingDataContract($"Value{i}"))
            });
        }
        
        // Wait for events to be created and stabilize (at least 3 events)
        int lastCount = 0;
        await WaitForCondition(
            async () => {
                lastCount = (await GetEvents(startTime, DateTime.UtcNow)).Events.Count();
                return lastCount >= 3;
            },
            TimeSpan.FromSeconds(5),
            () => $"Expected at least 3 events but was actually {lastCount}");
        
        // Get the initial count
        var eventsBefore = await GetEvents(startTime, DateTime.UtcNow);
        var eventCountBefore = eventsBefore.Events.Count();
        Assert.That(eventCountBefore, Is.GreaterThanOrEqualTo(3), "Should have created at least 3 event logs");
        
        // Backdate the event logs to make them older than the retention period (8 days old)
        var oldTimestamp = DateTime.UtcNow.AddDays(-8);
        await BackdateEventLogs(startTime, DateTime.UtcNow, oldTimestamp);
        
        // Verify the events now appear in the old date range
        var oldEvents = await GetEvents(oldTimestamp.AddMinutes(-1), oldTimestamp.AddMinutes(1));
        Assert.That(oldEvents.Events.Count(), Is.GreaterThanOrEqualTo(eventCountBefore), 
            "Should have backdated event logs");
        
        // Record how many old events we have before cleanup
        var oldEventCountBeforeCleanup = oldEvents.Events.Count();
        
        // Act - Run cleanup service (should delete the old events)
        int deletedCount;
        using (var scope = GetServiceScope())
        {
            var cleanupService = scope.ServiceProvider.GetRequiredService<IDataCleanupService>();
            deletedCount = await cleanupService.PerformCleanupAsync();
        }
        
        // Assert - The old events should be deleted
        Assert.That(deletedCount, Is.GreaterThanOrEqualTo(eventCountBefore), "Should have deleted old event logs");
        
        var eventsAfter = await GetEvents(oldTimestamp.AddMinutes(-1), oldTimestamp.AddMinutes(1));
        Assert.That(eventsAfter.Events.Count(), Is.EqualTo(0), 
            "Old event logs should no longer exist");
    }
    
    [Test]
    public async Task ShallDeleteOldApiStatus()
    {
        // Arrange - Configure cleanup for API status older than 7 days
        await SetConfiguration(CreateConfiguration(apiStatusCleanupDays: 7));
        
        // Get initial count before inserting old records
        var initialStatuses = await GetAllApiStatuses();
        var initialCount = initialStatuses.Count;
        
        // Insert old, inactive API status records directly (8 days old)
        var oldTimestamp = DateTime.UtcNow.AddDays(-8);
        var oldStatusCount = 3;
        await InsertOldApiStatus(oldTimestamp, oldStatusCount);
        
        // Note: GetAllApiStatuses only returns active statuses, so we can't verify
        // the old inactive records were inserted via the API. We trust the insert worked.
        
        // Act - Run cleanup service (should delete the old, inactive API status records)
        int deletedCount;
        using (var scope = GetServiceScope())
        {
            var cleanupService = scope.ServiceProvider.GetRequiredService<IDataCleanupService>();
            deletedCount = await cleanupService.PerformCleanupAsync();
        }
        
        // Assert - The old API status records should be deleted
        Assert.That(deletedCount, Is.GreaterThanOrEqualTo(oldStatusCount), 
            "Should have deleted old API status records");
        
        // Verify count remains the same (only active statuses, old inactive ones were cleaned up)
        var statusesAfter = await GetAllApiStatuses();
        Assert.That(statusesAfter.Count, Is.EqualTo(initialCount), 
            "Active API status count should remain unchanged after cleanup");
    }
    
    [Test]
    public async Task ShallDeleteOldSettingHistory()
    {
        // Arrange - Configure cleanup for setting history older than 7 days
        await SetConfiguration(CreateConfiguration(settingHistoryCleanupDays: 7));
        
        var startTime = DateTime.UtcNow;
        
        // Create some test data by making setting changes
        var settings = await RegisterSettings<ThreeSettings>();
        
        // Make several setting changes to generate history records
        for (int i = 0; i < 5; i++)
        {
            await SetSettings(settings.ClientName, new List<SettingDataContract>
            {
                new(nameof(settings.AStringSetting), new StringSettingDataContract($"HistoryValue{i}"))
            });
        }
        
        // Wait for history to be created and stabilize (at least 5 history records)
        await WaitForCondition(
            async () => (await GetHistory(settings.ClientName, nameof(settings.AStringSetting))).Count() >= 5,
            TimeSpan.FromSeconds(5));
        
        // Verify we have setting history
        var historyBefore = await GetHistory(settings.ClientName, nameof(settings.AStringSetting));
        var historyCountBefore = historyBefore.Count();
        Assert.That(historyCountBefore, Is.GreaterThan(0), "Should have created setting history records");
        
        // Backdate the setting history to make it older than the retention period (8 days old)
        var oldTimestamp = DateTime.UtcNow.AddDays(-8);
        await BackdateSettingHistory(startTime, DateTime.UtcNow, oldTimestamp);
        
        // Act - Run cleanup service (should delete the old setting history)
        int deletedCount;
        using (var scope = GetServiceScope())
        {
            var cleanupService = scope.ServiceProvider.GetRequiredService<IDataCleanupService>();
            deletedCount = await cleanupService.PerformCleanupAsync();
        }
        
        // Assert - The old setting history should be deleted
        Assert.That(deletedCount, Is.GreaterThan(0), "Should have deleted old setting history records");
        
        var historyAfter = await GetHistory(settings.ClientName, nameof(settings.AStringSetting));
        Assert.That(historyAfter.Count(), Is.EqualTo(0), 
            "Old setting history should no longer exist");
    }
}

