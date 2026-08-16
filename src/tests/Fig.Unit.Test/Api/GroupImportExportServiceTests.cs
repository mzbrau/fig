using Fig.Api;
using Fig.Api.DataImport;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Services;
using Fig.Client.Abstractions.Data;
using Fig.Contracts.Authentication;
using Fig.Contracts.ImportExport;
using Fig.Contracts.SettingGroups;
using Fig.Datalayer.BusinessEntities;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class GroupImportExportServiceTests
{
    private Mock<ISettingGroupRepository> _settingGroupRepository = null!;
    private Mock<IEventLogRepository> _eventLogRepository = null!;
    private Mock<IEventLogFactory> _eventLogFactory = null!;
    private GroupImportExportService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _settingGroupRepository = new Mock<ISettingGroupRepository>();
        _eventLogRepository = new Mock<IEventLogRepository>();
        _eventLogFactory = new Mock<IEventLogFactory>();

        _eventLogFactory
            .Setup(f => f.DataExported(It.IsAny<UserDataContract?>()))
            .Returns(new EventLogBusinessEntity());
        _eventLogFactory
            .Setup(f => f.DataImported(
                It.IsAny<ImportType>(),
                It.IsAny<ImportMode>(),
                It.IsAny<int>(),
                It.IsAny<UserDataContract?>()))
            .Returns(new EventLogBusinessEntity());

        _sut = new GroupImportExportService(
            _settingGroupRepository.Object,
            _eventLogRepository.Object,
            _eventLogFactory.Object,
            Mock.Of<ILogger<GroupImportExportService>>());

        _sut.SetAuthenticatedUser(new UserDataContract(
            Guid.NewGuid(),
            "importer",
            "Import",
            "User",
            Role.Administrator,
            ".*",
            Enum.GetValues<Classification>().ToList()));
    }

    [Test]
    public void ValidateImport_Throws_WhenGroupsNull()
    {
        var data = new SettingGroupExportDataContract(DateTime.UtcNow, 1, null!);

        Assert.That(
            () => GroupImportExportService.ValidateImport(data, ImportType.AddNew),
            Throws.ArgumentException.With.Message.Contain("non-null groups"));
    }

    [TestCase(ImportType.UpdateValues)]
    [TestCase(ImportType.UpdateValuesInitOnly)]
    public void ValidateImport_Throws_ForUnsupportedImportType(ImportType importType)
    {
        var data = CreateExport([CreateGroup("A")]);

        Assert.That(
            () => GroupImportExportService.ValidateImport(data, importType),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void ValidateImport_Throws_WhenGroupNameEmpty()
    {
        var data = CreateExport([CreateGroup("  ")]);

        Assert.That(
            () => GroupImportExportService.ValidateImport(data, ImportType.ReplaceExisting),
            Throws.ArgumentException.With.Message.Contain("non-empty name"));
    }

    [TestCase(ImportType.ClearAndImport)]
    [TestCase(ImportType.AddNew)]
    [TestCase(ImportType.ReplaceExisting)]
    public void ValidateImport_AllowsSupportedImportTypes(ImportType importType)
    {
        var data = CreateExport([CreateGroup("Valid")]);

        Assert.DoesNotThrow(() => GroupImportExportService.ValidateImport(data, importType));
    }

    [Test]
    public async Task ImportGroups_ClearAndImport_DeletesExistingThenCreatesAll()
    {
        var existing = new SettingGroupBusinessEntity { Id = Guid.NewGuid(), Name = "Old" };
        _settingGroupRepository.Setup(r => r.GetAllGroups())
            .ReturnsAsync(new List<SettingGroupBusinessEntity> { existing });
        _settingGroupRepository.Setup(r => r.AddGroup(It.IsAny<SettingGroupBusinessEntity>()))
            .ReturnsAsync(Guid.NewGuid());

        var result = await _sut.ImportGroups(
            CreateExport([CreateGroup("NewA"), CreateGroup("NewB")]),
            ImportType.ClearAndImport);

        Assert.That(result.ErrorMessage, Is.Null);
        _settingGroupRepository.Verify(r => r.DeleteGroup(existing), Times.Once);
        _settingGroupRepository.Verify(r => r.AddGroup(It.IsAny<SettingGroupBusinessEntity>()), Times.Exactly(2));
    }

    [Test]
    public async Task ImportGroups_AddNew_SkipsExistingNames()
    {
        _settingGroupRepository.Setup(r => r.GetGroupByName("Existing"))
            .ReturnsAsync(new SettingGroupBusinessEntity { Name = "Existing" });
        _settingGroupRepository.Setup(r => r.GetGroupByName("BrandNew"))
            .ReturnsAsync((SettingGroupBusinessEntity?)null);
        _settingGroupRepository.Setup(r => r.AddGroup(It.IsAny<SettingGroupBusinessEntity>()))
            .ReturnsAsync(Guid.NewGuid());

        var result = await _sut.ImportGroups(
            CreateExport([CreateGroup("Existing"), CreateGroup("BrandNew")]),
            ImportType.AddNew);

        Assert.That(result.ErrorMessage, Is.Null);
        _settingGroupRepository.Verify(r => r.AddGroup(It.Is<SettingGroupBusinessEntity>(e => e.Name == "BrandNew")), Times.Once);
        _settingGroupRepository.Verify(r => r.AddGroup(It.Is<SettingGroupBusinessEntity>(e => e.Name == "Existing")), Times.Never);
        _settingGroupRepository.Verify(r => r.UpdateGroup(It.IsAny<SettingGroupBusinessEntity>()), Times.Never);
        _settingGroupRepository.Verify(r => r.DeleteGroup(It.IsAny<SettingGroupBusinessEntity>()), Times.Never);
    }

    [Test]
    public async Task ImportGroups_ReplaceExisting_UpdatesMatchingAndCreatesMissing()
    {
        var existing = new SettingGroupBusinessEntity
        {
            Id = Guid.NewGuid(),
            Name = "Existing",
            Description = "old",
            GroupSettingsJson = "[]"
        };

        _settingGroupRepository.Setup(r => r.GetGroupByName("Existing")).ReturnsAsync(existing);
        _settingGroupRepository.Setup(r => r.GetGroupByName("BrandNew")).ReturnsAsync((SettingGroupBusinessEntity?)null);
        _settingGroupRepository.Setup(r => r.AddGroup(It.IsAny<SettingGroupBusinessEntity>()))
            .ReturnsAsync(Guid.NewGuid());

        var replacement = CreateGroup("Existing", "updated",
        [
            new GroupedSettingDataContract("Timeout", null, "Int32",
                [new SourceSettingDataContract("Api", "Timeout")])
        ]);

        SettingGroupBusinessEntity? updated = null;
        _settingGroupRepository
            .Setup(r => r.UpdateGroup(It.IsAny<SettingGroupBusinessEntity>()))
            .Callback<SettingGroupBusinessEntity>(e => updated = e)
            .Returns(Task.CompletedTask);

        var result = await _sut.ImportGroups(
            CreateExport([replacement, CreateGroup("BrandNew")]),
            ImportType.ReplaceExisting);

        Assert.That(result.ErrorMessage, Is.Null);
        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.Name, Is.EqualTo("Existing"));
        Assert.That(updated.Description, Is.EqualTo("updated"));
        Assert.That(Deserialize(updated.GroupSettingsJson).Single().Name, Is.EqualTo("Timeout"));
        _settingGroupRepository.Verify(r => r.AddGroup(It.Is<SettingGroupBusinessEntity>(e => e.Name == "BrandNew")), Times.Once);
        _settingGroupRepository.Verify(r => r.DeleteGroup(It.IsAny<SettingGroupBusinessEntity>()), Times.Never);
    }

    [Test]
    public async Task ImportGroups_ReturnsErrorMessage_WhenRepositoryThrows()
    {
        _settingGroupRepository.Setup(r => r.GetAllGroups())
            .ThrowsAsync(new InvalidOperationException("db down"));

        var result = await _sut.ImportGroups(CreateExport([CreateGroup("A")]), ImportType.ClearAndImport);

        Assert.That(result.ErrorMessage, Is.EqualTo("db down"));
    }

    [Test]
    public async Task ExportGroups_ReturnsAllGroupsAndLogsExport()
    {
        _settingGroupRepository.Setup(r => r.GetAllGroups())
            .ReturnsAsync(new List<SettingGroupBusinessEntity>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "G1",
                    Description = "d",
                    GroupSettingsJson = "[]"
                }
            });

        var result = await _sut.ExportGroups();

        Assert.That(result.Groups, Has.Count.EqualTo(1));
        Assert.That(result.Groups[0].Name, Is.EqualTo("G1"));
        _eventLogRepository.Verify(r => r.Add(It.IsAny<EventLogBusinessEntity>()), Times.Once);
    }

    private static SettingGroupExportDataContract CreateExport(List<SettingGroupDataContract> groups) =>
        new(DateTime.UtcNow, 1, groups);

    private static SettingGroupDataContract CreateGroup(
        string name,
        string? description = null,
        List<GroupedSettingDataContract>? settings = null) =>
        new(null, name, description, settings ?? []);

    private static List<GroupedSettingDataContract> Deserialize(string json) =>
        JsonConvert.DeserializeObject<List<GroupedSettingDataContract>>(json) ?? [];
}
