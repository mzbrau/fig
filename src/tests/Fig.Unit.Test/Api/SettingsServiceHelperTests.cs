using Fig.Api;
using Fig.Api.Converters;
using Fig.Api.DataImport;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Secrets;
using Fig.Api.Services;
using Fig.Api.Utils;
using Fig.Api.Validators;
using Fig.Client.Abstractions.Data;
using Fig.Common.Events;
using Fig.Contracts.Authentication;
using Fig.Contracts.Settings;
using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.BusinessEntities.SettingValues;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class SettingsServiceHelperTests
{
    private Mock<IDeferredChangeRepository> _deferredChangeRepository = null!;
    private Mock<ISettingHistoryRepository> _settingHistoryRepository = null!;
    private Mock<IEventLogRepository> _eventLogRepository = null!;
    private Mock<IEventLogFactory> _eventLogFactory = null!;
    private SettingsService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _deferredChangeRepository = new Mock<IDeferredChangeRepository>();
        _settingHistoryRepository = new Mock<ISettingHistoryRepository>();
        _eventLogRepository = new Mock<IEventLogRepository>();
        _eventLogFactory = new Mock<IEventLogFactory>();

        _eventLogFactory
            .Setup(f => f.ChangesScheduled(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<SettingValueUpdatesDataContract>(),
                It.IsAny<DateTime>(),
                It.IsAny<bool>(),
                It.IsAny<bool>()))
            .Returns(new EventLogBusinessEntity());

        _sut = new SettingsService(
            Mock.Of<ILogger<SettingsService>>(),
            Mock.Of<ISettingClientRepository>(),
            _eventLogRepository.Object,
            _settingHistoryRepository.Object,
            Mock.Of<IClientOverrideService>(),
            new SettingConverter(new ValueToStringConverter()),
            Mock.Of<ISettingDefinitionConverter>(),
            _eventLogFactory.Object,
            Mock.Of<IConfigurationRepository>(),
            Mock.Of<IValidValuesHandler>(),
            Mock.Of<IDeferredClientImportRepository>(),
            Mock.Of<ISettingChangeRepository>(),
            Mock.Of<ISettingApplier>(),
            Mock.Of<ISettingChangeRecorder>(),
            Mock.Of<IWebHookDisseminationService>(),
            Mock.Of<IStatusService>(),
            Mock.Of<ISecretStoreHandler>(),
            Mock.Of<IEventDistributor>(),
            _deferredChangeRepository.Object,
            Mock.Of<IClientRegistrationLockService>(),
            Mock.Of<IRegistrationStatusValidator>(),
            Mock.Of<IClientRegistrationHistoryService>(),
            Mock.Of<ISettingGroupService>());

        _sut.SetAuthenticatedUser(new UserDataContract(
            Guid.NewGuid(),
            "test-user",
            "Test",
            "User",
            Role.Administrator,
            ".*",
            Enum.GetValues<Classification>().ToList()));
    }

    [Test]
    public void HasValueChanged_ReturnsFalse_WhenValuesEqual()
    {
        var existing = CreateSetting("MySetting", "same-value");
        var overrideContract = new SettingDataContract(
            "MySetting",
            new StringSettingDataContract("same-value"));

        var result = _sut.HasValueChanged(existing, overrideContract);

        Assert.That(result, Is.False);
    }

    [Test]
    public void HasValueChanged_ReturnsTrue_WhenValuesDiffer()
    {
        var existing = CreateSetting("MySetting", "old-value");
        var overrideContract = new SettingDataContract(
            "MySetting",
            new StringSettingDataContract("new-value"));

        var result = _sut.HasValueChanged(existing, overrideContract);

        Assert.That(result, Is.True);
    }

    [Test]
    public void FilterChangedOverrides_ReturnsAll_WhenExistingClientIsNull()
    {
        var overrides = new List<SettingDataContract>
        {
            new("A", new StringSettingDataContract("1")),
            new("B", new StringSettingDataContract("2"))
        };

        var result = _sut.FilterChangedOverrides(overrides, null);

        Assert.That(result, Is.SameAs(overrides));
        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public void FilterChangedOverrides_FiltersUnchangedOverrides()
    {
        var existingClient = new SettingClientBusinessEntity
        {
            Id = Guid.NewGuid(),
            Name = "Client",
            Settings =
            [
                CreateSetting("Unchanged", "same"),
                CreateSetting("Changed", "old"),
                CreateSetting("Other", "keep")
            ]
        };

        var overrides = new List<SettingDataContract>
        {
            new("Unchanged", new StringSettingDataContract("same")),
            new("Changed", new StringSettingDataContract("new")),
            new("BrandNew", new StringSettingDataContract("fresh"))
        };

        var result = _sut.FilterChangedOverrides(overrides, existingClient);

        Assert.That(result.Select(o => o.Name), Is.EquivalentTo(new[] { "Changed", "BrandNew" }));
    }

    [Test]
    public async Task EnsureRevertScheduledAfterPartialApply_ReturnsEarly_WhenMatchingDeferredChangeExists()
    {
        const string clientName = "MyClient";
        const string instance = "prod";
        var revertAtUtc = new DateTime(2026, 8, 16, 12, 0, 0, DateTimeKind.Utc);

        _deferredChangeRepository
            .Setup(r => r.GetAllChanges())
            .ReturnsAsync(
            [
                new DeferredChangeBusinessEntity
                {
                    ClientName = clientName,
                    Instance = instance,
                    ExecuteAtUtc = revertAtUtc
                }
            ]);

        var client = CreateClient(CreateSetting("A", "current"));
        var updatedSettings = new SettingValueUpdatesDataContract(
            [new SettingDataContract("A", new StringSettingDataContract("temp"))],
            "partial apply");

        await _sut.EnsureRevertScheduledAfterPartialApply(
            clientName,
            instance,
            updatedSettings,
            client,
            updatedSettings.ValueUpdates.ToList(),
            revertAtUtc);

        _deferredChangeRepository.Verify(r => r.Schedule(It.IsAny<DeferredChangeBusinessEntity>()), Times.Never);
        _settingHistoryRepository.Verify(
            r => r.GetAll(It.IsAny<Guid>(), It.IsAny<string>()),
            Times.Never);
    }

    [Test]
    public async Task EnsureRevertScheduledAfterPartialApply_SchedulesRevertUsingHistory_WhenNoDuplicate()
    {
        const string clientName = "MyClient";
        var clientId = Guid.NewGuid();
        var revertAtUtc = new DateTime(2026, 8, 16, 14, 0, 0, DateTimeKind.Utc);

        _deferredChangeRepository
            .Setup(r => r.GetAllChanges())
            .ReturnsAsync([]);

        DeferredChangeBusinessEntity? scheduled = null;
        _deferredChangeRepository
            .Setup(r => r.Schedule(It.IsAny<DeferredChangeBusinessEntity>()))
            .Callback<DeferredChangeBusinessEntity>(entity => scheduled = entity)
            .Returns(Task.CompletedTask);

        _settingHistoryRepository
            .Setup(r => r.GetAll(clientId, "A"))
            .ReturnsAsync(
            [
                new SettingValueBusinessEntity
                {
                    ClientId = clientId,
                    SettingName = "A",
                    Value = new StringSettingBusinessEntity("newest"),
                    ChangedAt = DateTime.UtcNow,
                    ChangedBy = "user"
                },
                new SettingValueBusinessEntity
                {
                    ClientId = clientId,
                    SettingName = "A",
                    Value = new StringSettingBusinessEntity("previous"),
                    ChangedAt = DateTime.UtcNow.AddMinutes(-5),
                    ChangedBy = "user"
                }
            ]);

        var client = CreateClient(clientId, CreateSetting("A", "newest"));
        var updatedSettings = new SettingValueUpdatesDataContract(
            [new SettingDataContract("A", new StringSettingDataContract("temp"))],
            "partial apply");

        await _sut.EnsureRevertScheduledAfterPartialApply(
            clientName,
            null,
            updatedSettings,
            client,
            updatedSettings.ValueUpdates.ToList(),
            revertAtUtc);

        Assert.That(scheduled, Is.Not.Null);
        Assert.That(scheduled!.ClientName, Is.EqualTo(clientName));
        Assert.That(scheduled.Instance, Is.Null);
        Assert.That(scheduled.ExecuteAtUtc, Is.EqualTo(revertAtUtc));
        Assert.That(scheduled.RequestingUser, Is.EqualTo("test-user"));
        Assert.That(scheduled.ChangeSet, Is.Not.Null);

        var revertValues = scheduled.ChangeSet!.ValueUpdates.ToList();
        Assert.That(revertValues, Has.Count.EqualTo(1));
        Assert.That(revertValues[0].Name, Is.EqualTo("A"));
        Assert.That(revertValues[0].Value, Is.TypeOf<StringSettingDataContract>());
        Assert.That(((StringSettingDataContract)revertValues[0].Value!).Value, Is.EqualTo("previous"));

        _deferredChangeRepository.Verify(r => r.Schedule(It.IsAny<DeferredChangeBusinessEntity>()), Times.Once);
        _eventLogFactory.Verify(f => f.ChangesScheduled(
            clientName,
            null,
            "test-user",
            It.IsAny<SettingValueUpdatesDataContract>(),
            revertAtUtc,
            true,
            false), Times.Once);
        _eventLogRepository.Verify(r => r.Add(It.IsAny<EventLogBusinessEntity>()), Times.Once);
    }

    [Test]
    public async Task EnsureRevertScheduledAfterPartialApply_DoesNothing_WhenHistoryEmpty()
    {
        const string clientName = "MyClient";
        var clientId = Guid.NewGuid();
        var revertAtUtc = DateTime.UtcNow.AddHours(1);

        _deferredChangeRepository
            .Setup(r => r.GetAllChanges())
            .ReturnsAsync([]);

        _settingHistoryRepository
            .Setup(r => r.GetAll(clientId, "A"))
            .ReturnsAsync(new List<SettingValueBusinessEntity>());

        var client = CreateClient(clientId, CreateSetting("A", "current"));
        var updatedSettings = new SettingValueUpdatesDataContract(
            [new SettingDataContract("A", new StringSettingDataContract("temp"))],
            "partial apply");

        await _sut.EnsureRevertScheduledAfterPartialApply(
            clientName,
            null,
            updatedSettings,
            client,
            updatedSettings.ValueUpdates.ToList(),
            revertAtUtc);

        _deferredChangeRepository.Verify(r => r.Schedule(It.IsAny<DeferredChangeBusinessEntity>()), Times.Never);
        _eventLogRepository.Verify(r => r.Add(It.IsAny<EventLogBusinessEntity>()), Times.Never);
    }

    private static SettingClientBusinessEntity CreateClient(params SettingBusinessEntity[] settings)
        => CreateClient(Guid.NewGuid(), settings);

    private static SettingClientBusinessEntity CreateClient(Guid id, params SettingBusinessEntity[] settings)
    {
        return new SettingClientBusinessEntity
        {
            Id = id,
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
