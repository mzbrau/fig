using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Fig.Contracts.Authentication;
using Fig.Contracts.Settings;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

[TestFixture]
public class SchedulingTests : IntegrationTestBase
{
    [Test]
    public async Task ShallScheduleChangesForLaterExecution()
    {
        // Arrange
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        const string newValue = "New scheduled value";
        var executeAt = DateTime.UtcNow.AddSeconds(5);
        
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract(newValue))
        };

        // Act - Schedule a change for future execution
        var result = await SetSettings(settings.ClientName, settingsToUpdate, applyAt: executeAt);
        
        // Assert - Verify the change was scheduled
        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        // Get all scheduled changes
        var scheduledChanges = await GetScheduledChanges();
        Assert.That(scheduledChanges.Changes.Count(), Is.EqualTo(1));
        
        var scheduledChange = scheduledChanges.Changes.First();
        Assert.That(scheduledChange.ClientName, Is.EqualTo(settings.ClientName));
        Assert.That(scheduledChange.ExecuteAtUtc, Is.EqualTo(executeAt).Within(1).Seconds);
        Assert.That(scheduledChange.Instance, Is.Null.Or.Empty);
    }

    [Test]
    public async Task ShallAutomaticallyApplyScheduledChanges()
    {
        await SetConfiguration(CreateConfiguration(pollIntervalOverrideMs: 1000));
        var secret = GetNewSecret();
        var (settings, _) = InitializeConfigurationProvider<ThreeSettings>(secret);
        const string newValue = "New value from scheduled change";
        var applyAt = DateTime.UtcNow.AddSeconds(2);
        
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.CurrentValue.AStringSetting), new StringSettingDataContract(newValue))
        };

        // Act - Schedule a change for near-future execution
        await SetSettings(settings.CurrentValue.ClientName, settingsToUpdate, applyAt: applyAt);

        var initialSettings = await GetSettingsForClient(settings.CurrentValue.ClientName, secret);
        
        Assert.That(initialSettings.First(a => a.Name == nameof(settings.CurrentValue.AStringSetting))
            .Value?.GetValue(), Is.Not.EqualTo(newValue));
        
        // Wait for the scheduled change to be applied
        await WaitForCondition(
            () => Task.FromResult(settings.CurrentValue.AStringSetting == newValue),
            TimeSpan.FromSeconds(8)
        );
        
        Assert.That(settings.CurrentValue.AStringSetting, Is.EqualTo(newValue), "Setting should be updated after scheduled time");
        
        await WaitForCondition(async () =>
        {
            var currentSettings = await GetScheduledChanges();
            return !currentSettings.Changes.Any();
        }, TimeSpan.FromSeconds(8));
        
        // Check that the scheduled change is no longer in the list
        var scheduledChanges = await GetScheduledChanges();
        Assert.That(scheduledChanges.Changes.Count(), Is.EqualTo(0), "Scheduled change should be removed after execution");
    }

    [Test]
    public async Task ShallGetAllScheduledChanges()
    {
        // Arrange
        var secret = GetNewSecret();
        var settings1 = await RegisterSettings<ThreeSettings>(secret);
        var settings2 = await RegisterSettings<ClientA>(secret);
        
        var applyAt1 = DateTime.UtcNow.AddMinutes(10);
        var applyAt2 = DateTime.UtcNow.AddMinutes(20);
        
        // Schedule changes for both clients
        await SetSettings(settings1.ClientName, new List<SettingDataContract>
        {
            new(nameof(settings1.AStringSetting), new StringSettingDataContract("New value 1"))
        }, applyAt: applyAt1);
        
        await SetSettings(settings2.ClientName, new List<SettingDataContract>
        {
            new(nameof(settings2.WebsiteAddress), new StringSettingDataContract("New value 2"))
        }, applyAt: applyAt2);

        // Act
        var scheduledChanges = await GetScheduledChanges();

        // Assert
        Assert.That(scheduledChanges.Changes.Count(), Is.EqualTo(2));
        
        var change1 = scheduledChanges.Changes.First(c => c.ClientName == settings1.ClientName);
        var change2 = scheduledChanges.Changes.First(c => c.ClientName == settings2.ClientName);
        
        Assert.That(change1.ExecuteAtUtc, Is.EqualTo(applyAt1).Within(1).Seconds);
        Assert.That(change2.ExecuteAtUtc, Is.EqualTo(applyAt2).Within(1).Seconds);
    }

    [Test]
    public async Task ShallRescheduleChanges()
    {
        // Arrange
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        var applyAt = DateTime.UtcNow.AddMinutes(10);
        
        // Schedule a change
        await SetSettings(settings.ClientName, new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract("New value"))
        }, applyAt: applyAt);
        
        // Get the scheduled change
        var scheduledChanges = await GetScheduledChanges();
        var change = scheduledChanges.Changes.First();
        
        // Act - Reschedule the change
        var newExecuteAt = DateTime.UtcNow.AddMinutes(15);
        var rescheduleResult = await RescheduleChange(change.Id, newExecuteAt);
        
        // Assert
        Assert.That(rescheduleResult, Is.Null);
        
        // Get updated scheduled changes
        var updatedChanges = await GetScheduledChanges();
        var updatedChange = updatedChanges.Changes.First();
        
        Assert.That(updatedChange.ExecuteAtUtc, Is.EqualTo(newExecuteAt).Within(1).Seconds);
    }

    [Test]
    public async Task ShallDeleteScheduledChanges()
    {
        // Arrange
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        var applyAt = DateTime.UtcNow.AddMinutes(10);
        
        // Schedule a change
        await SetSettings(settings.ClientName, new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract("New value"))
        }, applyAt: applyAt);
        
        // Get the scheduled change
        var scheduledChanges = await GetScheduledChanges();
        var change = scheduledChanges.Changes.First();
        
        // Act - Delete the change
        await DeleteScheduledChange(change.Id, true);

        // Verify change was deleted
        var updatedChanges = await GetScheduledChanges();
        Assert.That(updatedChanges.Changes.Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task ShallReturnUnauthorizedWhenNonAdminTriesToRescheduleChanges()
    {
        // Arrange
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        var applyAt = DateTime.UtcNow.AddMinutes(10);
        
        // Schedule a change
        await SetSettings(settings.ClientName, new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract("New value"))
        }, applyAt: applyAt);
        
        // Get the scheduled change
        var scheduledChanges = await GetScheduledChanges();
        var change = scheduledChanges.Changes.First();
        
        // Create a non-admin user
        var user = NewUser(role: Role.User);
        await CreateUser(user);
        var loginResult = await Login(user.Username, user.Password!);
        
        // Act - Attempt to reschedule with non-admin user
        var newExecuteAt = DateTime.UtcNow.AddMinutes(15);
        var rescheduleResult = await RescheduleChange(change.Id, newExecuteAt, false, loginResult.Token);
        
        // Assert
        Assert.That(rescheduleResult?.ErrorType, Is.EqualTo(StatusCodes.Status401Unauthorized.ToString()));
    }

    [Test]
    public async Task ShallReturnUnauthorizedWhenNonAdminTriesToDeleteChanges()
    {
        // Arrange
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        var applyAt = DateTime.UtcNow.AddMinutes(10);
        
        // Schedule a change
        await SetSettings(settings.ClientName, new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract("New value"))
        }, applyAt: applyAt);
        
        // Get the scheduled change
        var scheduledChanges = await GetScheduledChanges();
        var change = scheduledChanges.Changes.First();
        
        // Create a non-admin user
        var user = NewUser(role: Role.User);
        await CreateUser(user);
        var loginResult = await Login(user.Username, user.Password!);
        
        // Act - Attempt to delete with non-admin user
        var deleteResult = await DeleteScheduledChange(change.Id, false, tokenOverride: loginResult.Token);
        
        // Assert
        Assert.That(deleteResult?.ErrorType, Is.EqualTo(StatusCodes.Status401Unauthorized.ToString()));
    }

    [Test]
    public async Task ShallScheduleChangesForSpecificInstance()
    {
        // Arrange
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        const string newValue = "New value for specific instance";
        var applyAt = DateTime.UtcNow.AddSeconds(5);
        const string instanceName = "Instance1";
        
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract(newValue))
        };

        // Act - Schedule a change for a specific instance
        var result = await SetSettings(settings.ClientName, settingsToUpdate, instanceName, applyAt: applyAt);
        
        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        // Get scheduled changes and check instance
        var scheduledChanges = await GetScheduledChanges();
        var scheduledChange = scheduledChanges.Changes.First();
        
        Assert.That(scheduledChange.ClientName, Is.EqualTo(settings.ClientName));
        Assert.That(scheduledChange.Instance, Is.EqualTo(instanceName));
        Assert.That(scheduledChange.ExecuteAtUtc, Is.EqualTo(applyAt).Within(1).Seconds);
    }

    [Test]
    public async Task ShallApplyScheduledChangeToSpecificInstanceOnly()
    {
        await SetConfiguration(CreateConfiguration(pollIntervalOverrideMs: 1000));
        var secret = GetNewSecret();
        var (settings, _) = InitializeConfigurationProvider<ThreeSettings>(secret);
        const string newValue = "New value for specific instance";
        var applyAt = DateTime.UtcNow.AddSeconds(2);
        const string instanceName = "Instance1";
        
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.CurrentValue.AStringSetting), new StringSettingDataContract(newValue))
        };

        // Create settings for the specific instance first
        await SetSettings(settings.CurrentValue.ClientName, settingsToUpdate, instanceName);

        // Schedule a change for the specific instance
        await SetSettings(settings.CurrentValue.ClientName, new List<SettingDataContract>
        {
            new(nameof(settings.CurrentValue.AStringSetting), new StringSettingDataContract("Updated value"))
        }, instanceName, applyAt: applyAt);

        // Wait for the scheduled change to be applied
        await WaitForCondition(async () => 
        {
            var instanceSettings = await GetSettingsForClient(settings.CurrentValue.ClientName, secret, instanceName);
            return instanceSettings.First(a => a.Name == nameof(settings.CurrentValue.AStringSetting)).Value?.GetValue() as string == "Updated value";
        }, TimeSpan.FromSeconds(8));

        // Get settings for both default and specific instance
        var defaultSettings = await GetSettingsForClient(settings.CurrentValue.ClientName, secret);
        var instanceSettings = await GetSettingsForClient(settings.CurrentValue.ClientName, secret, instanceName);
        
        // Assert - Default settings should remain unchanged
        Assert.That(defaultSettings.First(a => a.Name == nameof(settings.CurrentValue.AStringSetting)).Value?.GetValue(),
            Is.EqualTo("Horse"));
        
        // Instance settings should be updated
        Assert.That(instanceSettings.First(a => a.Name == nameof(settings.CurrentValue.AStringSetting)).Value?.GetValue(),
            Is.EqualTo("Updated value"));
    }

    [Test]
    public async Task ShallScheduleChangesWithRevert()
    {
        // Arrange
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        const string newValue = "Temporary scheduled value";
        var originalValue = settings.AStringSetting;
        var revertAt = DateTime.UtcNow.AddSeconds(1);
        
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract(newValue))
        };

        // Act - Schedule a change with revert
        var result = await SetSettings(settings.ClientName, settingsToUpdate, revertAt: revertAt);
        
        // Assert - Verify the revert is scheduled
        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        // Get all scheduled changes
        var scheduledChanges = await GetScheduledChanges();
        Assert.That(scheduledChanges.Changes.Count(), Is.EqualTo(1));
        
        var revertChange = scheduledChanges.Changes.First(c => c.ExecuteAtUtc == revertAt);
        
        Assert.That(revertChange.ClientName, Is.EqualTo(settings.ClientName));
        
        await WaitForCondition(
            async () => await GetCurrentSettingValue() == newValue,
            TimeSpan.FromSeconds(10),
            () => "New value should be applied"
        );
        
        await WaitForCondition(
            async () => await GetCurrentSettingValue() == originalValue,
            TimeSpan.FromSeconds(10),
            () => "Original value should be reverted"
        );

        var value = await GetCurrentSettingValue();
        Assert.That(value, Is.EqualTo(originalValue));
        
        async Task<string?> GetCurrentSettingValue()
        {
            var currentSettings = await GetSettingsForClient(settings.ClientName, secret);
            return currentSettings.First(a => a.Name == nameof(settings.AStringSetting)).Value!.GetValue()?.ToString();
        }
    }
    
    [Test]
    public async Task ShallScheduleChangesWithRevertAndDelayedApply()
    {
        // Arrange
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        const string newValue = "Temporary scheduled value";
        var originalValue = settings.AStringSetting;
        var applyAt = DateTime.UtcNow.AddSeconds(1);
        var revertAt = DateTime.UtcNow.AddSeconds(3);
        
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract(newValue))
        };

        // Act - Schedule a change with revert
        var result = await SetSettings(settings.ClientName, settingsToUpdate, applyAt: applyAt, revertAt: revertAt);
        
        // Assert - Verify the change and revert were scheduled
        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var scheduledChanges = await GetScheduledChanges();
        Assert.That(scheduledChanges.Changes.Count(), Is.EqualTo(1));
        
        var applyChange = scheduledChanges.Changes.First(c => c.ExecuteAtUtc == applyAt);
        
        Assert.That(applyChange.ClientName, Is.EqualTo(settings.ClientName));
        
        var value = await GetCurrentSettingValue();
        Assert.That(value, Is.EqualTo(originalValue));
        
        await WaitForCondition(
            async () => await GetCurrentSettingValue() == newValue,
            TimeSpan.FromSeconds(10), 
            () =>
            {
                var val = GetCurrentSettingValue().GetAwaiter().GetResult();
                return $"New value ({newValue}) should have been applied but had {val} instead";
            });
        
        var value2 = await GetCurrentSettingValue();
        Assert.That(value2, Is.EqualTo(newValue));
        
        await WaitForCondition(
            async () => await GetCurrentSettingValue() == originalValue,
            TimeSpan.FromSeconds(10),
            () =>
            {
                var val = GetCurrentSettingValue().GetAwaiter().GetResult();
                return $"Original value ({originalValue}) should have been applied but had {val} instead";
            }
        );

        var value3 = await GetCurrentSettingValue();
        Assert.That(value3, Is.EqualTo(originalValue));
        
        async Task<string?> GetCurrentSettingValue()
        {
            var currentSettings = await GetSettingsForClient(settings.ClientName, secret);
            return currentSettings.First(a => a.Name == nameof(settings.AStringSetting)).Value!.GetValue()?.ToString();
        }
    }

    [Test]
    public async Task ShallRevertMultipleSettings()
    {
        await SetConfiguration(CreateConfiguration(pollIntervalOverrideMs: 1000));
        var secret = GetNewSecret();
        var (settings, _) = InitializeConfigurationProvider<ThreeSettings>(secret);
        const string initialStringValue = "Horse"; // Default value
        const int initialIntValue = 6; // Default value
        
        const string tempStringValue = "Temporary string value";
        const int tempIntValue = 999;
        
        // Verify initial values
        Assert.That(settings.CurrentValue.AStringSetting, Is.EqualTo(initialStringValue));
        Assert.That(settings.CurrentValue.AnIntSetting, Is.EqualTo(initialIntValue));
        
        var applyAt = DateTime.UtcNow.AddSeconds(2);
        var revertAt = DateTime.UtcNow.AddSeconds(4);
        
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.CurrentValue.AStringSetting), new StringSettingDataContract(tempStringValue)),
            new(nameof(settings.CurrentValue.AnIntSetting), new IntSettingDataContract(tempIntValue))
        };

        // Act - Schedule a change with revert for multiple settings
        await SetSettings(settings.CurrentValue.ClientName, settingsToUpdate, applyAt: applyAt, revertAt: revertAt);

        // Wait for the scheduled changes to be applied
        await WaitForCondition(
            () => Task.FromResult(
                settings.CurrentValue is { AStringSetting: tempStringValue, AnIntSetting: tempIntValue }),
            TimeSpan.FromSeconds(10),
            () => $"Scheduled Changes should be applied. AStringSetting:{settings.CurrentValue.AStringSetting}, AnIntSetting:{settings.CurrentValue.AnIntSetting}"
        );

        // Assert - Verify both changes were applied
        Assert.That(settings.CurrentValue.AStringSetting, Is.EqualTo(tempStringValue));
        Assert.That(settings.CurrentValue.AnIntSetting, Is.EqualTo(tempIntValue));
        
        // Wait for the revert to be applied
        await WaitForCondition(
            () => Task.FromResult(
                settings.CurrentValue is { AStringSetting: initialStringValue, AnIntSetting: initialIntValue }),
            TimeSpan.FromSeconds(10),
            () => "Settings should be reverted"
        );
        
        // Assert - Verify both settings were reverted
        Assert.That(settings.CurrentValue.AStringSetting, Is.EqualTo(initialStringValue));
        Assert.That(settings.CurrentValue.AnIntSetting, Is.EqualTo(initialIntValue));
    }

    [Test]
    public async Task ShallRevertInstanceSpecificChangesOnly()
    {
        const string instanceName = "RevertTestInstance";
        await SetConfiguration(CreateConfiguration(pollIntervalOverrideMs: 1000));
        var secret = GetNewSecret();
        var (settings, _) = InitializeConfigurationProvider<ThreeSettings>(secret, instanceName);
        const string initialValue = "Horse"; // Default value
        const string tempValue = "Temporary instance value";

        // Create settings for the specific instance first
        await SetSettings(settings.CurrentValue.ClientName, new List<SettingDataContract>
        {
            new(nameof(settings.CurrentValue.AStringSetting), new StringSettingDataContract(initialValue))
        }, instanceName);
        
        var applyAt = DateTime.UtcNow.AddSeconds(2);
        var revertAt = DateTime.UtcNow.AddSeconds(6);
        
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.CurrentValue.AStringSetting), new StringSettingDataContract(tempValue))
        };

        // Act - Schedule a change with revert for the specific instance
        await SetSettings(settings.CurrentValue.ClientName, settingsToUpdate, instanceName, applyAt: applyAt, revertAt: revertAt);

        // Wait for the scheduled change to be applied
        await WaitForCondition(() => Task.FromResult(settings.CurrentValue.AStringSetting == tempValue), TimeSpan.FromSeconds(8));

        // Verify the instance settings were updated but default settings were not
        var defaultSettings = await GetSettingsForClient(settings.CurrentValue.ClientName, secret);
        var instanceSettings = await GetSettingsForClient(settings.CurrentValue.ClientName, secret, instanceName);
        
        Assert.That(defaultSettings.First(a => a.Name == nameof(settings.CurrentValue.AStringSetting)).Value?.GetValue(),
            Is.EqualTo(initialValue), "Default settings should remain unchanged");
        Assert.That(instanceSettings.First(a => a.Name == nameof(settings.CurrentValue.AStringSetting)).Value?.GetValue(),
            Is.EqualTo(tempValue), "Instance settings should be updated");
        
        // Wait for the revert to be applied
        await WaitForCondition(() => Task.FromResult(settings.CurrentValue.AStringSetting == initialValue), TimeSpan.FromSeconds(8));
        
        Assert.That(settings.CurrentValue.AStringSetting, Is.EqualTo(initialValue));
    }

    [Test]
    public async Task ShallCancelRevertWhenScheduledChangeIsDeleted()
    {
        // Arrange
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        const string newValue = "Value that won't be reverted";
        var revertAt = DateTime.UtcNow.AddMinutes(10);
        
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract(newValue))
        };

        // Schedule a change with revert
        await SetSettings(settings.ClientName, settingsToUpdate, revertAt: revertAt);
        
        // Get all scheduled changes - should have two entries (apply and revert)
        var scheduledChanges = await GetScheduledChanges();
        Assert.That(scheduledChanges.Changes.Count(), Is.EqualTo(1));
        
        // Act - Delete the apply change
        await DeleteScheduledChange(scheduledChanges.Changes.Single().Id, true);
        
        scheduledChanges = await GetScheduledChanges();
        Assert.That(scheduledChanges.Changes.Count(), Is.EqualTo(0));
    }
}