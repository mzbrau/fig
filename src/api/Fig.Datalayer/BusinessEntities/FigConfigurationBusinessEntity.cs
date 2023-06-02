namespace Fig.Datalayer.BusinessEntities;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by nhibernate.
public class FigConfigurationBusinessEntity
{
    public virtual Guid Id { get; init; }
    
    public virtual bool AllowNewRegistrations { get; set; } = true;

    public virtual bool AllowUpdatedRegistrations { get; set; } = true;

    public virtual bool AllowFileImports { get; set; } = true;

    public virtual bool AllowOfflineSettings { get; set; } = true;

    public virtual bool AllowDynamicVerifications { get; set; } = true;

    public virtual long DelayBeforeMemoryLeakMeasurementsMs { get; set; } = 300000;

    public virtual long IntervalBetweenMemoryLeakChecksMs { get; set; } = 1200000;

    public virtual string? WebApplicationBaseAddress { get; set; } = "https://localhost:7148/";
}