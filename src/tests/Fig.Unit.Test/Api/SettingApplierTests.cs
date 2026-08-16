using System;
using System.Collections.Generic;
using Fig.Api.Converters;
using Fig.Api.DataImport;
using Fig.Api.Exceptions;
using Fig.Api.Services;
using Fig.Contracts.ImportExport;
using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.BusinessEntities.SettingValues;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class SettingApplierTests
{
    private Mock<IEncryptionService> _encryption = null!;
    private SettingApplier _applier = null!;

    [SetUp]
    public void SetUp()
    {
        _encryption = new Mock<IEncryptionService>();
        var converter = new SettingConverter(new ValueToStringConverter());
        _applier = new SettingApplier(converter, _encryption.Object, NullLogger<SettingApplier>.Instance);
    }

    [Test]
    public void ApplySettings_UpdatesValue_WhenImportDiffers()
    {
        var client = CreateClient(
            CreateSetting("A", "old"));

        var result = _applier.ApplySettings(client,
        [
            new SettingValueExportDataContract("A", "new", false, false)
        ]);

        Assert.That(result.Changes, Has.Count.EqualTo(1));
        Assert.That(client.Settings.First().Value?.GetValue(), Is.EqualTo("new"));
        Assert.That(result.HandledImportSettingNames, Does.Contain("A"));
    }

    [Test]
    public void ApplySettings_DoesNotChange_WhenValuesAreEquivalent()
    {
        var client = CreateClient(CreateSetting("A", "same"));

        var result = _applier.ApplySettings(client,
        [
            new SettingValueExportDataContract("A", "same", false, false)
        ]);

        Assert.That(result.Changes, Is.Empty);
    }

    [Test]
    public void ApplySettings_UsesMigrateFrom_WhenSourceNoLongerRegistered()
    {
        var setting = CreateSetting("NewSetting", "default");
        setting.MigrateFrom = "OldSetting";
        var client = CreateClient(setting);

        var result = _applier.ApplySettings(client,
        [
            new SettingValueExportDataContract("OldSetting", "migrated", false, false)
        ]);

        Assert.That(result.Changes, Has.Count.EqualTo(1));
        Assert.That(client.Settings.First().Value?.GetValue(), Is.EqualTo("migrated"));
        Assert.That(result.Warnings, Has.Count.EqualTo(1));
        Assert.That(result.Warnings[0], Does.Contain("MigrateFrom"));
    }

    [Test]
    public void ApplySettings_WarnsAndSkips_WhenCustomMigrationMethodRequired()
    {
        var setting = CreateSetting("NewSetting", "default");
        setting.MigrateFrom = "OldSetting";
        setting.MigrateFromMigrationMethod = "Migrate";
        var client = CreateClient(setting);

        var result = _applier.ApplySettings(client,
        [
            new SettingValueExportDataContract("OldSetting", "migrated", false, false)
        ]);

        Assert.That(result.Changes, Is.Empty);
        Assert.That(result.Warnings, Has.Count.EqualTo(1));
        Assert.That(result.Warnings[0], Does.Contain("custom MigrateFrom"));
        Assert.That(client.Settings.First().Value?.GetValue(), Is.EqualTo("default"));
    }

    [Test]
    public void ApplySettings_IgnoresStaleMigrateFromSource_WhenBothPresent()
    {
        var setting = CreateSetting("NewSetting", "default");
        setting.MigrateFrom = "OldSetting";
        var client = CreateClient(setting);

        var result = _applier.ApplySettings(client,
        [
            new SettingValueExportDataContract("NewSetting", "new value", false, false),
            new SettingValueExportDataContract("OldSetting", "stale", false, false)
        ]);

        Assert.That(client.Settings.First().Value?.GetValue(), Is.EqualTo("new value"));
        Assert.That(result.Warnings.Any(w => w.Contains("ignored")), Is.True);
    }

    [Test]
    public void ApplySettings_DecryptsEncryptedValue()
    {
        _encryption.Setup(e => e.DecryptForImport("encrypted", null)).Returns("plain");
        var client = CreateClient(CreateSetting("Secret", "old"));

        var result = _applier.ApplySettings(client,
        [
            new SettingValueExportDataContract("Secret", "encrypted", true, false)
        ]);

        Assert.That(result.Changes, Has.Count.EqualTo(1));
        Assert.That(client.Settings.First().Value?.GetValue(), Is.EqualTo("plain"));
    }

    [Test]
    public void ApplySettings_ThrowsInvalidImport_WhenDecryptFails()
    {
        _encryption.Setup(e => e.DecryptForImport(It.IsAny<string>(), null))
            .Throws(new FormatException("bad"));
        var client = CreateClient(CreateSetting("Secret", "old"));

        Assert.Throws<InvalidImportException>(() => _applier.ApplySettings(client,
        [
            new SettingValueExportDataContract("Secret", "encrypted", true, false)
        ]));
    }

    private static SettingClientBusinessEntity CreateClient(params SettingBusinessEntity[] settings)
    {
        return new SettingClientBusinessEntity
        {
            Id = Guid.NewGuid(),
            Name = "TestClient",
            Settings = settings.ToList()
        };
    }

    private static SettingBusinessEntity CreateSetting(string name, string value)
    {
        return new SettingBusinessEntity
        {
            Name = name,
            ValueType = typeof(string),
            Value = new StringSettingBusinessEntity(value)
        };
    }
}
