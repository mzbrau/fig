using System.Collections.Generic; // Required for ICollection and IList
using Fig.Datalayer.BusinessEntities.CustomActions; // Required for CustomActionBusinessEntity

namespace Fig.Datalayer.BusinessEntities;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by nhibernate.
public class SettingClientBusinessEntity : ClientBase
{
    public virtual ICollection<SettingBusinessEntity> Settings { get; set; } =
        new List<SettingBusinessEntity>();

    public virtual ICollection<SettingVerificationBusinessEntity> Verifications { get; set; } =
        new List<SettingVerificationBusinessEntity>();

    public virtual IList<CustomActionBusinessEntity> CustomActions { get; set; } = new List<CustomActionBusinessEntity>();
}