using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fig.Client.Abstractions.Data;
using Fig.Common.Constants;
using Fig.Contracts.ImportExport;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Web.Facades;
using Fig.Web.Models.Compare;
using Fig.Web.Models.Setting;
using Fig.Web.Models.Setting.ConfigurationModels.DataGrid;
using Fig.Web.Services;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Web;

[TestFixture]
public class SettingCompareServiceTests
{
    private Mock<ISettingClientFacade> _settingClientFacade = null!;
    private Mock<IDataFacade> _dataFacade = null!;
    private SettingCompareService _sut = null!;

    [SetUp]
    public void Setup()
    {
        _settingClientFacade = new Mock<ISettingClientFacade>();
        _dataFacade = new Mock<IDataFacade>();

        _settingClientFacade.Setup(f => f.LoadAllClients()).Returns(Task.CompletedTask);
        _settingClientFacade.Setup(f => f.SettingClients).Returns(new List<SettingClientConfigurationModel>());

        // Default: bulk last-changed returns empty list
        _dataFacade.Setup(f => f.GetLastChangedForAllClientsSettings())
            .ReturnsAsync(new List<ClientSettingsLastChangedDataContract>());

        _sut = new SettingCompareService(_settingClientFacade.Object, _dataFacade.Object);
    }

    [Test]
    public async Task ShallReturnEmptyWhenExportHasNoClients()
    {
        var exportData = CreateExportData();

        var (rows, stats) = await _sut.CompareAsync(exportData);

        Assert.That(rows, Is.Empty);
        Assert.That(stats.TotalSettings, Is.EqualTo(0));
    }

    [Test]
    public async Task ShallDetectOnlyInExportSettings()
    {
        var exportData = CreateExportData(
            CreateExportClient("ClientA", null,
                CreateExportSetting("Setting1", "value1")));

        // No live clients
        _settingClientFacade.Setup(f => f.SettingClients)
            .Returns(new List<SettingClientConfigurationModel>());

        var (rows, stats) = await _sut.CompareAsync(exportData);

        Assert.That(rows.Count, Is.EqualTo(1));
        Assert.That(rows[0].Status, Is.EqualTo(CompareStatus.OnlyInExport));
        Assert.That(stats.OnlyInExportCount, Is.EqualTo(1));
    }

    [Test]
    public async Task ShallDetectOnlyInLiveSettings()
    {
        var exportData = CreateExportData(); // No clients in export

        var liveSetting = CreateMockSetting("Setting1", "liveVal");
        var liveClient = CreateMockClient("ClientA", null, new List<ISetting> { liveSetting });

        _settingClientFacade.Setup(f => f.SettingClients)
            .Returns(new List<SettingClientConfigurationModel> { liveClient });

        var (rows, stats) = await _sut.CompareAsync(exportData);

        Assert.That(rows.Count, Is.EqualTo(1));
        Assert.That(rows[0].Status, Is.EqualTo(CompareStatus.OnlyInLive));
        Assert.That(stats.OnlyInLiveCount, Is.EqualTo(1));
    }

    [Test]
    public async Task ShallDetectMatchingSettings()
    {
        var exportData = CreateExportData(
            CreateExportClient("ClientA", null,
                CreateExportSetting("Setting1", "sameValue")));

        var liveSetting = CreateMockSetting("Setting1", "sameValue");
        var liveClient = CreateMockClient("ClientA", null, new List<ISetting> { liveSetting });

        _settingClientFacade.Setup(f => f.SettingClients)
            .Returns(new List<SettingClientConfigurationModel> { liveClient });

        var (rows, stats) = await _sut.CompareAsync(exportData);

        Assert.That(rows.Count, Is.EqualTo(1));
        Assert.That(rows[0].Status, Is.EqualTo(CompareStatus.Match));
        Assert.That(stats.MatchCount, Is.EqualTo(1));
    }

    [Test]
    public async Task ShallDetectDifferentSettings()
    {
        var exportData = CreateExportData(
            CreateExportClient("ClientA", null,
                CreateExportSetting("Setting1", "exportValue")));

        var liveSetting = CreateMockSetting("Setting1", "liveValue");
        var liveClient = CreateMockClient("ClientA", null, new List<ISetting> { liveSetting });

        _settingClientFacade.Setup(f => f.SettingClients)
            .Returns(new List<SettingClientConfigurationModel> { liveClient });

        var (rows, stats) = await _sut.CompareAsync(exportData);

        Assert.That(rows.Count, Is.EqualTo(1));
        Assert.That(rows[0].Status, Is.EqualTo(CompareStatus.Different));
        Assert.That(rows[0].LiveValue, Is.EqualTo("liveValue"));
        Assert.That(rows[0].ExportValue, Is.EqualTo("exportValue"));
        Assert.That(stats.DifferenceCount, Is.EqualTo(1));
    }

    [Test]
    public async Task ShallMatchEnumDisplayValueAgainstExportValue()
    {
        var exportData = CreateExportData(
            CreateExportClient("ClientA", null,
                CreateExportSetting("Setting1", "1", valueType: typeof(TestEnum))));

        var liveSetting = CreateMockSetting("Setting1", "1 -> High");
        var liveClient = CreateMockClient("ClientA", null, new List<ISetting> { liveSetting });

        _settingClientFacade.Setup(f => f.SettingClients)
            .Returns(new List<SettingClientConfigurationModel> { liveClient });

        var (rows, _) = await _sut.CompareAsync(exportData);

        Assert.That(rows[0].Status, Is.EqualTo(CompareStatus.Match));
    }

    [Test]
    public async Task ShallIgnoreSecretPlaceholderInDataGridComparison()
    {
        var exportRows = new List<Dictionary<string, object?>>
        {
            new()
            {
                ["User"] = "alice",
                ["Password"] = SecretConstants.SecretPlaceholder
            }
        };

        var exportData = CreateExportData(
            CreateExportClient("ClientA", null,
                CreateExportDataGridSetting("Grid1", exportRows)));

        var liveRows = new List<Dictionary<string, object?>>
        {
            new()
            {
                ["User"] = "alice",
                ["Password"] = "real-password"
            }
        };

        var mockScriptRunner = Mock.Of<Fig.Common.NetStandard.Scripting.IScriptRunner>();
        var liveClient = new SettingClientConfigurationModel("ClientA", "desc", null, false, mockScriptRunner);
        var liveSetting = CreateDataGridSetting(liveClient, "Grid1", liveRows);
        liveClient.Settings = new List<ISetting> { liveSetting };

        _settingClientFacade.Setup(f => f.SettingClients)
            .Returns(new List<SettingClientConfigurationModel> { liveClient });

        var (rows, _) = await _sut.CompareAsync(exportData);

        Assert.That(rows[0].Status, Is.EqualTo(CompareStatus.Match));
    }

    [Test]
    public async Task ShallIncludeLastChangedMetadata()
    {
        var exportData = CreateExportData(
            CreateExportClient("ClientA", null,
                CreateExportSetting("Setting1", "val")));

        var liveSetting = CreateMockSetting("Setting1", "val");
        var liveClient = CreateMockClient("ClientA", null, new List<ISetting> { liveSetting });

        _settingClientFacade.Setup(f => f.SettingClients)
            .Returns(new List<SettingClientConfigurationModel> { liveClient });

        var changedAt = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var lastChangedEntries = new List<SettingValueDataContract>
        {
            new("Setting1", "val", changedAt, "user1", "some message")
        };

        _dataFacade.Setup(f => f.GetLastChangedForAllClientsSettings())
            .ReturnsAsync(new List<ClientSettingsLastChangedDataContract>
            {
                new("ClientA", null, lastChangedEntries)
            });

        var (rows, _) = await _sut.CompareAsync(exportData);

        Assert.That(rows[0].LastChangedBy, Is.EqualTo("user1"));
        Assert.That(rows[0].LastChangedAt, Is.EqualTo(changedAt));
        Assert.That(rows[0].LastChangeMessage, Is.EqualTo("some message"));
    }

    [Test]
    public async Task ShallCalculateStatisticsCorrectly()
    {
        var exportData = CreateExportData(
            CreateExportClient("ClientA", null,
                CreateExportSetting("Match1", "same"),
                CreateExportSetting("Diff1", "exportVal"),
                CreateExportSetting("OnlyExport1", "val"),
                CreateExportSetting("Secret1", "secretVal", isSecret: true)));

        var liveSetting1 = CreateMockSetting("Match1", "same");
        var liveSetting2 = CreateMockSetting("Diff1", "liveVal");
        var liveSetting3 = CreateMockSetting("OnlyLive1", "val3");
        var liveSetting4 = CreateMockSetting("Secret1", "**********", isSecret: true);
        var liveClient = CreateMockClient("ClientA", null,
            new List<ISetting> { liveSetting1, liveSetting2, liveSetting3, liveSetting4 });

        _settingClientFacade.Setup(f => f.SettingClients)
            .Returns(new List<SettingClientConfigurationModel> { liveClient });

        var (_, stats) = await _sut.CompareAsync(exportData);

        Assert.That(stats.TotalSettings, Is.EqualTo(5));
        Assert.That(stats.MatchCount, Is.EqualTo(1));
        Assert.That(stats.DifferenceCount, Is.EqualTo(1));
        Assert.That(stats.OnlyInExportCount, Is.EqualTo(1));
        Assert.That(stats.OnlyInLiveCount, Is.EqualTo(1));
        Assert.That(stats.NotComparedCount, Is.EqualTo(1));
    }

    [Test]
    public async Task ShallMarkDifferentSettingsAsApplicable()
    {
        var exportData = CreateExportData(
            CreateExportClient("ClientA", null,
                CreateExportSetting("Setting1", "exportValue")));

        var liveSetting = CreateMockSetting("Setting1", "liveValue");
        var liveClient = CreateMockClient("ClientA", null, new List<ISetting> { liveSetting });

        _settingClientFacade.Setup(f => f.SettingClients)
            .Returns(new List<SettingClientConfigurationModel> { liveClient });

        var (rows, _) = await _sut.CompareAsync(exportData);

        Assert.That(rows[0].IsApplicable, Is.True);
    }

    [Test]
    public async Task ShallHandleInstancedClients()
    {
        var exportData = CreateExportData(
            CreateExportClient("ClientA", "Inst1",
                CreateExportSetting("Setting1", "exportVal")));

        var liveSetting = CreateMockSetting("Setting1", "liveVal");
        var liveClient = CreateMockClient("ClientA", "Inst1", new List<ISetting> { liveSetting });

        _settingClientFacade.Setup(f => f.SettingClients)
            .Returns(new List<SettingClientConfigurationModel> { liveClient });

        var (rows, _) = await _sut.CompareAsync(exportData);

        Assert.That(rows.Count, Is.EqualTo(1));
        Assert.That(rows[0].Instance, Is.EqualTo("Inst1"));
        Assert.That(rows[0].ClientDisplayName, Is.EqualTo("ClientA [Inst1]"));
    }

    [Test]
    public async Task ShallMatchGroupManagedSetting()
    {
        var exportData = CreateExportData(
            CreateExportClient("ClientA", null,
                CreateExportSetting("Setting1", "sameValue")));

        var liveSetting = CreateMockSetting("Setting1", "sameValue", isGroupManaged: true);
        var liveClient = CreateMockClient("ClientA", null, new List<ISetting> { liveSetting });

        _settingClientFacade.Setup(f => f.SettingClients)
            .Returns(new List<SettingClientConfigurationModel> { liveClient });

        var (rows, stats) = await _sut.CompareAsync(exportData);

        Assert.That(rows.Count, Is.EqualTo(1));
        Assert.That(rows[0].Status, Is.EqualTo(CompareStatus.Match));
        Assert.That(stats.MatchCount, Is.EqualTo(1));
    }

    [Test]
    public async Task ShallDetectDifferentGroupManagedSetting()
    {
        var exportData = CreateExportData(
            CreateExportClient("ClientA", null,
                CreateExportSetting("Setting1", "exportValue")));

        var liveSetting = CreateMockSetting("Setting1", "liveValue", isGroupManaged: true);
        var liveClient = CreateMockClient("ClientA", null, new List<ISetting> { liveSetting });

        _settingClientFacade.Setup(f => f.SettingClients)
            .Returns(new List<SettingClientConfigurationModel> { liveClient });

        var (rows, stats) = await _sut.CompareAsync(exportData);

        Assert.That(rows.Count, Is.EqualTo(1));
        Assert.That(rows[0].Status, Is.EqualTo(CompareStatus.Different));
        Assert.That(stats.DifferenceCount, Is.EqualTo(1));
    }

    [Test]
    public async Task ShallDetectOnlyInLiveGroupManagedSetting()
    {
        var exportData = CreateExportData();

        var liveSetting = CreateMockSetting("Setting1", "liveVal", isGroupManaged: true);
        var liveClient = CreateMockClient("ClientA", null, new List<ISetting> { liveSetting });

        _settingClientFacade.Setup(f => f.SettingClients)
            .Returns(new List<SettingClientConfigurationModel> { liveClient });

        var (rows, stats) = await _sut.CompareAsync(exportData);

        Assert.That(rows.Count, Is.EqualTo(1));
        Assert.That(rows[0].Status, Is.EqualTo(CompareStatus.OnlyInLive));
        Assert.That(stats.OnlyInLiveCount, Is.EqualTo(1));
    }

    [Test]
    public async Task ShallMarkSecretSettingsAsNotCompared()
    {
        var exportData = CreateExportData(
            CreateExportClient("ClientA", null,
                CreateExportSetting("SecretSetting", "secretExportVal", isSecret: true)));

        var liveSetting = CreateMockSetting("SecretSetting", "**********", isSecret: true);
        var liveClient = CreateMockClient("ClientA", null, new List<ISetting> { liveSetting });

        _settingClientFacade.Setup(f => f.SettingClients)
            .Returns(new List<SettingClientConfigurationModel> { liveClient });

        var (rows, stats) = await _sut.CompareAsync(exportData);

        Assert.That(rows.Count, Is.EqualTo(1));
        Assert.That(rows[0].Status, Is.EqualTo(CompareStatus.NotCompared));
        Assert.That(rows[0].LiveValue, Is.Null);
        Assert.That(rows[0].ExportValue, Is.Null);
        Assert.That(rows[0].IsApplicable, Is.False);
        Assert.That(stats.NotComparedCount, Is.EqualTo(1));
    }

    [Test]
    public async Task ShallMarkAsNotComparedWhenOnlyExportIsSecret()
    {
        var exportData = CreateExportData(
            CreateExportClient("ClientA", null,
                CreateExportSetting("Setting1", "val", isSecret: true)));

        var liveSetting = CreateMockSetting("Setting1", "val", isSecret: false);
        var liveClient = CreateMockClient("ClientA", null, new List<ISetting> { liveSetting });

        _settingClientFacade.Setup(f => f.SettingClients)
            .Returns(new List<SettingClientConfigurationModel> { liveClient });

        var (rows, _) = await _sut.CompareAsync(exportData);

        Assert.That(rows[0].Status, Is.EqualTo(CompareStatus.NotCompared));
    }

    [Test]
    public async Task ShallIncludeExportChangeDetails()
    {
        var exportChangedAt = new DateTime(2025, 5, 15, 10, 0, 0, DateTimeKind.Utc);
        var exportSetting = CreateExportSetting("Setting1", "val");
        exportSetting.LastChangedDetails = new SettingLastChangedDataContract(
            "exportUser", exportChangedAt, "export message");

        var exportData = CreateExportData(
            CreateExportClient("ClientA", null, exportSetting));

        var liveSetting = CreateMockSetting("Setting1", "val");
        var liveClient = CreateMockClient("ClientA", null, new List<ISetting> { liveSetting });

        _settingClientFacade.Setup(f => f.SettingClients)
            .Returns(new List<SettingClientConfigurationModel> { liveClient });

        var (rows, _) = await _sut.CompareAsync(exportData);

        Assert.That(rows[0].ExportChangedBy, Is.EqualTo("exportUser"));
        Assert.That(rows[0].ExportChangedAt, Is.EqualTo(exportChangedAt));
        Assert.That(rows[0].ExportChangeMessage, Is.EqualTo("export message"));
    }

    #region Value-Only Export Tests

    [Test]
    public async Task ValueOnly_ShallReturnEmptyWhenExportHasNoClients()
    {
        var exportData = CreateValueOnlyExportData();

        var (rows, stats) = await _sut.CompareAsync(exportData);

        Assert.That(rows, Is.Empty);
        Assert.That(stats.TotalSettings, Is.EqualTo(0));
    }

    [Test]
    public async Task ValueOnly_ShallDetectMatchingSettings()
    {
        var exportData = CreateValueOnlyExportData(
            CreateValueOnlyExportClient("ClientA", null,
                CreateValueOnlyExportSetting("Setting1", "sameValue")));

        var liveSetting = CreateMockSetting("Setting1", "sameValue");
        var liveClient = CreateMockClient("ClientA", null, new List<ISetting> { liveSetting });

        _settingClientFacade.Setup(f => f.SettingClients)
            .Returns(new List<SettingClientConfigurationModel> { liveClient });

        var (rows, stats) = await _sut.CompareAsync(exportData);

        Assert.That(rows.Count, Is.EqualTo(1));
        Assert.That(rows[0].Status, Is.EqualTo(CompareStatus.Match));
        Assert.That(stats.MatchCount, Is.EqualTo(1));
    }

    [Test]
    public async Task ValueOnly_ShallDetectDifferentSettings()
    {
        var exportData = CreateValueOnlyExportData(
            CreateValueOnlyExportClient("ClientA", null,
                CreateValueOnlyExportSetting("Setting1", "exportValue")));

        var liveSetting = CreateMockSetting("Setting1", "liveValue");
        var liveClient = CreateMockClient("ClientA", null, new List<ISetting> { liveSetting });

        _settingClientFacade.Setup(f => f.SettingClients)
            .Returns(new List<SettingClientConfigurationModel> { liveClient });

        var (rows, stats) = await _sut.CompareAsync(exportData);

        Assert.That(rows.Count, Is.EqualTo(1));
        Assert.That(rows[0].Status, Is.EqualTo(CompareStatus.Different));
        Assert.That(rows[0].LiveValue, Is.EqualTo("liveValue"));
        Assert.That(rows[0].ExportValue, Is.EqualTo("exportValue"));
        Assert.That(stats.DifferenceCount, Is.EqualTo(1));
    }

    [Test]
    public async Task ValueOnly_ShallDetectOnlyInExportSettings()
    {
        var exportData = CreateValueOnlyExportData(
            CreateValueOnlyExportClient("ClientA", null,
                CreateValueOnlyExportSetting("Setting1", "value1")));

        _settingClientFacade.Setup(f => f.SettingClients)
            .Returns(new List<SettingClientConfigurationModel>());

        var (rows, stats) = await _sut.CompareAsync(exportData);

        Assert.That(rows.Count, Is.EqualTo(1));
        Assert.That(rows[0].Status, Is.EqualTo(CompareStatus.OnlyInExport));
        Assert.That(stats.OnlyInExportCount, Is.EqualTo(1));
    }

    [Test]
    public async Task ValueOnly_ShallDetectOnlyInLiveSettings()
    {
        var exportData = CreateValueOnlyExportData();

        var liveSetting = CreateMockSetting("Setting1", "liveVal");
        var liveClient = CreateMockClient("ClientA", null, new List<ISetting> { liveSetting });

        _settingClientFacade.Setup(f => f.SettingClients)
            .Returns(new List<SettingClientConfigurationModel> { liveClient });

        var (rows, stats) = await _sut.CompareAsync(exportData);

        Assert.That(rows.Count, Is.EqualTo(1));
        Assert.That(rows[0].Status, Is.EqualTo(CompareStatus.OnlyInLive));
        Assert.That(stats.OnlyInLiveCount, Is.EqualTo(1));
    }

    [Test]
    public async Task ValueOnly_ShallMarkEncryptedSettingsAsNotCompared()
    {
        var exportData = CreateValueOnlyExportData(
            CreateValueOnlyExportClient("ClientA", null,
                CreateValueOnlyExportSetting("EncryptedSetting", "encryptedVal", isEncrypted: true)));

        var liveSetting = CreateMockSetting("EncryptedSetting", "liveVal");
        var liveClient = CreateMockClient("ClientA", null, new List<ISetting> { liveSetting });

        _settingClientFacade.Setup(f => f.SettingClients)
            .Returns(new List<SettingClientConfigurationModel> { liveClient });

        var (rows, stats) = await _sut.CompareAsync(exportData);

        Assert.That(rows.Count, Is.EqualTo(1));
        Assert.That(rows[0].Status, Is.EqualTo(CompareStatus.NotCompared));
        Assert.That(stats.NotComparedCount, Is.EqualTo(1));
    }

    [Test]
    public async Task ValueOnly_ShallIncludeLastChangedDetails()
    {
        var exportChangedAt = new DateTime(2025, 5, 15, 10, 0, 0, DateTimeKind.Utc);
        var exportSetting = CreateValueOnlyExportSetting("Setting1", "val");
        exportSetting.LastChangedDetails = new SettingLastChangedDataContract(
            "exportUser", exportChangedAt, "export message");

        var exportData = CreateValueOnlyExportData(
            CreateValueOnlyExportClient("ClientA", null, exportSetting));

        var liveSetting = CreateMockSetting("Setting1", "val");
        var liveClient = CreateMockClient("ClientA", null, new List<ISetting> { liveSetting });

        _settingClientFacade.Setup(f => f.SettingClients)
            .Returns(new List<SettingClientConfigurationModel> { liveClient });

        var (rows, _) = await _sut.CompareAsync(exportData);

        Assert.That(rows[0].ExportChangedBy, Is.EqualTo("exportUser"));
        Assert.That(rows[0].ExportChangedAt, Is.EqualTo(exportChangedAt));
        Assert.That(rows[0].ExportChangeMessage, Is.EqualTo("export message"));
    }

    [Test]
    public async Task ValueOnly_ShallCalculateStatisticsCorrectly()
    {
        var exportData = CreateValueOnlyExportData(
            CreateValueOnlyExportClient("ClientA", null,
                CreateValueOnlyExportSetting("Match1", "same"),
                CreateValueOnlyExportSetting("Diff1", "exportVal"),
                CreateValueOnlyExportSetting("OnlyExport1", "val"),
                CreateValueOnlyExportSetting("Encrypted1", "encVal", isEncrypted: true)));

        var liveSetting1 = CreateMockSetting("Match1", "same");
        var liveSetting2 = CreateMockSetting("Diff1", "liveVal");
        var liveSetting3 = CreateMockSetting("OnlyLive1", "val3");
        var liveSetting4 = CreateMockSetting("Encrypted1", "liveEncVal");
        var liveClient = CreateMockClient("ClientA", null,
            new List<ISetting> { liveSetting1, liveSetting2, liveSetting3, liveSetting4 });

        _settingClientFacade.Setup(f => f.SettingClients)
            .Returns(new List<SettingClientConfigurationModel> { liveClient });

        var (_, stats) = await _sut.CompareAsync(exportData);

        Assert.That(stats.TotalSettings, Is.EqualTo(5));
        Assert.That(stats.MatchCount, Is.EqualTo(1));
        Assert.That(stats.DifferenceCount, Is.EqualTo(1));
        Assert.That(stats.OnlyInExportCount, Is.EqualTo(1));
        Assert.That(stats.OnlyInLiveCount, Is.EqualTo(1));
        Assert.That(stats.NotComparedCount, Is.EqualTo(1));
    }

    [Test]
    public async Task ValueOnly_ShallHandleInstancedClients()
    {
        var exportData = CreateValueOnlyExportData(
            CreateValueOnlyExportClient("ClientA", "Inst1",
                CreateValueOnlyExportSetting("Setting1", "exportVal")));

        var liveSetting = CreateMockSetting("Setting1", "liveVal");
        var liveClient = CreateMockClient("ClientA", "Inst1", new List<ISetting> { liveSetting });

        _settingClientFacade.Setup(f => f.SettingClients)
            .Returns(new List<SettingClientConfigurationModel> { liveClient });

        var (rows, _) = await _sut.CompareAsync(exportData);

        Assert.That(rows.Count, Is.EqualTo(1));
        Assert.That(rows[0].Instance, Is.EqualTo("Inst1"));
        Assert.That(rows[0].ClientDisplayName, Is.EqualTo("ClientA [Inst1]"));
    }

    #endregion

    #region Helpers

    private static FigDataExportDataContract CreateExportData(
        params SettingClientExportDataContract[] clients)
    {
        return new FigDataExportDataContract(
            DateTime.UtcNow,
            ImportType.ClearAndImport,
            1,
            clients.ToList());
    }

    private static SettingClientExportDataContract CreateExportClient(
        string name, string? instance, params SettingExportDataContract[] settings)
    {
        return new SettingClientExportDataContract(
            name,
            "description",
            "secret",
            instance,
            settings.ToList());
    }

    private static SettingExportDataContract CreateExportSetting(
        string name,
        string value,
        bool isSecret = false,
        Type? valueType = null)
    {
        return new SettingExportDataContract(
            name: name,
            description: "desc",
            isSecret: isSecret,
            valueType: valueType ?? typeof(string),
            value: new StringSettingDataContract(value),
            defaultValue: null,
            isEncrypted: false,
            jsonSchema: null,
            validationRegex: null,
            validationExplanation: null,
            validValues: null,
            group: null,
            displayOrder: null,
            advanced: false,
            lookupTableKey: null,
            editorLineCount: null,
            dataGridDefinitionJson: null,
            enablesSettings: null,
            supportsLiveUpdate: false,
            lastChanged: null,
            categoryColor: null,
            categoryName: null,
            displayScript: null,
            isExternallyManaged: false,
            classification: Classification.Technical,
            environmentSpecific: null,
            lookupKeySettingName: null,
            indent: null);
    }

    private static SettingExportDataContract CreateExportDataGridSetting(
        string name,
        List<Dictionary<string, object?>> rows)
    {
        return new SettingExportDataContract(
            name: name,
            description: "desc",
            isSecret: false,
            valueType: typeof(List<Dictionary<string, object?>>),
            value: new DataGridSettingDataContract(rows),
            defaultValue: null,
            isEncrypted: false,
            jsonSchema: null,
            validationRegex: null,
            validationExplanation: null,
            validValues: null,
            group: null,
            displayOrder: null,
            advanced: false,
            lookupTableKey: null,
            editorLineCount: null,
            dataGridDefinitionJson: null,
            enablesSettings: null,
            supportsLiveUpdate: false,
            lastChanged: null,
            categoryColor: null,
            categoryName: null,
            displayScript: null,
            isExternallyManaged: false,
            classification: Classification.Technical,
            environmentSpecific: null,
            lookupKeySettingName: null,
            indent: null);
    }

    private static SettingClientConfigurationModel CreateMockClient(
        string name, string? instance, List<ISetting> settings)
    {
        var mockScriptRunner = Mock.Of<Fig.Common.NetStandard.Scripting.IScriptRunner>();
        var client = new SettingClientConfigurationModel(name, "desc", instance, false, mockScriptRunner);
        client.Settings = settings;
        return client;
    }

    private static ISetting CreateMockSetting(
        string name,
        string value,
        bool isSecret = false,
        bool isGroupManaged = false)
    {
        var mock = new Mock<ISetting>();
        mock.SetupGet(s => s.Name).Returns(name);
        mock.SetupGet(s => s.IsGroupManaged).Returns(isGroupManaged);
        mock.SetupGet(s => s.IsSecret).Returns(isSecret);
        mock.Setup(s => s.GetStringValue(It.IsAny<int>())).Returns(value);
        return mock.Object;
    }

    private static DataGridSettingConfigurationModel CreateDataGridSetting(
        SettingClientConfigurationModel client,
        string name,
        List<Dictionary<string, object?>> rows)
    {
        var columns = new List<DataGridColumnDataContract>
        {
            new("User", typeof(string)),
            new("Password", typeof(string), isSecret: true)
        };

        var definition = new DataGridDefinitionDataContract(columns, false);
        var valueContract = new DataGridSettingDataContract(rows);
        var dataContract = new SettingDefinitionDataContract(
            name: name,
            description: "desc",
            value: valueContract,
            valueType: typeof(List<Dictionary<string, object?>>),
            dataGridDefinition: definition);

        return new DataGridSettingConfigurationModel(dataContract, client, new SettingPresentation(false));
    }

    private enum TestEnum
    {
        Low = 0,
        High = 1
    }

    private static FigValueOnlyDataExportDataContract CreateValueOnlyExportData(
        params SettingClientValueExportDataContract[] clients)
    {
        return new FigValueOnlyDataExportDataContract(
            DateTime.UtcNow,
            ImportType.UpdateValues,
            1,
            null,
            clients.ToList());
    }

    private static SettingClientValueExportDataContract CreateValueOnlyExportClient(
        string name, string? instance, params SettingValueExportDataContract[] settings)
    {
        return new SettingClientValueExportDataContract(name, instance, settings.ToList());
    }

    private static SettingValueExportDataContract CreateValueOnlyExportSetting(
        string name, object? value, bool isEncrypted = false)
    {
        return new SettingValueExportDataContract(name, value, isEncrypted, null);
    }

    #endregion
}
