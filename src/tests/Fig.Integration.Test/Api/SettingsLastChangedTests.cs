using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fig.Contracts.Settings;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

[TestFixture]
public class SettingsLastChangedTests : IntegrationTestBase
{
    [Test]
    public async Task ShallReturnBulkLastChangedForAllClients()
    {
        var settingsA = await RegisterSettings<ThreeSettings>();
        var settingsB = await RegisterSettings<ClientA>();

        // Update a setting on each client so they appear in the last-changed data
        await SetSettings(settingsA.ClientName, new List<SettingDataContract>
        {
            new(nameof(settingsA.AStringSetting), new StringSettingDataContract("updatedA"))
        }, message: "update A");

        await SetSettings(settingsB.ClientName, new List<SettingDataContract>
        {
            new(nameof(settingsB.WebsiteAddress), new StringSettingDataContract("http://example.com"))
        }, message: "update B");

        var bulkResult = await GetLastChangedForAllClientsSettings();

        Assert.That(bulkResult, Is.Not.Null);

        var clientAResult = bulkResult.FirstOrDefault(c => c.Name == settingsA.ClientName && c.Instance == null);
        Assert.That(clientAResult, Is.Not.Null, "Client A should be present in bulk results");
        Assert.That(clientAResult!.Settings.Count, Is.GreaterThanOrEqualTo(1));

        var clientBResult = bulkResult.FirstOrDefault(c => c.Name == settingsB.ClientName && c.Instance == null);
        Assert.That(clientBResult, Is.Not.Null, "Client B should be present in bulk results");
        Assert.That(clientBResult!.Settings.Count, Is.GreaterThanOrEqualTo(1));
    }
}