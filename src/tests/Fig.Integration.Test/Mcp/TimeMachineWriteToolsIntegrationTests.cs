using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fig.Contracts.CheckPoint;
using Fig.Mcp.Tools;
using Fig.Test.Common.TestSettings;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Integration.Test.Mcp;

[TestFixture]
public class TimeMachineWriteToolsIntegrationTests : McpToolIntegrationTestBase
{
    [Test]
    public async Task ApplyCheckPoint_WithExistingCheckpoint_DoesNotCrash()
    {
        await SetConfiguration(CreateConfiguration(enableTimeMachine: true));
        await WaitForNoRecentCheckpoints();

        var secret = GetNewSecret();
        await RegisterSettings<ClientA>(secret);

        await Task.Delay(2000);

        var listResult = await TimeMachineReadTools.ListCheckPoints(
            McpApiClient,
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow.AddHours(1),
            CancellationToken.None);

        var collection = JsonConvert.DeserializeObject<CheckPointCollectionDataContract>(listResult);
        if (collection?.CheckPoints == null || !collection.CheckPoints.Any())
        {
            Assert.Inconclusive("No checkpoints available");
            return;
        }

        var checkpointId = collection.CheckPoints.First().Id.ToString();

        var result = await TimeMachineWriteTools.ApplyCheckPoint(
            McpApiClient, checkpointId, CancellationToken.None);
        Assert.That(result, Does.Contain("applied successfully"));
    }

    [Test]
    public async Task UpdateCheckPointNote_WithExistingCheckpoint_UpdatesNote()
    {
        await SetConfiguration(CreateConfiguration(enableTimeMachine: true));
        await WaitForNoRecentCheckpoints();

        var secret = GetNewSecret();
        await RegisterSettings<ClientA>(secret);

        await Task.Delay(2000);

        var listResult = await TimeMachineReadTools.ListCheckPoints(
            McpApiClient,
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow.AddHours(1),
            CancellationToken.None);

        var collection = JsonConvert.DeserializeObject<CheckPointCollectionDataContract>(listResult);
        if (collection?.CheckPoints == null || !collection.CheckPoints.Any())
        {
            Assert.Inconclusive("No checkpoints available");
            return;
        }

        var checkpointId = collection.CheckPoints.First().Id.ToString();

        var result = await TimeMachineWriteTools.UpdateCheckPointNote(
            McpApiClient, checkpointId, "Integration test note", CancellationToken.None);
        Assert.That(result, Does.Contain("Note updated"));
    }
}
