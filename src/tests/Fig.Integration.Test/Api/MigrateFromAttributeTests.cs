using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fig.Client.Abstractions.Attributes;
using Fig.Contracts.ImportExport;
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
    public async Task ShallBringOverSettingHistoryWhenSettingIsRenamed()
    {
        var secret = GetNewSecret();
        await RegisterSettings<OriginalSettings>(secret);
        await SetSettings(ClientName,
            [new(nameof(OriginalSettings.OldSetting), new StringSettingDataContract("preserved value"))],
            message: "history before rename");

        await RegisterSettings<RenamedSettings>(secret);

        var history = (await GetHistory(ClientName, nameof(RenamedSettings.NewSetting)))
            .OrderBy(a => a.ChangedAt)
            .ToList();

        Assert.That(history.Count, Is.EqualTo(3));
        Assert.That(history.All(a => a.Name == nameof(RenamedSettings.NewSetting)), Is.True);
        Assert.That(history.Select(a => a.Value).ToList(),
            Is.EqualTo(new[] { "old default", "preserved value", "preserved value" }));

        var renameEntry = history.Single(a => a.ChangedBy == "MIGRATE_FROM");
        Assert.That(renameEntry.ChangeMessage,
            Is.EqualTo("Setting renamed from 'OldSetting' to 'NewSetting'. Value migrated from 'preserved value' to 'preserved value'."));
    }

    [Test]
    public async Task ShallBringOverSettingHistoryForAllInstancesWhenSettingIsRenamed()
    {
        var secret = GetNewSecret();
        await RegisterSettings<OriginalSettings>(secret);
        await SetSettings(ClientName, [new(nameof(OriginalSettings.OldSetting), new StringSettingDataContract("default instance"))]);
        await SetSettings(ClientName, [new(nameof(OriginalSettings.OldSetting), new StringSettingDataContract("named instance"))], "Instance1");

        await RegisterSettings<RenamedSettings>(secret);

        var defaultHistory = (await GetHistory(ClientName, nameof(RenamedSettings.NewSetting)))
            .OrderBy(a => a.ChangedAt)
            .ToList();
        var instanceHistory = (await GetHistory(ClientName, nameof(RenamedSettings.NewSetting), instance: "Instance1"))
            .OrderBy(a => a.ChangedAt)
            .ToList();

        Assert.That(defaultHistory.Select(a => a.Value).ToList(),
            Is.EqualTo(new[] { "old default", "default instance", "default instance" }));
        Assert.That(defaultHistory.Count(a => a.ChangedBy == "MIGRATE_FROM"), Is.EqualTo(1));
        Assert.That(defaultHistory.All(a => a.Name == nameof(RenamedSettings.NewSetting)), Is.True);

        Assert.That(instanceHistory.Select(a => a.Value).ToList(),
            Is.EqualTo(new[] { "old default", "default instance", "named instance", "named instance" }));
        Assert.That(instanceHistory.Count(a => a.ChangedBy == "MIGRATE_FROM"), Is.EqualTo(1));
        Assert.That(instanceHistory.All(a => a.Name == nameof(RenamedSettings.NewSetting)), Is.True);
    }

    [Test]
    public async Task ShallKeepMigratedHistoryWhenMigrateFromAttributeIsRemoved()
    {
        var secret = GetNewSecret();
        await RegisterSettings<OriginalSettings>(secret);
        await SetSettings(ClientName,
            [new(nameof(OriginalSettings.OldSetting), new StringSettingDataContract("preserved value"))],
            message: "history before rename");

        await RegisterSettings<RenamedSettings>(secret);
        await RegisterSettings<FinalRenamedSettings>(secret);

        var history = (await GetHistory(ClientName, nameof(FinalRenamedSettings.NewSetting)))
            .OrderBy(a => a.ChangedAt)
            .ToList();

        Assert.That(history.Count, Is.EqualTo(3));
        Assert.That(history.All(a => a.Name == nameof(FinalRenamedSettings.NewSetting)), Is.True);
        Assert.That(history.Count(a => a.ChangedBy == "MIGRATE_FROM"), Is.EqualTo(1));
        Assert.That(history.Last().ChangeMessage,
            Is.EqualTo("Setting renamed from 'OldSetting' to 'NewSetting'. Value migrated from 'preserved value' to 'preserved value'."));
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

    [Test]
    public async Task ShallImportValueOnlySettingUsingMigrateFromSource()
    {
        var secret = GetNewSecret();
        await RegisterSettings<RenamedSettings>(secret);

        var import = CreateValueOnlyImport(
            new SettingValueExportDataContract(nameof(OriginalSettings.OldSetting), "imported value", false, null));
        var result = await ImportValueOnlyData(import);

        Assert.That(result.ImportedClients, Does.Contain(ClientName));
        Assert.That(result.ErrorMessage, Does.Contain(nameof(OriginalSettings.OldSetting)));
        Assert.That(result.ErrorMessage, Does.Contain(nameof(RenamedSettings.NewSetting)));
        Assert.That(result.ErrorMessage, Does.Contain("Update the import file"));
        Assert.That(result.ErrorMessage, Does.Not.Contain("did not exist"));

        var settings = await GetSettingsForClient(ClientName, secret);
        Assert.That(GetValue(settings, nameof(RenamedSettings.NewSetting)), Is.EqualTo("imported value"));
    }

    [Test]
    public async Task ShallPreferExactNameWhenValueOnlyImportContainsOldAndNewNames()
    {
        var secret = GetNewSecret();
        await RegisterSettings<RenamedSettings>(secret);

        var import = CreateValueOnlyImport(
            new SettingValueExportDataContract(nameof(OriginalSettings.OldSetting), "old imported value", false, null),
            new SettingValueExportDataContract(nameof(RenamedSettings.NewSetting), "new imported value", false, null));
        var result = await ImportValueOnlyData(import);

        Assert.That(result.ErrorMessage, Does.Contain("ignored"));
        Assert.That(result.ErrorMessage, Does.Contain(nameof(OriginalSettings.OldSetting)));
        Assert.That(result.ErrorMessage, Does.Not.Contain("did not exist"));

        var settings = await GetSettingsForClient(ClientName, secret);
        Assert.That(GetValue(settings, nameof(RenamedSettings.NewSetting)), Is.EqualTo("new imported value"));
    }

    [Test]
    public async Task ShallApplyDeferredValueOnlyImportUsingMigrateFromSource()
    {
        var secret = GetNewSecret();
        var import = CreateValueOnlyImport(
            new SettingValueExportDataContract(nameof(OriginalSettings.OldSetting), "deferred imported value", false, null));

        var result = await ImportValueOnlyData(import);
        Assert.That(result.DeferredImportClients, Does.Contain(ClientName));

        await RegisterSettings<RenamedSettings>(secret);

        var settings = await GetSettingsForClient(ClientName, secret);
        Assert.That(GetValue(settings, nameof(RenamedSettings.NewSetting)), Is.EqualTo("deferred imported value"));
    }

    [Test]
    public async Task ShallImportSecretValueOnlySettingUsingMigrateFromSource()
    {
        var secret = GetNewSecret();
        await RegisterSettings<OriginalSecretSettings>(secret);
        await SetSettings(ClientName, [new(nameof(OriginalSecretSettings.OldSecret), new StringSettingDataContract("imported secret"))]);

        var export = await ExportValueOnlyData();

        await RegisterSettings<RenamedSecretSettings>(secret);
        await SetSettings(ClientName, [new(nameof(RenamedSecretSettings.NewSecret), new StringSettingDataContract("current secret"))]);

        var result = await ImportValueOnlyData(export);

        Assert.That(result.ErrorMessage, Does.Contain(nameof(OriginalSecretSettings.OldSecret)));
        Assert.That(result.ErrorMessage, Does.Contain(nameof(RenamedSecretSettings.NewSecret)));

        var settings = await GetSettingsForClient(ClientName, secret);
        Assert.That(GetValue(settings, nameof(RenamedSecretSettings.NewSecret)), Is.EqualTo("imported secret"));
    }

    [Test]
    public async Task ShallKeepCurrentValueWhenValueOnlyImportMigrateFromValueIsInvalid()
    {
        var secret = GetNewSecret();
        await RegisterSettings<TypeMismatchSettings>(secret);

        var import = CreateValueOnlyImport(
            new SettingValueExportDataContract(nameof(OriginalSettings.OldSetting), "not an int", false, null));
        var result = await ImportValueOnlyData(import);

        Assert.That(result.ErrorMessage, Does.Contain(nameof(OriginalSettings.OldSetting)));
        Assert.That(result.ErrorMessage, Does.Contain(nameof(TypeMismatchSettings.NewSetting)));

        var settings = await GetSettingsForClient(ClientName, secret);
        Assert.That(GetValue(settings, nameof(TypeMismatchSettings.NewSetting)), Is.EqualTo(42));
    }

    private static object? GetValue(IEnumerable<SettingDataContract> settings, string settingName)
    {
        return settings.First(s => s.Name == settingName).Value?.GetValue();
    }

    private static FigValueOnlyDataExportDataContract CreateValueOnlyImport(params SettingValueExportDataContract[] settings)
    {
        return new FigValueOnlyDataExportDataContract(
            System.DateTime.UtcNow,
            ImportType.UpdateValues,
            1,
            null,
            [new SettingClientValueExportDataContract(ClientName, null, settings.ToList())]);
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

    private class FinalRenamedSettings : MigrateFromTestSettingsBase
    {
        [Setting("Renamed setting")]
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
