using Fig.Contracts.Authentication;

namespace Fig.Datalayer.BusinessEntities;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by nhibernate.
public class UserBusinessEntity
{
    public virtual Guid Id { get; init; }
    
    public virtual string Username { get; set; } = default!;
    
    public virtual string FirstName { get; set; } = default!;
    
    public virtual string LastName { get; set; } = default!;
    
    public virtual Role Role { get; set; }

    public virtual string ClientFilter { get; set; } = default!;
    
    public virtual string PasswordHash { get; set; } = default!;
}