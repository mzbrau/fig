namespace Fig.Datalayer.BusinessEntities;

public class CheckPointDataBusinessEntity
{
    public virtual Guid Id { get; init; }
    
    public virtual string? ExportAsJson { get; set; }
    
    public virtual DateTime LastEncrypted { get; set; }
}