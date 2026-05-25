namespace Fig.Api.Services;

public sealed class ApiSecretRotationSnapshot
{
    public ApiSecretRotationSnapshot(
        ApiSecretRotationMigrationStatus status,
        ApiSecretKeyOrder keyOrder,
        bool isRotationConfigured,
        bool isMigrationRequired,
        bool isMigrationCompleted,
        DateTime? startedAtUtc,
        DateTime? completedAtUtc,
        DateTime? failedAtUtc,
        string? startedByHost,
        string? lastCompletedStage,
        int processedRecords,
        string? lastError)
    {
        Status = status;
        KeyOrder = keyOrder;
        IsRotationConfigured = isRotationConfigured;
        IsMigrationRequired = isMigrationRequired;
        IsMigrationCompleted = isMigrationCompleted;
        StartedAtUtc = startedAtUtc;
        CompletedAtUtc = completedAtUtc;
        FailedAtUtc = failedAtUtc;
        StartedByHost = startedByHost;
        LastCompletedStage = lastCompletedStage;
        ProcessedRecords = processedRecords;
        LastError = lastError;
    }

    public ApiSecretRotationMigrationStatus Status { get; }

    public ApiSecretKeyOrder KeyOrder { get; }

    public bool IsRotationConfigured { get; }

    public bool IsMigrationRequired { get; }

    public bool IsMigrationCompleted { get; }

    public DateTime? StartedAtUtc { get; }

    public DateTime? CompletedAtUtc { get; }

    public DateTime? FailedAtUtc { get; }

    public string? StartedByHost { get; }

    public string? LastCompletedStage { get; }

    public int ProcessedRecords { get; }

    public string? LastError { get; }
}
