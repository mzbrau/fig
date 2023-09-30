namespace Fig.Datalayer.BusinessEntities;

public class SettingChangeBusinessEntity
{
    public virtual Guid Id { get; set; }
    
    public virtual DateTime LastChange { get; set; }
}