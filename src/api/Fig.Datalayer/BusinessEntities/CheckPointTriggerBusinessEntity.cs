namespace Fig.Datalayer.BusinessEntities;

public class CheckPointTriggerBusinessEntity
{
    public virtual Guid Id { get; init; }
    
    public virtual string AfterEvent { get; set; } = default!;
    
    public virtual DateTime Timestamp { get; set; }
    
    public virtual string? HandlingInstance { get; set; }
    
    public virtual string? User { get; set; }
}