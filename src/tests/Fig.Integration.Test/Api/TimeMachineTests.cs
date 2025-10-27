using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Fig.Contracts.CheckPoint;
using Fig.Contracts.Settings;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

public class TimeMachineTests : IntegrationTestBase
{
    [SetUp]
    public override async Task Setup()
    {
        await base.Setup();
        await EnableTimeMachine();
    }

    [Test]
    public async Task ShallCreateCheckpointOnClientRegistration()
    {
        // Wait for any cleanup-triggered checkpoints to complete
        await WaitForNoRecentCheckpoints();
        
        var startTime = DateTime.UtcNow;
        var settings = await RegisterSettings<SettingsWithCustomAction>();

        await ValidateCheckpointWasCreated(startTime, settings.ClientName, "Initial Registration");
    }
    
    [Test]
    public async Task ShallNotCheckpointOnDuplicateClientRegistration()
    {
        // Wait for any cleanup-triggered checkpoints to complete
        await WaitForNoRecentCheckpoints();
        
        var secret = GetNewSecret();
        var startTime = DateTime.UtcNow;
        var settings = await RegisterSettings<SettingsWithCustomAction>(secret);
        await RegisterSettings<SettingsWithCustomAction>(secret);
        await RegisterSettings<SettingsWithCustomAction>(secret);

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
        await RegisterClientAndWaitForCheckpoint<ClientXWithThreeSettings>();
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
    
    [Test]
    public async Task ShallApplyCheckPoint()
    {
        var startTime = DateTime.UtcNow;
        var settings = await RegisterClientAndWaitForCheckpoint<ThreeSettings>();
        await RegisterClientAndWaitForCheckpoint<ClientXWithThreeSettings>();

        var checkpointCollection = await GetCheckpoints(startTime, DateTime.UtcNow);
        
        const string newValue = "Some new value";
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract(newValue))
        };
        
        await SetSettings(settings.ClientName, settingsToUpdate);
        await RegisterClientAndWaitForCheckpoint<ClientA>();

        await ApplyCheckPoint(checkpointCollection.CheckPoints.OrderBy(a => a.Timestamp).Last());

        var clients = (await GetAllClients()).ToList();
        
        Assert.Multiple(() =>
        {
            Assert.That(clients.Count, Is.EqualTo(2));
            Assert.That(string.Join(",", clients.Select(a => a.Name).OrderBy(a => a)), Is.EqualTo("ClientX,ThreeSettings"));
            Assert.That(clients.Single(a => a.Name == settings.ClientName)
                .Settings.Single(a => a.Name == nameof(settings.AStringSetting))
                .Value!.GetValue()!
                .ToString() == settings.AStringSetting);
        });
    }

    [Test]
    public async Task ShallNotAllowGetCheckpointsIfNotAdmin()
    {
        var naughtyUser = NewUser(Guid.NewGuid().ToString());
        await CreateUser(naughtyUser);

        var loginResult = await Login(naughtyUser.Username, naughtyUser.Password!);
        
        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", loginResult.Token);
        var startTime = DateTime.UtcNow - TimeSpan.FromSeconds(1);
        var endTime = DateTime.UtcNow;
        var result = await httpClient.GetAsync("/timemachine" + 
                                               $"?startTime={Uri.EscapeDataString(startTime.ToString("o"))}" +
                                               $"&endTime={Uri.EscapeDataString(endTime.ToString("o"))}");

        Assert.That((int) result.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized),
            "Only administrators are able to export data");
    }
    
    [Test]
    public async Task ShallAddNoteToCheckPoint()
    {
        var startTime = DateTime.UtcNow;
        var settings = await RegisterSettings<SettingsWithCustomAction>();

        await ValidateCheckpointWasCreated(startTime, settings.ClientName, "Initial Registration");
        
        var checkpointCollection1 = await GetCheckpoints(startTime, DateTime.UtcNow);

        var checkPoint = checkpointCollection1.CheckPoints.Single();
        var update = new CheckPointUpdateDataContract("This is an update");
        await UpdateCheckPoint(checkPoint.Id, update);
        
        var checkpointCollection2 = await GetCheckpoints(startTime, DateTime.UtcNow);
        var updatedCheckPoint = checkpointCollection2.CheckPoints.Single();
        
        Assert.That(updatedCheckPoint.Note, Is.EqualTo(update.Note));
    }
    
    [Test]
    public async Task ShallGetCheckPointData()
    {
        var startTime = DateTime.UtcNow;
        await RegisterClientAndWaitForCheckpoint<SettingsWithCustomAction>();

        var checkPoints = await GetCheckpoints(startTime, DateTime.UtcNow);

        var data = await GetCheckPointData(checkPoints.CheckPoints.Single().DataId);
        
        Assert.That(data, Is.Not.Null);
        Assert.That(data!.Clients.Count, Is.EqualTo(1));
    }

    private async Task ValidateCheckpointWasCreated(DateTime startTime,
        params string[] messageContains)
    {
        await WaitForCondition(async () => (await GetCheckpoints(startTime, DateTime.UtcNow)).CheckPoints.Count() == 1,
            TimeSpan.FromSeconds(5), 
            () => $"Expected 1 checkpoint but was actually {GetCheckpoints(startTime, DateTime.UtcNow).GetAwaiter().GetResult().CheckPoints.Count()}");

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

    private async Task UpdateCheckPoint(Guid id, CheckPointUpdateDataContract update)
    {
        var uri = $"/timemachine/{Uri.EscapeDataString(id.ToString())}/note";
        await ApiClient.Put<HttpResponseMessage>(uri, update);
    }
}