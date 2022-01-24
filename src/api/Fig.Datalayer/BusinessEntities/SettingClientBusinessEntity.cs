namespace Fig.Datalayer.BusinessEntities;

public class SettingClientBusinessEntity
{
    public virtual Guid Id { get; set; }

    public virtual string Name { get; set; } = string.Empty;

    public virtual string ClientSecret { get; set; } = string.Empty;

    public virtual string? Instance { get; set; }

    public virtual DateTime? LastRegistration { get; set; }

    public virtual DateTime? LastRead { get; set; }

    public virtual string? IpAddress { get; set; }

    public virtual string? Hostname { get; set; }

    public virtual ICollection<SettingBusinessEntity> Settings { get; set; } =
        new List<SettingBusinessEntity>();

    public virtual ICollection<SettingPluginVerificationBusinessEntity> PluginVerifications { get; set; } =
        new List<SettingPluginVerificationBusinessEntity>();

    public virtual ICollection<SettingDynamicVerificationBusinessEntity> DynamicVerifications { get; set; } =
        new List<SettingDynamicVerificationBusinessEntity>();
}