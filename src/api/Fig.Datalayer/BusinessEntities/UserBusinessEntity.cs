using Fig.Contracts.Authentication;

namespace Fig.Datalayer.BusinessEntities;

public class UserBusinessEntity
{
    public virtual Guid Id { get; set; }
    
    public virtual string Username { get; set; }
    
    public virtual string FirstName { get; set; }
    
    public virtual string LastName { get; set; }
    
    public virtual Role Role { get; set; }
    
    public virtual string PasswordHash { get; set; }
}