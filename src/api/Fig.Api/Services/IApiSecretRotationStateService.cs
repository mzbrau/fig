using Fig.Contracts.ApiSecret;

namespace Fig.Api.Services;

public interface IApiSecretRotationStateService
{
    Task<ApiSecretRotationSnapshot> GetSnapshot(bool upgradeLock = false);

    Task<ApiSecretRotationStatusDataContract> GetStatus();

    Task<ApiSecretRotationSnapshot> MarkMigrationStarted();

    Task InitializeMigrationProgress(IEnumerable<ApiSecretRotationStageProgressDataContract> stages);

    Task MarkMigrationStageStarted(string stageId,
        int? totalRecords = null,
        string? currentItem = null,
        string? currentAction = null);

    Task MarkMigrationProgress(string stageId,
        int processedRecords,
        int? totalRecords = null,
        string? currentItem = null,
        string? currentAction = null,
        bool force = false);

    void ReportLiveMigrationProgress(string stageId,
        int processedRecords,
        int? totalRecords = null,
        string? currentItem = null,
        string? currentAction = null);

    Task MarkMigrationStageCompleted(string stageId, int processedRecords, int? totalRecords = null);

    Task MarkMigrationCompleted();

    Task MarkMigrationFailed(Exception exception);
}
