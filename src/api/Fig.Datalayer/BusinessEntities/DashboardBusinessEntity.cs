namespace Fig.Datalayer.BusinessEntities;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by nhibernate.
public class DashboardBusinessEntity
{
    public virtual Guid? Id { get; init; }

    public virtual string Name { get; set; } = default!;

    public virtual string? Description { get; set; }

    public virtual bool AdminOnly { get; set; }

    public virtual string DefinitionJson { get; set; } = "{}";

    public virtual DateTime CreatedAt { get; set; }

    public virtual DateTime LastModifiedAt { get; set; }

    public virtual string? LastModifiedBy { get; set; }
}
