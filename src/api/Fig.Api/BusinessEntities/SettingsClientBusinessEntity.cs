
namespace Fig.Api.BusinessEntities;

public class SettingsClientBusinessEntity
{
    public virtual Guid Id { get; set; }
    
    public virtual string Name { get; set; }

    public virtual string ClientSecret { get; set; }

    public virtual string? Instance { get; set; }

    public virtual List<SettingBusinessEntity> Settings { get; set; }
}