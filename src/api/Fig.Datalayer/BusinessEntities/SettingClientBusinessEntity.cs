namespace Fig.Datalayer.BusinessEntities;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by nhibernate.
public class SettingClientBusinessEntity : ClientBase
{
    public virtual ICollection<SettingBusinessEntity> Settings { get; set; } =
        new List<SettingBusinessEntity>();

    public virtual ICollection<SettingPluginVerificationBusinessEntity> PluginVerifications { get; set; } =
        new List<SettingPluginVerificationBusinessEntity>();

    public virtual ICollection<SettingDynamicVerificationBusinessEntity> DynamicVerifications { get; set; } =
        new List<SettingDynamicVerificationBusinessEntity>();
}