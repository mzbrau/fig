namespace Fig.Datalayer.BusinessEntities;

public class DeferredClientImportBusinessEntity
{
    public virtual Guid Id { get; set; }
    
    public virtual string Name { get; set; } = null!;

    public virtual string? Instance { get; set; }

    public virtual string SettingValuesAsJson { get; set; } = null!;

    public virtual int SettingCount { get; set; }
    
    public virtual string AuthenticatedUser { get; set; } = null!;

    public virtual DateTime ImportTime { get; set; }
}