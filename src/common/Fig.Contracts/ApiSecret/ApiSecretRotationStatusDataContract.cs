using System;
using System.Collections.Generic;

namespace Fig.Contracts.ApiSecret;

public class ApiSecretRotationStatusDataContract
{
    public string Status { get; set; } = default!;

    public string KeyOrder { get; set; } = default!;

    public bool IsRotationConfigured { get; set; }

    public bool IsMigrationRequired { get; set; }

    public bool IsMigrationCompleted { get; set; }

    public DateTime? StartedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public DateTime? FailedAtUtc { get; set; }

    public string? StartedByHost { get; set; }

    public string? LastCompletedStage { get; set; }

    public int ProcessedRecords { get; set; }

    public string? LastError { get; set; }

    public string? CurrentStageId { get; set; }

    public string? CurrentStageName { get; set; }

    public int? CurrentStageIndex { get; set; }

    public int? TotalStages { get; set; }

    public int StageProcessedRecords { get; set; }

    public int? StageTotalRecords { get; set; }

    public string? CurrentItem { get; set; }

    public string? CurrentProgressMessage { get; set; }

    public string? CompletionReminder { get; set; }

    public IList<ApiSecretRotationStageProgressDataContract> Stages { get; set; } =
        new List<ApiSecretRotationStageProgressDataContract>();
}
