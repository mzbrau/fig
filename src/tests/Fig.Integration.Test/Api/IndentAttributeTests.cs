using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fig.Client.Attributes;
using Fig.Client.Enums;
using Fig.Contracts.ImportExport;
using Fig.Contracts.SettingDefinitions;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

[TestFixture]
public class IndentAttributeTests : IntegrationTestBase
{
    [Test]
    public async Task ShallRegisterSettingsWithIndentAttribute()
    {
        var settings = await RegisterSettings<SettingsWithIndentAttribute>();
        var client = await GetSingleClientWithExpectedSettings(settings.ClientName, 6);

        ValidateIndentLevels(client.Settings, SettingsWithIndentAttributeTestCases);
    }

    [Test]
    public async Task ShallExportSettingsWithIndentAttribute()
    {
        await RegisterSettings<SettingsWithIndentAttribute>();
        var exportData = await ExportData();

        var client = GetSingleExportedClient(exportData, 6);
        ValidateIndentLevels(client.Settings, SettingsWithIndentAttributeTestCases);
    }

    [Test]
    public async Task ShallImportSettingsWithIndentAttribute()
    {
        await RegisterSettings<SettingsWithIndentAttribute>();

        var exportData = await ExportData();
        exportData.ImportType = ImportType.ClearAndImport;

        // Modify indent values in export data
        ModifySettingIndent(exportData, nameof(SettingsWithIndentAttribute.ChildSetting), 4);
        ModifySettingIndent(exportData, nameof(SettingsWithIndentAttribute.GrandchildSetting), null);

        await ImportData(exportData);

        var client = await GetSingleClientAfterImport();

        AssertSettingHasIndent(client.Settings, nameof(SettingsWithIndentAttribute.ChildSetting), 4,
            "Modified indent level should be preserved on import");
        AssertSettingHasIndent(client.Settings, nameof(SettingsWithIndentAttribute.GrandchildSetting), null,
            "Removed indent should be preserved on import");
    }

    [Test]
    public async Task IndentAttribute_EndToEndFlow_ShouldWorkCorrectly()
    {
        var settings = await RegisterSettings<IndentTestSettings>();
        var client = await GetSingleClientWithExpectedSettings(settings.ClientName, 6);

        // Verify registration
        ValidateIndentLevels(client.Settings, IndentTestSettingsTestCases);

        // Verify export
        var exportData = await ExportData();
        var exportedClient = GetSingleExportedClient(exportData, 6);
        ValidateIndentLevels(exportedClient.Settings, IndentTestSettingsTestCases, "Export: ");

        // Test import with modifications
        ModifySettingIndent(exportData, nameof(IndentTestSettings.Level1Setting), 4);
        ModifySettingIndent(exportData, nameof(IndentTestSettings.ZeroIndentSetting), null);

        exportData.ImportType = ImportType.ClearAndImport;
        await ImportData(exportData);

        var importedClient = await GetSingleClientAfterImport();

        AssertSettingHasIndent(importedClient.Settings, nameof(IndentTestSettings.Level1Setting), 4,
            "Imported level 1 setting should have modified indent");
        AssertSettingHasIndent(importedClient.Settings, nameof(IndentTestSettings.ZeroIndentSetting), null,
            "Imported zero indent setting should have null indent");
        AssertSettingHasIndent(importedClient.Settings, nameof(IndentTestSettings.Level2Setting), 2,
            "Imported level 2 setting should maintain original indent");
    }

    /// <summary>
    /// Gets a single client after registration and validates basic properties.
    /// </summary>
    private async Task<SettingsClientDefinitionDataContract> GetSingleClientWithExpectedSettings(string expectedClientName, int expectedSettingsCount)
    {
        var clients = (await GetAllClients()).ToList();
        Assert.That(clients.Count, Is.EqualTo(1));

        var client = clients.First();
        Assert.That(client.Name, Is.EqualTo(expectedClientName));
        Assert.That(client.Settings.Count, Is.EqualTo(expectedSettingsCount));

        return client;
    }

    /// <summary>
    /// Gets a single client from export data and validates basic properties.
    /// </summary>
    private static SettingClientExportDataContract GetSingleExportedClient(FigDataExportDataContract exportData, int expectedSettingsCount)
    {
        Assert.That(exportData.Clients.Count, Is.EqualTo(1));

        var client = exportData.Clients.First();
        Assert.That(client.Settings.Count, Is.EqualTo(expectedSettingsCount));

        return client;
    }

    /// <summary>
    /// Gets a single client after import operation.
    /// </summary>
    private async Task<SettingsClientDefinitionDataContract> GetSingleClientAfterImport()
    {
        var clients = (await GetAllClients()).ToList();
        return clients.First();
    }

    /// <summary>
    /// Validates that settings have the expected indent levels.
    /// </summary>
    private static void ValidateIndentLevels(IReadOnlyList<SettingDefinitionDataContract> settings,
        (string SettingName, int? ExpectedIndent, string Message)[] testCases,
        string messagePrefix = "")
    {
        foreach (var (settingName, expectedIndent, message) in testCases)
        {
            var setting = settings.FirstOrDefault(s => s.Name == settingName);
            Assert.That(setting, Is.Not.Null, $"Setting {settingName} should exist");
            Assert.That(setting!.Indent, Is.EqualTo(expectedIndent), $"{messagePrefix}{message}");
        }
    }

    /// <summary>
    /// Validates that settings have the expected indent levels (for export data).
    /// </summary>
    private static void ValidateIndentLevels(IReadOnlyList<SettingExportDataContract> settings,
        (string SettingName, int? ExpectedIndent, string Message)[] testCases,
        string messagePrefix = "")
    {
        foreach (var (settingName, expectedIndent, message) in testCases)
        {
            var setting = settings.FirstOrDefault(s => s.Name == settingName);
            Assert.That(setting, Is.Not.Null, $"Setting {settingName} should exist");
            Assert.That(setting!.Indent, Is.EqualTo(expectedIndent), $"{messagePrefix}{message}");
        }
    }

    /// <summary>
    /// Asserts that a specific setting has the expected indent level.
    /// </summary>
    private static void AssertSettingHasIndent(IReadOnlyList<SettingDefinitionDataContract> settings, string settingName, int? expectedIndent, string message)
    {
        var setting = settings.FirstOrDefault(s => s.Name == settingName);
        Assert.That(setting, Is.Not.Null, $"Setting {settingName} should exist");
        Assert.That(setting!.Indent, Is.EqualTo(expectedIndent), message);
    }

    /// <summary>
    /// Modifies the indent level of a setting in export data.
    /// </summary>
    private static void ModifySettingIndent(FigDataExportDataContract exportData, string settingName, int? newIndentLevel)
    {
        var setting = exportData.Clients.First().Settings.FirstOrDefault(s => s.Name == settingName);
        if (setting != null)
        {
            setting.Indent = newIndentLevel;
        }
    }

    private static readonly (string SettingName, int? ExpectedIndent, string Message)[] SettingsWithIndentAttributeTestCases =
    [
        (nameof(SettingsWithIndentAttribute.RootSetting), null, "Root setting should have no indent"),
        (nameof(SettingsWithIndentAttribute.ChildSetting), 1, "Child setting should have indent level 1"),
        (nameof(SettingsWithIndentAttribute.GrandchildSetting), 2, "Grandchild setting should have indent level 2"),
        (nameof(SettingsWithIndentAttribute.DeepNestedSetting), 3, "Deep nested setting should have indent level 3"),
        (nameof(SettingsWithIndentAttribute.MaxIndentSetting), 5, "Max indent setting should have indent level 5"),
        (nameof(SettingsWithIndentAttribute.ZeroIndentSetting), 0, "Zero indent setting should have indent level 0")
    ];

    private static readonly (string SettingName, int? ExpectedIndent, string Message)[] IndentTestSettingsTestCases =
    [
        (nameof(IndentTestSettings.RootSetting), null, "Root setting should have no indent"),
        (nameof(IndentTestSettings.Level1Setting), 1, "Level 1 setting should have indent 1"),
        (nameof(IndentTestSettings.Level2Setting), 2, "Level 2 setting should have indent 2"),
        (nameof(IndentTestSettings.Level3Setting), 3, "Level 3 setting should have indent 3"),
        (nameof(IndentTestSettings.ZeroIndentSetting), 0, "Zero indent setting should have indent 0"),
        (nameof(IndentTestSettings.MaxIndentSetting), 5, "Max indent setting should have indent 5")
    ];

    private class IndentTestSettings : TestSettingsBase
    {
        public override string ClientDescription => "Test settings for indent functionality";
        public override string ClientName => "IndentTestSettings";

        [Setting("Root setting")]
        [Fig.Client.Attributes.Category("Test", CategoryColor.Blue)]
        public string RootSetting { get; set; } = "Root";

        [Setting("Level 1 indented")]
        [Fig.Client.Attributes.Category("Test", CategoryColor.Blue)]
        [Indent(1)]
        public string Level1Setting { get; set; } = "Level1";

        [Setting("Level 2 indented")]
        [Fig.Client.Attributes.Category("Test", CategoryColor.Blue)]
        [Indent(2)]
        public string Level2Setting { get; set; } = "Level2";

        [Setting("Level 3 indented")]
        [Fig.Client.Attributes.Category("Test", CategoryColor.Blue)]
        [Indent(3)]
        public string Level3Setting { get; set; } = "Level3";

        [Setting("Zero indent")]
        [Fig.Client.Attributes.Category("Test", CategoryColor.Red)]
        [Indent(0)]
        public string ZeroIndentSetting { get; set; } = "Zero";

        [Setting("Max indent")]
        [Fig.Client.Attributes.Category("Test", CategoryColor.Red)]
        [Indent(5)]
        public string MaxIndentSetting { get; set; } = "Max";

        public override IEnumerable<string> GetValidationErrors() => [];
    }
}
