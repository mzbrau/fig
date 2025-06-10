namespace Fig.Datalayer.BusinessEntities;

public class SettingClientDescription
{
    public virtual Guid Id { get; set; }

    public virtual string? Description { get; set; }

    public virtual SettingClientBusinessEntity Client { get; set; }
}