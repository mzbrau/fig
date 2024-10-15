namespace Fig.Datalayer.BusinessEntities;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by nhibernate.
public class CheckPointBusinessEntity
{
    public virtual Guid Id { get; init; }
    
    public virtual Guid DataId { get; set; }
    
    public virtual DateTime Timestamp { get; init; }

    public virtual int NumberOfClients { get; set; }
    
    public virtual int NumberOfSettings { get; set; }
    
    public virtual string AfterEvent { get; set; } = default!;
    
    public virtual string? Note { get; set; }
    
    public virtual string? User { get; set; }
}