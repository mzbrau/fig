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
public class TimeMachineReadToolsIntegrationTests : McpToolIntegrationTestBase
{
    [Test]
    public async Task ListCheckPoints_AfterRegisteringClient_ReturnsValidJson()
    {
        await SetConfiguration(CreateConfiguration(enableTimeMachine: true));
        await WaitForNoRecentCheckpoints();

        var secret = GetNewSecret();
        await RegisterSettings<ClientA>(secret);

        var result = await TimeMachineReadTools.ListCheckPoints(
            McpApiClient,
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow.AddHours(1),
            CancellationToken.None);

        Assert.That(result, Is.Not.Null.And.Not.Empty);
        Assert.DoesNotThrow(() => JsonConvert.DeserializeObject(result));
    }

    [Test]
    public async Task GetCheckPointData_ForExistingCheckpoint_ReturnsData()
    {
        await SetConfiguration(CreateConfiguration(enableTimeMachine: true));
        await WaitForNoRecentCheckpoints();

        var secret = GetNewSecret();
        var startTime = DateTime.UtcNow.AddSeconds(-1);
        await RegisterSettings<ClientA>(secret);

        var collection = await WaitForMcpCheckPoint(startTime, DateTime.UtcNow.AddHours(1));

        var dataId = collection.CheckPoints.First().DataId.ToString();
        var result = await TimeMachineReadTools.GetCheckPointData(
            McpApiClient, dataId, CancellationToken.None);

        Assert.That(result, Is.Not.Null.And.Not.Empty);
    }
}
