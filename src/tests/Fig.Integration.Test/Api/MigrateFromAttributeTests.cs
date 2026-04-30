using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fig.Client.Abstractions.Attributes;
using Fig.Contracts.Settings;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

[TestFixture]
[NonParallelizable]
public class MigrateFromAttributeTests : IntegrationTestBase
{
    private const string ClientName = "MigrateFromClient";

    [Test]
    public async Task ShallMigrateValueWhenSettingIsRenamed()
    {
        var secret = GetNewSecret();
        await RegisterSettings<OriginalSettings>(secret);
        await SetSettings(ClientName, [new(nameof(OriginalSettings.OldSetting), new StringSettingDataContract("preserved value"))]);

        await RegisterSettings<RenamedSettings>(secret);

        var settings = await GetSettingsForClient(ClientName, secret);
        Assert.That(GetValue(settings, nameof(RenamedSettings.NewSetting)), Is.EqualTo("preserved value"));
        Assert.That(settings.Any(s => s.Name == nameof(OriginalSettings.OldSetting)), Is.False);
    }

    [Test]
    public async Task ShallUseDefaultWhenMigrateFromSourceDoesNotExist()
    {
        var secret = GetNewSecret();
        await RegisterSettings<UnrelatedSettings>(secret);

        await RegisterSettings<MissingSourceSettings>(secret);

        var settings = await GetSettingsForClient(ClientName, secret);
        Assert.That(GetValue(settings, nameof(MissingSourceSettings.NewSetting)), Is.EqualTo("new default"));
    }

    [Test]
    public async Task ShallPreferExactNameMatchOverMigrateFromSource()
    {
        var secret = GetNewSecret();
        await RegisterSettings<SettingsWithExistingTargetAndSource>(secret);
        await SetSettings(ClientName,
        [
            new(nameof(SettingsWithExistingTargetAndSource.NewSetting), new StringSettingDataContract("exact value")),
            new(nameof(SettingsWithExistingTargetAndSource.OldSetting), new StringSettingDataContract("source value"))
        ]);

        await RegisterSettings<RenamedSettings>(secret);

        var settings = await GetSettingsForClient(ClientName, secret);
        Assert.That(GetValue(settings, nameof(RenamedSettings.NewSetting)), Is.EqualTo("exact value"));
    }

    [Test]
    public async Task ShallMigrateValuesForAllInstances()
    {
        var secret = GetNewSecret();
        await RegisterSettings<OriginalSettings>(secret);
        await SetSettings(ClientName, [new(nameof(OriginalSettings.OldSetting), new StringSettingDataContract("default instance"))]);
        await SetSettings(ClientName, [new(nameof(OriginalSettings.OldSetting), new StringSettingDataContract("named instance"))], "Instance1");

        await RegisterSettings<RenamedSettings>(secret);

        var defaultSettings = await GetSettingsForClient(ClientName, secret);
        var instanceSettings = await GetSettingsForClient(ClientName, secret, "Instance1");
        Assert.That(GetValue(defaultSettings, nameof(RenamedSettings.NewSetting)), Is.EqualTo("default instance"));
        Assert.That(GetValue(instanceSettings, nameof(RenamedSettings.NewSetting)), Is.EqualTo("named instance"));
    }

    [Test]
    public async Task ShallKeepDefaultWhenMigrateFromSourceTypeDiffers()
    {
        var secret = GetNewSecret();
        await RegisterSettings<OriginalSettings>(secret);
        await SetSettings(ClientName, [new(nameof(OriginalSettings.OldSetting), new StringSettingDataContract("not an int"))]);

        await RegisterSettings<TypeMismatchSettings>(secret);

        var settings = await GetSettingsForClient(ClientName, secret);
        Assert.That(GetValue(settings, nameof(TypeMismatchSettings.NewSetting)), Is.EqualTo(42));
    }

    [Test]
    public async Task ShallMigrateSecretSettingValue()
    {
        var secret = GetNewSecret();
        await RegisterSettings<OriginalSecretSettings>(secret);
        await SetSettings(ClientName, [new(nameof(OriginalSecretSettings.OldSecret), new StringSettingDataContract("super secret"))]);

        await RegisterSettings<RenamedSecretSettings>(secret);

        var settings = await GetSettingsForClient(ClientName, secret);
        Assert.That(GetValue(settings, nameof(RenamedSecretSettings.NewSecret)), Is.EqualTo("super secret"));
    }

    [Test]
    public async Task ShallResolveNestedMigrateFromSourceInSameNestedSetting()
    {
        var secret = GetNewSecret();
        await RegisterSettings<OriginalNestedSettings>(secret);
        await SetSettings(ClientName, [new("Nested->OldSetting", new StringSettingDataContract("nested value"))]);

        await RegisterSettings<RenamedNestedSettings>(secret);

        var settings = await GetSettingsForClient(ClientName, secret);
        Assert.That(GetValue(settings, "Nested->NewSetting"), Is.EqualTo("nested value"));
    }

    [Test]
    public async Task ShallAcceptExplicitNestedMigrateFromPath()
    {
        var secret = GetNewSecret();
        await RegisterSettings<OriginalExplicitNestedSettings>(secret);
        await SetSettings(ClientName, [new("NestedB->OldSetting", new StringSettingDataContract("other nested value"))]);

        await RegisterSettings<RenamedExplicitNestedSettings>(secret);

        var settings = await GetSettingsForClient(ClientName, secret);
        Assert.That(GetValue(settings, "NestedA->NewSetting"), Is.EqualTo("other nested value"));
    }

    [Test]
    public async Task ShallMigrateAndWarnInDebugWhenMigrateFromSourceStillExists()
    {
        var secret = GetNewSecret();
        await RegisterSettings<OriginalSettings>(secret);
        await SetSettings(ClientName, [new(nameof(OriginalSettings.OldSetting), new StringSettingDataContract("source value"))]);

        await RegisterSettings<AmbiguousSourceSettings>(secret);

        var settings = await GetSettingsForClient(ClientName, secret);
        Assert.That(GetValue(settings, nameof(AmbiguousSourceSettings.NewSetting)), Is.EqualTo("source value"));
    }

    private static object? GetValue(IEnumerable<SettingDataContract> settings, string settingName)
    {
        return settings.First(s => s.Name == settingName).Value?.GetValue();
    }

    private abstract class MigrateFromTestSettingsBase : TestSettingsBase
    {
        public override string ClientName => MigrateFromAttributeTests.ClientName;

        public override string ClientDescription => "Settings for MigrateFrom integration testing";

        public override IEnumerable<string> GetValidationErrors() => [];
    }

    private class OriginalSettings : MigrateFromTestSettingsBase
    {
        [Setting("Original setting")]
        public string OldSetting { get; set; } = "old default";
    }

    private class RenamedSettings : MigrateFromTestSettingsBase
    {
        [Setting("Renamed setting")]
        [MigrateFrom(nameof(OriginalSettings.OldSetting))]
        public string NewSetting { get; set; } = "new default";
    }

    private class UnrelatedSettings : MigrateFromTestSettingsBase
    {
        [Setting("Unrelated setting")]
        public string OtherSetting { get; set; } = "other";
    }

    private class MissingSourceSettings : MigrateFromTestSettingsBase
    {
        [Setting("Renamed setting")]
        [MigrateFrom("MissingSetting")]
        public string NewSetting { get; set; } = "new default";
    }

    private class SettingsWithExistingTargetAndSource : MigrateFromTestSettingsBase
    {
        [Setting("Existing target setting")]
        public string NewSetting { get; set; } = "existing target";

        [Setting("Existing source setting")]
        public string OldSetting { get; set; } = "existing source";
    }

    private class TypeMismatchSettings : MigrateFromTestSettingsBase
    {
        [Setting("Renamed setting")]
        [MigrateFrom(nameof(OriginalSettings.OldSetting))]
        public int NewSetting { get; set; } = 42;
    }

    private class OriginalSecretSettings : MigrateFromTestSettingsBase
    {
        [Secret]
        [Setting("Original secret")]
        public string OldSecret { get; set; } = "old secret";
    }

    private class RenamedSecretSettings : MigrateFromTestSettingsBase
    {
        [Secret]
        [Setting("Renamed secret")]
        [MigrateFrom(nameof(OriginalSecretSettings.OldSecret))]
        public string NewSecret { get; set; } = "new secret";
    }

    private class OriginalNestedSettings : MigrateFromTestSettingsBase
    {
        [NestedSetting]
        public SameNestedSettings Nested { get; set; } = new();

        public class SameNestedSettings
        {
            [Setting("Original nested setting")]
            public string OldSetting { get; set; } = "old nested";
        }
    }

    private class RenamedNestedSettings : MigrateFromTestSettingsBase
    {
        [NestedSetting]
        public SameNestedSettings Nested { get; set; } = new();

        public class SameNestedSettings
        {
            [Setting("Renamed nested setting")]
            [MigrateFrom("OldSetting")]
            public string NewSetting { get; set; } = "new nested";
        }
    }

    private class OriginalExplicitNestedSettings : MigrateFromTestSettingsBase
    {
        [NestedSetting]
        public SourceNestedSettings NestedB { get; set; } = new();

        public class SourceNestedSettings
        {
            [Setting("Original nested setting")]
            public string OldSetting { get; set; } = "old nested";
        }
    }

    private class RenamedExplicitNestedSettings : MigrateFromTestSettingsBase
    {
        [NestedSetting]
        public TargetNestedSettings NestedA { get; set; } = new();

        public class TargetNestedSettings
        {
            [Setting("Renamed nested setting")]
            [MigrateFrom("NestedB->OldSetting")]
            public string NewSetting { get; set; } = "new nested";
        }
    }

    private class AmbiguousSourceSettings : MigrateFromTestSettingsBase
    {
        [Setting("Original setting still present")]
        public string OldSetting { get; set; } = "old default";

        [Setting("Renamed setting")]
        [MigrateFrom(nameof(OldSetting))]
        public string NewSetting { get; set; } = "new default";
    }
}
