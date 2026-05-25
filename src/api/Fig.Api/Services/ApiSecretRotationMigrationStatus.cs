namespace Fig.Api.Services;

public enum ApiSecretRotationMigrationStatus
{
    NotRequired,
    PendingMigration,
    MigrationInProgress,
    MigrationCompleted,
    MigrationFailed
}
