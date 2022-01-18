namespace Fig.Datalayer.BusinessEntities;

public class SettingClientBusinessEntity
{
    public virtual Guid Id { get; set; }

    public virtual string Name { get; set; }

    public virtual string ClientSecret { get; set; }

    public virtual string? Instance { get; set; }

    public virtual ICollection<SettingBusinessEntity> Settings { get; set; }

    public virtual ICollection<SettingPluginVerificationBusinessEntity> PluginVerifications { get; set; }

    public virtual ICollection<SettingDynamicVerificationBusinessEntity> DynamicVerifications { get; set; }
}