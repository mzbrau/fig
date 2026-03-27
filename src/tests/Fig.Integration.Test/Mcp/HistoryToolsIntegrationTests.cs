using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fig.Contracts.Settings;
using Fig.Mcp.Tools;
using Fig.Test.Common.TestSettings;
using NUnit.Framework;

namespace Fig.Integration.Test.Mcp;

[TestFixture]
public class HistoryToolsIntegrationTests : McpToolIntegrationTestBase
{
    [Test]
    public async Task GetSettingHistory_AfterUpdate_ContainsUpdatedValue()
    {
        var clientSecret = GetNewSecret();
        var settings = await RegisterSettings<ClientA>(clientSecret);

        const string newValue = "http://www.updated-example.com";
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(ClientA.WebsiteAddress), new StringSettingDataContract(newValue))
        };
        await SetSettings(settings.ClientName, settingsToUpdate);

        var result = await HistoryTools.GetSettingHistory(
            McpApiClient, settings.ClientName, nameof(ClientA.WebsiteAddress), null, CancellationToken.None);

        Assert.That(result, Is.Not.Null.And.Not.Empty);
        Assert.That(result, Does.Contain(newValue));
    }

    [Test]
    public async Task GetLastChanged_AfterRegistration_ReturnsNonEmptyResult()
    {
        var clientSecret = GetNewSecret();
        await RegisterSettings<ClientA>(clientSecret);

        var result = await HistoryTools.GetLastChanged(McpApiClient, CancellationToken.None);

        Assert.That(result, Is.Not.Null.And.Not.Empty);
    }
}
