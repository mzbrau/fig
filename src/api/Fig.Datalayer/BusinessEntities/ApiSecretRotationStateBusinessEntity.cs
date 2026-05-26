namespace Fig.Datalayer.BusinessEntities;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by nhibernate.
public class ApiSecretRotationStateBusinessEntity
{
    public virtual Guid Id { get; init; }

    public virtual string CurrentSecretFingerprint { get; set; } = default!;

    public virtual string PreviousSecretFingerprint { get; set; } = default!;

    public virtual string Status { get; set; } = default!;

    public virtual DateTime CreatedAtUtc { get; set; }

    public virtual DateTime UpdatedAtUtc { get; set; }

    public virtual DateTime? StartedAtUtc { get; set; }

    public virtual DateTime? CompletedAtUtc { get; set; }

    public virtual DateTime? FailedAtUtc { get; set; }

    public virtual string? StartedByHost { get; set; }

    public virtual string? LastCompletedStage { get; set; }

    public virtual int ProcessedRecords { get; set; }

    public virtual string? LastError { get; set; }

    public virtual string? ProgressJson { get; set; }
}
