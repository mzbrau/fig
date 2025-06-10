namespace Fig.Datalayer.BusinessEntities;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by nhibernate.
public class SettingClientDescriptionBusinessEntity
{
    public virtual Guid Id { get; set; }

    public virtual string? Description { get; set; }

    public virtual SettingClientBusinessEntity Client { get; set; } = null!;
}