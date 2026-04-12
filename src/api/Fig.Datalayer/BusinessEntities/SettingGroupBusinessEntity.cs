using Newtonsoft.Json;

namespace Fig.Datalayer.BusinessEntities;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by nhibernate.
public class SettingGroupBusinessEntity
{
    public virtual Guid? Id { get; init; }

    public virtual string Name { get; set; } = default!;

    public virtual string? Description { get; set; }

    public virtual string GroupSettingsJson { get; set; } = "[]";

    public virtual DateTime CreatedAt { get; set; }

    public virtual DateTime LastModifiedAt { get; set; }

    public virtual string? LastModifiedBy { get; set; }
}
