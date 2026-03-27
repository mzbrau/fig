using System;
using System.Threading;
using System.Threading.Tasks;
using Fig.Mcp.Tools;
using Fig.Test.Common.TestSettings;
using NUnit.Framework;

namespace Fig.Integration.Test.Mcp;

[TestFixture]
public class EventToolsIntegrationTests : McpToolIntegrationTestBase
{
    [Test]
    public async Task GetEvents_AfterRegistration_ContainsClientName()
    {
        var startTime = DateTime.UtcNow.AddMinutes(-1);
        var clientSecret = GetNewSecret();
        var settings = await RegisterSettings<ClientA>(clientSecret);
        var endTime = DateTime.UtcNow.AddMinutes(1);

        var result = await EventTools.GetEvents(McpApiClient, startTime, endTime, CancellationToken.None);

        Assert.That(result, Does.Contain(settings.ClientName));
    }

    [Test]
    public async Task GetEventCount_AfterRegistration_ReturnsCountJson()
    {
        var clientSecret = GetNewSecret();
        await RegisterSettings<ClientA>(clientSecret);

        var result = await EventTools.GetEventCount(McpApiClient, CancellationToken.None);

        Assert.That(result, Does.Contain("EventLogCount"));
    }

    [Test]
    public async Task GetClientTimeline_AfterRegistration_ReturnsNonEmptyResult()
    {
        var clientSecret = GetNewSecret();
        var settings = await RegisterSettings<ClientA>(clientSecret);

        var result = await EventTools.GetClientTimeline(McpApiClient, settings.ClientName, CancellationToken.None);

        Assert.That(result, Is.Not.Null.And.Not.Empty);
    }
}
