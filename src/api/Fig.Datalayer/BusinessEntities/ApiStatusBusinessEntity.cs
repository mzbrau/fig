namespace Fig.Datalayer.BusinessEntities;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by nhibernate.
public class ApiStatusBusinessEntity
{
    public virtual Guid? Id { get; init; }

    public virtual Guid RuntimeId { get; set; }

    public virtual DateTime StartTimeUtc { get; set; }

    public virtual DateTime LastSeen { get; set; }

    public virtual string? IpAddress { get; set; }

    public virtual string? Hostname { get; set; }

    public virtual string Version { get; set; } = default!;

    public virtual long MemoryUsageBytes { get; set; }

    public virtual string RunningUser { get; set; } = default!;

    public virtual long TotalRequests { get; set; }

    public virtual double RequestsPerMinute { get; set; }

    public virtual bool IsActive { get; set; }

    public virtual string SecretHash { get; set; } = default!;
    
    public virtual bool ConfigurationErrorDetected { get; set; }
    
    public virtual int NumberOfVerifiers { get; set; }

    public virtual string? Verifiers { get; set; }
}