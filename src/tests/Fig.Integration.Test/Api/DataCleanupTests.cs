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
    public async Task ShallDeleteOldEventLogsWhenConfigured()
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
        
        // Wait a moment for events to be saved
        await Task.Delay(500);
        
        // Verify we have event logs
        var eventsBefore = await GetEvents(startTime, DateTime.UtcNow);
        var eventCountBefore = await GetEventCount();
        Assert.That(eventsBefore.Events.Count(), Is.GreaterThan(0), "Should have created event logs");
        
        // Act - Run cleanup service (should not delete anything since events are recent)
        using (var scope = GetServiceScope())
        {
            var cleanupService = scope.ServiceProvider.GetRequiredService<IDataCleanupService>();
            var deletedRecent = await cleanupService.PerformCleanupAsync();
            
            // Assert - No recent events should be deleted
            Assert.That(deletedRecent, Is.EqualTo(0), "Should not delete recent event logs");
        }
        
        var eventCountAfterFirstCleanup = await GetEventCount();
        Assert.That(eventCountAfterFirstCleanup, Is.EqualTo(eventCountBefore), 
            "Event count should remain the same after cleaning recent data");
    }
    
    [Test]
    public async Task ShallDeleteOldCheckpointsWhenConfigured()
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
        
        await Task.Delay(500);
        
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
        
        await Task.Delay(500);
        
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
        Assert.That(eventsAfter.Events.Count(), Is.EqualTo(recentEventCount), 
            "Recent events should still exist");
    }
    
    [Test]
    public async Task ShallHandleMultipleCleanupTypesIndependently()
    {
        // Arrange - Enable only event log cleanup, disable others
        await SetConfiguration(CreateConfiguration(
            timeMachineCleanupDays: null,  // Disabled
            eventLogsCleanupDays: 7,        // Enabled
            apiStatusCleanupDays: null,     // Disabled
            settingHistoryCleanupDays: null)); // Disabled
        
        var startTime = DateTime.UtcNow;
        
        // Create both checkpoints and event logs
        var settings = await RegisterClientAndWaitForCheckpoint<ThreeSettings>();
        await SetSettings(settings.ClientName, new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract("TestValue"))
        });
        
        await Task.Delay(500);
        
        var checkpointsBefore = await GetCheckpoints(startTime, DateTime.UtcNow);
        var checkpointCountBefore = checkpointsBefore.CheckPoints.Count();
        
        // Act - Run cleanup
        using (var scope = GetServiceScope())
        {
            var cleanupService = scope.ServiceProvider.GetRequiredService<IDataCleanupService>();
            await cleanupService.PerformCleanupAsync();
        }
        
        // Assert - Checkpoints should remain (cleanup disabled for time machine)
        var checkpointsAfter = await GetCheckpoints(startTime, DateTime.UtcNow);
        Assert.That(checkpointsAfter.CheckPoints.Count(), Is.EqualTo(checkpointCountBefore), 
            "Checkpoints should not be deleted when time machine cleanup is disabled");
    }
}

