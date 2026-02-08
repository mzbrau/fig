using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fig.Contracts.Settings;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

[TestFixture]
public class ExportLastChangedTests : IntegrationTestBase
{
    [Test]
    public async Task ShallExcludeLastChangedDetailsByDefault()
    {
        await RegisterSettings<ThreeSettings>();

        var data = await ExportData();

        var client = data.Clients.First();
        Assert.That(client.Settings.All(s => s.LastChangedDetails == null), Is.True,
            "LastChangedDetails should be null when includeLastChanged is not set");
    }

    [Test]
    public async Task ShallIncludeLastChangedDetailsWhenRequested()
    {
        var settings = await RegisterSettings<ThreeSettings>();

        // Make a change so there is history
        var update = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract("updated"))
        };
        await SetSettings(settings.ClientName, update, message: "export test");

        var data = await ExportData(includeLastChanged: true);

        var client = data.Clients.First();
        var setting = client.Settings.First(s => s.Name == nameof(settings.AStringSetting));

        Assert.That(setting.LastChangedDetails, Is.Not.Null);
        Assert.That(setting.LastChangedDetails!.ChangedBy, Is.EqualTo(UserName));
        Assert.That(setting.LastChangedDetails.ChangeMessage, Is.EqualTo("export test"));
    }

    [Test]
    public async Task ShallPopulateLastChangedForAllSettingsInExport()
    {
        await RegisterSettings<ThreeSettings>();

        var data = await ExportData(includeLastChanged: true);

        var client = data.Clients.First();

        // All settings should have last changed details (from initial registration)
        foreach (var s in client.Settings)
        {
            Assert.That(s.LastChangedDetails, Is.Not.Null,
                $"Setting {s.Name} should have LastChangedDetails populated");
            Assert.That(s.LastChangedDetails!.ChangedBy, Is.Not.Null.And.Not.Empty);
        }
    }

    [Test]
    public async Task ShallIncludeLastChangedDetailsForMultipleClients()
    {
        await RegisterSettings<ThreeSettings>();
        await RegisterSettings<AllSettingsAndTypes>();

        var data = await ExportData(includeLastChanged: true);

        Assert.That(data.Clients.Count, Is.EqualTo(2));

        foreach (var client in data.Clients)
        {
            Assert.That(client.Settings.All(s => s.LastChangedDetails != null), Is.True,
                $"All settings for {client.Name} should have LastChangedDetails");
        }
    }
}
