namespace Fig.Datalayer.BusinessEntities;

public class SettingChangeBusinessEntity
{
    public virtual Guid Id { get; set; }
    
    public virtual string ServerName { get; set; } = null!;
    
    public virtual DateTime LastChange { get; set; }
}