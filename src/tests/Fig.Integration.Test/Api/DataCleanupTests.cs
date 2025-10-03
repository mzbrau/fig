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
}

