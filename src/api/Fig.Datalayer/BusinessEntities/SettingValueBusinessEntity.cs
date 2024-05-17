using Fig.Datalayer.BusinessEntities.SettingValues;

namespace Fig.Datalayer.BusinessEntities;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by nhibernate.
public class SettingValueBusinessEntity
{
    public virtual Guid Id { get; init; }

    public virtual Guid ClientId { get; set; }

    public virtual string SettingName { get; set; } = default!;
    
    public virtual SettingValueBaseBusinessEntity? Value { get; set; }

    public virtual string? ValueAsJson { get; set; }
    
    public virtual string? ValueAsJsonEncrypted { get; set; }

    public virtual DateTime ChangedAt { get; set; }

    public virtual string ChangedBy { get; set; } = default!;
    
    public virtual DateTime LastEncrypted { get; set; }
}