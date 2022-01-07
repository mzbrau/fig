namespace Fig.Api.Datalayer.BusinessEntities;

public class EventLogBusinessEntity
{
    public virtual Guid Id { get; set; }
    
    public virtual DateTime Timestamp { get; set; }
    
    public virtual Guid ClientId { get; set; }
    
    public virtual string ClientName { get; set; }
    
    public virtual string? SettingName { get; set; }
    
    public virtual string EventType { get; set; }
    
    public virtual string? OriginalValue { get; set; }
    
    public virtual string? NewValue { get; set; }
}