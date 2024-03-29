// ReSharper disable ClassWithVirtualMembersNeverInherited.Global Required by nhibernate.
namespace Fig.Datalayer.BusinessEntities;

public class MemoryUsageBusinessEntity
{
    public virtual Guid Id { get; set; }

    public virtual double ClientRunTimeSeconds { get; set; }
    
    public virtual long MemoryUsageBytes { get; set; }
}