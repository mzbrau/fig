namespace Fig.Datalayer.BusinessEntities;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by nhibernate.
public class EventLogBusinessEntity
{
    public virtual Guid Id { get; init; }

    public virtual DateTime Timestamp { get; set; }

    public virtual Guid? ClientId { get; set; }

    public virtual string? ClientName { get; set; }

    public virtual string? Instance { get; set; }

    public virtual string? SettingName { get; set; }

    public virtual string EventType { get; set; } = default!;

    public virtual string? OriginalValue { get; set; }

    public virtual string? NewValue { get; set; }

    public virtual string? AuthenticatedUser { get; set; }
    
    public virtual string? Message { get; set; }

    public virtual string? VerificationName { get; set; }

    public virtual string? IpAddress { get; set; }

    public virtual string? Hostname { get; set; }
    
    public virtual DateTime? LastEncrypted { get; set; }
}