using Fig.Api;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Services;
using Fig.Contracts.ApiSecret;
using Fig.Datalayer.BusinessEntities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class ApiSecretRotationStateServiceTests
{
    private Mock<IApiSecretRotationStateRepository> _repository = null!;
    private Mock<IOptionsMonitor<ApiSettings>> _apiSettings = null!;
    private ApiSecretRotationStateService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new Mock<IApiSecretRotationStateRepository>();
        _apiSettings = new Mock<IOptionsMonitor<ApiSettings>>();
        _apiSettings.SetupGet(a => a.CurrentValue).Returns(new ApiSettings
        {
            Secret = "new-secret",
            PreviousSecret = "old-secret",
            DbConnectionString = "Data Source=fig.db;Version=3;New=True"
        });

        _service = new ApiSecretRotationStateService(
            _apiSettings.Object,
            _repository.Object,
            new Mock<ILogger<ApiSecretRotationStateService>>().Object);
    }

    [Test]
    public async Task GetSnapshot_WhenTwoSecretsConfiguredAndNoCompletedState_UsesPreviousSecretFirst()
    {
        var snapshot = await _service.GetSnapshot();

        Assert.That(snapshot.Status, Is.EqualTo(ApiSecretRotationMigrationStatus.PendingMigration));
        Assert.That(snapshot.KeyOrder, Is.EqualTo(ApiSecretKeyOrder.PreviousThenCurrent));
        Assert.That(snapshot.IsMigrationRequired, Is.True);
    }

    [Test]
    public async Task GetSnapshot_WhenMigrationCompletedForSecretPair_UsesCurrentSecretFirst()
    {
        _repository
            .Setup(a => a.GetForSecretPair(It.IsAny<string>(), It.IsAny<string>(), false))
            .ReturnsAsync(new ApiSecretRotationStateBusinessEntity
            {
                Status = ApiSecretRotationMigrationStatus.MigrationCompleted.ToString()
            });

        var snapshot = await _service.GetSnapshot();

        Assert.That(snapshot.Status, Is.EqualTo(ApiSecretRotationMigrationStatus.MigrationCompleted));
        Assert.That(snapshot.KeyOrder, Is.EqualTo(ApiSecretKeyOrder.CurrentThenPrevious));
        Assert.That(snapshot.IsMigrationRequired, Is.False);
    }

    [Test]
    public async Task GetSnapshot_WhenPreviousSecretNotConfigured_UsesCurrentSecretOnly()
    {
        _apiSettings.SetupGet(a => a.CurrentValue).Returns(new ApiSettings
        {
            Secret = "new-secret",
            PreviousSecret = null,
            DbConnectionString = "Data Source=fig.db;Version=3;New=True"
        });

        var snapshot = await _service.GetSnapshot();

        Assert.That(snapshot.Status, Is.EqualTo(ApiSecretRotationMigrationStatus.NotRequired));
        Assert.That(snapshot.KeyOrder, Is.EqualTo(ApiSecretKeyOrder.CurrentOnly));
        Assert.That(snapshot.IsRotationConfigured, Is.False);
    }

    [Test]
    public async Task MarkMigrationStarted_CreatesInProgressStateForCurrentSecretPair()
    {
        ApiSecretRotationStateBusinessEntity? savedState = null;
        _repository
            .Setup(a => a.SaveState(It.IsAny<ApiSecretRotationStateBusinessEntity>()))
            .Callback<ApiSecretRotationStateBusinessEntity>(state => savedState = state)
            .Returns(Task.CompletedTask);
        _repository
            .Setup(a => a.GetForSecretPair(It.IsAny<string>(), It.IsAny<string>(), true))
            .ReturnsAsync((ApiSecretRotationStateBusinessEntity?)null);

        await _service.MarkMigrationStarted();

        Assert.That(savedState, Is.Not.Null);
        Assert.That(savedState!.Status, Is.EqualTo(ApiSecretRotationMigrationStatus.MigrationInProgress.ToString()));
        Assert.That(savedState.StartedByHost, Is.EqualTo(Environment.MachineName));
    }

    [Test]
    public async Task ProgressMethods_ShouldExposeCurrentStageAndStageLineItems()
    {
        var state = CreateCurrentState();
        _repository
            .Setup(a => a.GetForSecretPair(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(state);
        _repository
            .Setup(a => a.UpdateState(It.IsAny<ApiSecretRotationStateBusinessEntity>()))
            .Returns(Task.CompletedTask);

        await _service.InitializeMigrationProgress([
            new ApiSecretRotationStageProgressDataContract
            {
                StageId = "clients",
                DisplayName = "Clients",
                StageIndex = 1
            }
        ]);
        await _service.MarkMigrationStageStarted("clients", 2, "MyApp");
        await _service.MarkMigrationProgress("clients", 1, 2, "OtherApp", true);

        var status = await _service.GetStatus();

        Assert.That(status.CurrentStageId, Is.EqualTo("clients"));
        Assert.That(status.CurrentProgressMessage, Is.EqualTo("1/2 Clients Complete - Migrating OtherApp..."));
        Assert.That(status.StageProcessedRecords, Is.EqualTo(1));
        Assert.That(status.StageTotalRecords, Is.EqualTo(2));
        Assert.That(status.Stages, Has.Count.EqualTo(1));
        Assert.That(status.Stages[0].Status, Is.EqualTo("InProgress"));
        Assert.That(status.Stages[0].CurrentItem, Is.EqualTo("OtherApp"));

        await _service.MarkMigrationStageCompleted("clients", 2, 2);
        status = await _service.GetStatus();

        Assert.That(status.CurrentProgressMessage, Is.EqualTo("2/2 Clients Complete"));
        Assert.That(status.LastCompletedStage, Is.EqualTo("Clients"));
        Assert.That(status.ProcessedRecords, Is.EqualTo(2));
        Assert.That(status.Stages[0].Status, Is.EqualTo("Completed"));
    }

    [Test]
    public async Task MarkMigrationProgress_WhenNotForced_ShouldThrottleFrequentPersistence()
    {
        var state = CreateCurrentState();
        var progressUpdateCount = 0;
        _repository
            .Setup(a => a.GetForSecretPair(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(state);
        _repository
            .Setup(a => a.UpdateState(It.IsAny<ApiSecretRotationStateBusinessEntity>()))
            .Callback(() => progressUpdateCount++)
            .Returns(Task.CompletedTask);

        await _service.InitializeMigrationProgress([
            new ApiSecretRotationStageProgressDataContract
            {
                StageId = "clients",
                DisplayName = "Clients",
                StageIndex = 1
            }
        ]);
        await _service.MarkMigrationStageStarted("clients", 10);
        progressUpdateCount = 0;

        await _service.MarkMigrationProgress("clients", 1, 10, "Client 1");
        await _service.MarkMigrationProgress("clients", 2, 10, "Client 2");
        await _service.MarkMigrationProgress("clients", 3, 10, "Client 3");

        Assert.That(progressUpdateCount, Is.EqualTo(1));
    }

    private static ApiSecretRotationStateBusinessEntity CreateCurrentState()
    {
        return new ApiSecretRotationStateBusinessEntity
        {
            CurrentSecretFingerprint = "current",
            PreviousSecretFingerprint = "previous",
            Status = ApiSecretRotationMigrationStatus.MigrationInProgress.ToString(),
            StartedByHost = Environment.MachineName,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }
}
