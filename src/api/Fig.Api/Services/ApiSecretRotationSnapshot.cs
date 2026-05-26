using Fig.Contracts.ApiSecret;

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
        string? lastError,
        string? currentStageId = null,
        string? currentStageName = null,
        int? currentStageIndex = null,
        int? totalStages = null,
        int stageProcessedRecords = 0,
        int? stageTotalRecords = null,
        string? currentItem = null,
        string? currentProgressMessage = null,
        IReadOnlyList<ApiSecretRotationStageProgressDataContract>? stages = null)
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
        CurrentStageId = currentStageId;
        CurrentStageName = currentStageName;
        CurrentStageIndex = currentStageIndex;
        TotalStages = totalStages;
        StageProcessedRecords = stageProcessedRecords;
        StageTotalRecords = stageTotalRecords;
        CurrentItem = currentItem;
        CurrentProgressMessage = currentProgressMessage;
        Stages = stages ?? Array.Empty<ApiSecretRotationStageProgressDataContract>();
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

    public string? CurrentStageId { get; }

    public string? CurrentStageName { get; }

    public int? CurrentStageIndex { get; }

    public int? TotalStages { get; }

    public int StageProcessedRecords { get; }

    public int? StageTotalRecords { get; }

    public string? CurrentItem { get; }

    public string? CurrentProgressMessage { get; }

    public IReadOnlyList<ApiSecretRotationStageProgressDataContract> Stages { get; }
}
