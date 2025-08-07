namespace Fig.Datalayer.BusinessEntities;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by nhibernate.
public class DatabaseMigrationBusinessEntity
{
    public virtual Guid Id { get; init; }
    
    public virtual int ExecutionNumber { get; set; }
    
    public virtual string Description { get; set; } = default!;
    
    public virtual DateTime ExecutedAt { get; set; }
    
    public virtual TimeSpan ExecutionDuration { get; set; }

    // New status column: 'pending' while running, 'complete' when done.
    public virtual string? Status { get; set; }
}
