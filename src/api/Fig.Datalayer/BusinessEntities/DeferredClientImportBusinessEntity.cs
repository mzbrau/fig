namespace Fig.Datalayer.BusinessEntities;

public class DeferredClientImportBusinessEntity
{
    public virtual Guid Id { get; set; }
    
    public virtual string Name { get; set; }

    public virtual string? Instance { get; set; }

    public virtual string SettingValuesAsJson { get; set; }
}