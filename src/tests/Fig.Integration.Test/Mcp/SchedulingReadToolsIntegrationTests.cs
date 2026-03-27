using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fig.Contracts.Settings;
using Fig.Mcp.Tools;
using Fig.Test.Common.TestSettings;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Integration.Test.Mcp;

[TestFixture]
public class SchedulingReadToolsIntegrationTests : McpToolIntegrationTestBase
{
    [Test]
    public async Task ListDeferredChanges_WhenEmpty_ReturnsValidJson()
    {
        var result = await SchedulingReadTools.ListDeferredChanges(
            McpApiClient, CancellationToken.None);

        Assert.That(result, Is.Not.Null.And.Not.Empty);
        Assert.DoesNotThrow(() => JsonConvert.DeserializeObject(result));
    }

    [Test]
    public async Task ListDeferredChanges_AfterSchedulingChange_ReturnsChange()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ClientA>(secret);

        await SetSettings(settings.ClientName, new List<SettingDataContract>
        {
            new(nameof(settings.WebsiteAddress), new StringSettingDataContract("http://www.modified.com"))
        }, applyAt: DateTime.UtcNow.AddHours(1));

        var result = await SchedulingReadTools.ListDeferredChanges(
            McpApiClient, CancellationToken.None);

        Assert.That(result, Does.Contain(settings.ClientName));
    }
}
