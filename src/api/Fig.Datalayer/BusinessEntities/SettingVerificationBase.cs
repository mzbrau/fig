namespace Fig.Datalayer.BusinessEntities;

public abstract class SettingVerificationBase
{
    public virtual Guid Id { get; init; }
    
    public virtual string Name { get; set; } = default!;
}