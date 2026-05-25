using Fig.Api;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Services;
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
}
