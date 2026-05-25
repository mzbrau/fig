using System;

namespace Fig.Contracts.ApiSecret;

public class ApiSecretRotationStageProgressDataContract
{
    public string StageId { get; set; } = default!;

    public string DisplayName { get; set; } = default!;

    public int StageIndex { get; set; }

    public string Status { get; set; } = default!;

    public int ProcessedRecords { get; set; }

    public int? TotalRecords { get; set; }

    public string? CurrentItem { get; set; }

    public string? CurrentAction { get; set; }

    public DateTime? StartedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public DateTime? FailedAtUtc { get; set; }

    public string? Error { get; set; }
}
