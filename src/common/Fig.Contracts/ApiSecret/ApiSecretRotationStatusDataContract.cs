using System;

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
}
