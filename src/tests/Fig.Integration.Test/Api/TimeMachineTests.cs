using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fig.Contracts.Settings;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

public class TimeMachineTests : IntegrationTestBase
{
    [Test]
    public async Task ShallCreateCheckpointOnClientRegistration()
    {
        var startTime = DateTime.UtcNow;
        var settings = await RegisterSettings<SettingsWithVerification>();

        await ValidateCheckpointWasCreated(startTime, settings.ClientName, "Initial Registration");
    }
    
    [Test]
    public async Task ShallNotCheckpointOnDuplicateClientRegistration()
    {
        var secret = GetNewSecret();
        var startTime = DateTime.UtcNow;
        var settings = await RegisterSettings<SettingsWithVerification>(secret);
        await RegisterSettings<SettingsWithVerification>(secret);
        await RegisterSettings<SettingsWithVerification>(secret);

        await ValidateCheckpointWasCreated(startTime, settings.ClientName, "Initial Registration");
    }
    
    [Test]
    public async Task ShallCreateCheckpointForUpdatedRegistrations()
    {
        var secret = GetNewSecret();
        await RegisterClientAndWaitForCheckpoint<ClientXWithTwoSettings>(secret);
        
        var startTime = DateTime.UtcNow;
        var settings = await RegisterSettings<ClientXWithThreeSettings>(secret);

        await ValidateCheckpointWasCreated(startTime, settings.ClientName, "Updated Registration");
    }
    
    [Test]
    public async Task ShallCreateCheckpointOnSettingsUpdate()
    {
        var settings = await RegisterClientAndWaitForCheckpoint<ThreeSettings>();
        
        const string newValue = "Some new value";
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract(newValue))
        };

        var startTime = DateTime.UtcNow;
        await SetSettings(settings.ClientName, settingsToUpdate);

        await ValidateCheckpointWasCreated(startTime, settings.ClientName, "Settings updated");
    }
    
    [Test]
    public async Task ShallCreateCheckpointOnClientDeleted()
    {
        var settings = await RegisterClientAndWaitForCheckpoint<ThreeSettings>();
        
        var startTime = DateTime.UtcNow;
        await DeleteClient(settings.ClientName);

        await ValidateCheckpointWasCreated(startTime, settings.ClientName, "deleted");
    }
    
    [Test]
    public async Task ShallCreateCheckpointOnClientSecretChanged()
    {
        var settings = await RegisterClientAndWaitForCheckpoint<ThreeSettings>();

        var startTime = DateTime.UtcNow;
        await ChangeClientSecret(settings.ClientName, Guid.NewGuid().ToString(), DateTime.UtcNow);

        await ValidateCheckpointWasCreated(startTime, settings.ClientName, "Secret changed");
    }

    private async Task ValidateCheckpointWasCreated(DateTime startTime,
        params string[] messageContains)
    {
        await WaitForCondition(async () => (await GetCheckpoints(startTime, DateTime.UtcNow)).CheckPoints.Count() == 1,
            TimeSpan.FromSeconds(5));

        var checkpointCollection = await GetCheckpoints(startTime, DateTime.UtcNow);

        Assert.Multiple(() =>
        {
            Assert.That(checkpointCollection.CheckPoints.Count(), Is.EqualTo(1));
            var message = checkpointCollection.CheckPoints.Single().AfterEvent;
            foreach (var expectation in messageContains)
            {
                Assert.That(message, Does.Contain(expectation));
            }
        });
    }

    private async Task<T> RegisterClientAndWaitForCheckpoint<T>(string? secret = null) where T : TestSettingsBase
    {
        var theSecret = secret ?? GetNewSecret();
        var setupStartTime = DateTime.UtcNow;
        var settings = await RegisterSettings<T>(theSecret);
        
        await WaitForCondition(async () => (await GetCheckpoints(setupStartTime, DateTime.UtcNow)).CheckPoints.Count() == 1,
            TimeSpan.FromSeconds(10));

        return settings;
    }
    
    // TODO: Tests that only admin can get these
}