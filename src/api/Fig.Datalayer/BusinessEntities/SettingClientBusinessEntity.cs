
namespace Fig.Datalayer.BusinessEntities;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by nhibernate.
public class SettingClientBusinessEntity : ClientBase
{
    public virtual SettingClientDescriptionBusinessEntity? DescriptionWrapper { get; set; }

    public virtual string? Description => DescriptionWrapper?.Description;

    public virtual ICollection<SettingBusinessEntity> Settings { get; set; } =
        new List<SettingBusinessEntity>();

    public virtual IList<CustomActionBusinessEntity> CustomActions { get; set; } = new List<CustomActionBusinessEntity>();
}