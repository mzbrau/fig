using Fig.Api;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Services;
using Fig.Client.Abstractions.Data;
using Fig.Contracts.Authentication;
using Fig.Contracts.SettingGroups;
using Fig.Datalayer.BusinessEntities;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class SettingGroupServiceTests
{
    private Mock<ISettingGroupRepository> _settingGroupRepository = null!;
    private Mock<ISettingClientRepository> _settingClientRepository = null!;
    private Mock<IEventLogRepository> _eventLogRepository = null!;
    private Mock<IEventLogFactory> _eventLogFactory = null!;
    private SettingGroupService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _settingGroupRepository = new Mock<ISettingGroupRepository>();
        _settingClientRepository = new Mock<ISettingClientRepository>();
        _eventLogRepository = new Mock<IEventLogRepository>();
        _eventLogFactory = new Mock<IEventLogFactory>();

        _eventLogFactory
            .Setup(f => f.GroupCreated(It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(new EventLogBusinessEntity());
        _eventLogFactory
            .Setup(f => f.GroupUpdated(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(new EventLogBusinessEntity());
        _eventLogFactory
            .Setup(f => f.GroupDeleted(It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(new EventLogBusinessEntity());

        _sut = new SettingGroupService(
            _settingGroupRepository.Object,
            _settingClientRepository.Object,
            _eventLogRepository.Object,
            _eventLogFactory.Object,
            Mock.Of<ILogger<SettingGroupService>>());

        _sut.SetAuthenticatedUser(CreateUser(".*", Enum.GetValues<Classification>().ToList()));
    }

    [Test]
    public void GetLeafName_ReturnsTrimmedName_WhenNoDelimiter()
    {
        Assert.That(SettingGroupService.GetLeafName("  MySetting  "), Is.EqualTo("MySetting"));
    }

    [Test]
    public void GetLeafName_ReturnsLastSegment_WhenDelimited()
    {
        Assert.That(SettingGroupService.GetLeafName("Parent->Child->Leaf"), Is.EqualTo("Leaf"));
    }

    [Test]
    public void GetLeafName_ReturnsEmpty_WhenNullOrWhitespace()
    {
        Assert.That(SettingGroupService.GetLeafName(null!), Is.EqualTo(string.Empty));
        Assert.That(SettingGroupService.GetLeafName("   "), Is.EqualTo(string.Empty));
    }

    [Test]
    public void ValidateGroupedSettings_AllowsNullGroupedSettings()
    {
        var group = new SettingGroupDataContract(null, "Group", null, null!);

        Assert.DoesNotThrow(() => SettingGroupService.ValidateGroupedSettings(group));
    }

    [Test]
    public void ValidateGroupedSettings_Throws_WhenGroupedSettingNameEmpty()
    {
        var group = CreateGroupContract("Group",
        [
            new GroupedSettingDataContract("  ", null, "String",
                [new SourceSettingDataContract("Client", "Setting")])
        ]);

        Assert.That(
            () => SettingGroupService.ValidateGroupedSettings(group),
            Throws.ArgumentException.With.Message.Contain("Grouped setting name"));
    }

    [Test]
    public void ValidateGroupedSettings_Throws_WhenNoSourceSettings()
    {
        var group = CreateGroupContract("Group",
        [
            new GroupedSettingDataContract("Setting", null, "String", [])
        ]);

        Assert.That(
            () => SettingGroupService.ValidateGroupedSettings(group),
            Throws.ArgumentException.With.Message.Contain("at least one source setting"));
    }

    [Test]
    public void ValidateGroupedSettings_Throws_WhenSourceClientOrSettingEmpty()
    {
        var missingClient = CreateGroupContract("Group",
        [
            new GroupedSettingDataContract("Setting", null, "String",
                [new SourceSettingDataContract(" ", "SettingA")])
        ]);
        var missingSetting = CreateGroupContract("Group",
        [
            new GroupedSettingDataContract("Setting", null, "String",
                [new SourceSettingDataContract("Client", "")])
        ]);

        Assert.That(
            () => SettingGroupService.ValidateGroupedSettings(missingClient),
            Throws.ArgumentException.With.Message.Contain("client name"));
        Assert.That(
            () => SettingGroupService.ValidateGroupedSettings(missingSetting),
            Throws.ArgumentException.With.Message.Contain("Source setting name"));
    }

    [Test]
    public async Task CreateGroup_PersistsValidGroup()
    {
        var groupId = Guid.NewGuid();
        _settingGroupRepository.Setup(r => r.GetGroupByName("Valid")).ReturnsAsync((SettingGroupBusinessEntity?)null);
        _settingGroupRepository.Setup(r => r.AddGroup(It.IsAny<SettingGroupBusinessEntity>())).ReturnsAsync(groupId);

        var group = CreateGroupContract("Valid",
        [
            new GroupedSettingDataContract("Timeout", null, "Int32",
                [new SourceSettingDataContract("Api", "Timeout")])
        ]);

        var result = await _sut.CreateGroup(group);

        Assert.That(result.Id, Is.EqualTo(groupId));
        _settingGroupRepository.Verify(r => r.AddGroup(It.Is<SettingGroupBusinessEntity>(e => e.Name == "Valid")), Times.Once);
        _eventLogRepository.Verify(r => r.Add(It.IsAny<EventLogBusinessEntity>()), Times.Once);
    }

    [Test]
    public async Task RemoveClientFromGroups_RemovesClientReferencesAndDeletesEmptyGroups()
    {
        var keepGroup = CreateGroupEntity("Keep",
        [
            new GroupedSettingDataContract("Shared", null, "String",
            [
                new SourceSettingDataContract("ClientA", "Shared"),
                new SourceSettingDataContract("ClientB", "Shared")
            ])
        ]);
        var emptyAfterRemoval = CreateGroupEntity("DeleteMe",
        [
            new GroupedSettingDataContract("OnlyA", null, "String",
                [new SourceSettingDataContract("ClientA", "OnlyA")])
        ]);

        _settingGroupRepository.Setup(r => r.GetAllGroups())
            .ReturnsAsync(new List<SettingGroupBusinessEntity> { keepGroup, emptyAfterRemoval });

        var updates = new List<SettingGroupBusinessEntity>();
        _settingGroupRepository
            .Setup(r => r.UpdateGroup(It.IsAny<SettingGroupBusinessEntity>()))
            .Callback<SettingGroupBusinessEntity>(e => updates.Add(e))
            .Returns(Task.CompletedTask);

        await _sut.RemoveClientFromGroups("ClientA");

        var updated = updates.Single(u => u.Name == "Keep");
        Assert.That(Deserialize(updated.GroupSettingsJson).Single().SourceSettings.Single().ClientName,
            Is.EqualTo("ClientB"));
        _settingGroupRepository.Verify(r => r.DeleteGroup(emptyAfterRemoval), Times.Once);
    }

    [Test]
    public async Task ValidateClientRegistrationGroups_RemovesOrphanedSourceSettings()
    {
        var group = CreateGroupEntity("Group",
        [
            new GroupedSettingDataContract("Timeout", null, "Int32",
            [
                new SourceSettingDataContract("Api", "Timeout"),
                new SourceSettingDataContract("Api", "RemovedSetting"),
                new SourceSettingDataContract("Other", "RemovedSetting")
            ])
        ]);

        _settingGroupRepository.Setup(r => r.GetAllGroups())
            .ReturnsAsync(new List<SettingGroupBusinessEntity> { group });

        SettingGroupBusinessEntity? updated = null;
        _settingGroupRepository
            .Setup(r => r.UpdateGroup(It.IsAny<SettingGroupBusinessEntity>()))
            .Callback<SettingGroupBusinessEntity>(e => updated = e)
            .Returns(Task.CompletedTask);

        await _sut.ValidateClientRegistrationGroups("Api", ["Timeout"]);

        Assert.That(updated, Is.Not.Null);
        var settings = Deserialize(updated!.GroupSettingsJson).Single();
        Assert.That(settings.SourceSettings, Has.Count.EqualTo(2));
        Assert.That(settings.SourceSettings.Any(s => s.ClientName == "Api" && s.SettingName == "Timeout"), Is.True);
        Assert.That(settings.SourceSettings.Any(s => s.ClientName == "Other" && s.SettingName == "RemovedSetting"), Is.True);
    }

    [Test]
    public async Task HandleInitialRegistrationGroups_MatchesByLeafNameAndCreatesMissingGroup()
    {
        var existing = CreateGroupEntity("Existing",
        [
            new GroupedSettingDataContract("DisplayName", null, "String",
                [new SourceSettingDataContract("Other", "Parent->Leaf")])
        ]);

        _settingGroupRepository.Setup(r => r.GetAllGroups())
            .ReturnsAsync(new List<SettingGroupBusinessEntity> { existing });
        _settingGroupRepository.Setup(r => r.AddGroup(It.IsAny<SettingGroupBusinessEntity>()))
            .ReturnsAsync(Guid.NewGuid());

        SettingGroupBusinessEntity? updated = null;
        _settingGroupRepository
            .Setup(r => r.UpdateGroup(It.IsAny<SettingGroupBusinessEntity>()))
            .Callback<SettingGroupBusinessEntity>(e => updated = e)
            .Returns(Task.CompletedTask);

        await _sut.HandleInitialRegistrationGroups("Api",
        [
            ("Parent->Leaf", "Existing", "String"),
            ("BrandNew", "BrandNewGroup", "Int32")
        ]);

        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.Name, Is.EqualTo("Existing"));
        Assert.That(
            Deserialize(updated.GroupSettingsJson).Single().SourceSettings
                .Any(s => s.ClientName == "Api" && s.SettingName == "Parent->Leaf"),
            Is.True);
        _settingGroupRepository.Verify(r => r.AddGroup(It.Is<SettingGroupBusinessEntity>(e =>
            e.Name == "BrandNewGroup")), Times.Once);
    }

    [Test]
    public async Task GetAllGroups_FiltersInaccessibleClassificationsAndClientRegex()
    {
        _sut.SetAuthenticatedUser(CreateUser("^Allowed", [Classification.Technical]));

        var group = CreateGroupEntity("Mixed",
        [
            new GroupedSettingDataContract("Mixed", null, "String",
            [
                new SourceSettingDataContract("AllowedClient", "TechSetting"),
                new SourceSettingDataContract("AllowedClient", "SpecialSetting"),
                new SourceSettingDataContract("DeniedClient", "TechSetting")
            ])
        ]);

        _settingGroupRepository.Setup(r => r.GetAllGroups())
            .ReturnsAsync(new List<SettingGroupBusinessEntity> { group });
        _settingClientRepository
            .Setup(r => r.GetAllClients(It.IsAny<UserDataContract>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<SettingClientBusinessEntity>
            {
                new()
                {
                    Name = "AllowedClient",
                    Settings =
                    [
                        new SettingBusinessEntity { Name = "TechSetting", Classification = Classification.Technical },
                        new SettingBusinessEntity { Name = "SpecialSetting", Classification = Classification.Special }
                    ]
                }
            });

        var groups = (await _sut.GetAllGroups()).ToList();

        Assert.That(groups, Has.Count.EqualTo(1));
        Assert.That(groups[0].GroupedSettings.Single().SourceSettings.Select(s => s.SettingName),
            Is.EquivalentTo(new[] { "TechSetting" }));
        Assert.That(groups[0].GroupedSettings.Single().SourceSettings.Select(s => s.ClientName),
            Is.All.EqualTo("AllowedClient"));
    }

    [Test]
    public async Task GetGroup_Throws_WhenAllSourcesAreFilteredOut()
    {
        _sut.SetAuthenticatedUser(CreateUser(".*", [Classification.Technical]));

        var id = Guid.NewGuid();
        var entity = new SettingGroupBusinessEntity
        {
            Id = id,
            Name = "Hidden",
            GroupSettingsJson = JsonConvert.SerializeObject(new List<GroupedSettingDataContract>
            {
                new("Secret", null, "String",
                    [new SourceSettingDataContract("Client", "SpecialSetting")])
            })
        };

        _settingGroupRepository.Setup(r => r.GetGroup(id, false)).ReturnsAsync(entity);
        _settingClientRepository
            .Setup(r => r.GetAllClients(It.IsAny<UserDataContract>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<SettingClientBusinessEntity>
            {
                new()
                {
                    Name = "Client",
                    Settings =
                    [
                        new SettingBusinessEntity { Name = "SpecialSetting", Classification = Classification.Special }
                    ]
                }
            });

        Assert.That(async () => await _sut.GetGroup(id), Throws.TypeOf<KeyNotFoundException>());
    }

    private static UserDataContract CreateUser(string clientFilter, List<Classification> classifications) =>
        new(Guid.NewGuid(), "tester", "Test", "User", Role.Administrator, clientFilter, classifications);

    private static SettingGroupDataContract CreateGroupContract(string name, List<GroupedSettingDataContract> settings) =>
        new(null, name, null, settings);

    private static SettingGroupBusinessEntity CreateGroupEntity(string name, List<GroupedSettingDataContract> settings) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            GroupSettingsJson = JsonConvert.SerializeObject(settings),
            CreatedAt = DateTime.UtcNow,
            LastModifiedAt = DateTime.UtcNow
        };

    private static List<GroupedSettingDataContract> Deserialize(string json) =>
        JsonConvert.DeserializeObject<List<GroupedSettingDataContract>>(json) ?? [];
}
