using Fig.Contracts.ApiSecret;

namespace Fig.Api.Services;

public interface IApiSecretRotationStateService
{
    Task<ApiSecretRotationSnapshot> GetSnapshot(bool upgradeLock = false);

    Task<ApiSecretRotationStatusDataContract> GetStatus();

    Task<ApiSecretRotationSnapshot> MarkMigrationStarted();

    Task MarkMigrationStageCompleted(string stage, int processedRecords);

    Task MarkMigrationCompleted();

    Task MarkMigrationFailed(Exception exception);
}
