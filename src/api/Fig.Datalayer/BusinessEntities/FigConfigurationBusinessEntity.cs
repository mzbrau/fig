namespace Fig.Datalayer.BusinessEntities;

public class FigConfigurationBusinessEntity
{
    public virtual Guid Id { get; set; }
    
    public virtual bool AllowNewRegistrations { get; set; } = true;

    public virtual bool AllowUpdatedRegistrations { get; set; } = true;

    public virtual bool AllowFileImports { get; set; } = true;

    public virtual bool AllowOfflineSettings { get; set; } = true;

    public virtual bool AllowDynamicVerifications { get; set; } = true;
}