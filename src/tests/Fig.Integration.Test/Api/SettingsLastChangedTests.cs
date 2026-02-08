using System;
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
    public async Task ShallReturnLastChangedEntryPerSetting()
    {
        var settings = await RegisterSettings<ThreeSettings>();

        // Update one setting twice
        var firstUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract("firstUpdate"))
        };
        await SetSettings(settings.ClientName, firstUpdate, message: "first");

        var secondUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract("secondUpdate"))
        };
        await SetSettings(settings.ClientName, secondUpdate, message: "second");

        var lastChanged = (await GetLastChangedForAllSettings(settings.ClientName)).ToList();

        Assert.That(lastChanged, Is.Not.Null);
        Assert.That(lastChanged.Count, Is.EqualTo(3), "Should have one entry per setting");

        var stringSettingEntry = lastChanged.First(e => e.Name == nameof(settings.AStringSetting));
        Assert.That(stringSettingEntry.Value, Is.EqualTo("secondUpdate"));
        Assert.That(stringSettingEntry.ChangeMessage, Is.EqualTo("second"));
        Assert.That(stringSettingEntry.ChangedBy, Is.EqualTo(UserName));
    }

    [Test]
    public async Task ShallReturnLastChangedForInstance()
    {
        var settings = await RegisterSettings<ThreeSettings>();

        const string instance = "Inst1";
        var instUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract("instanceValue"))
        };
        await SetSettings(settings.ClientName, instUpdate, instance, message: "inst change");

        var lastChanged = (await GetLastChangedForAllSettings(settings.ClientName, instance)).ToList();

        Assert.That(lastChanged, Is.Not.Null);
        Assert.That(lastChanged.Any(e => e.Name == nameof(settings.AStringSetting)), Is.True);

        var entry = lastChanged.First(e => e.Name == nameof(settings.AStringSetting));
        Assert.That(entry.Value, Is.EqualTo("instanceValue"));
    }
}
