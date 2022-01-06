namespace Fig.Api.Datalayer.BusinessEntities;

public class ServiceRegistrationBusinessEntity
{
    public virtual string ServiceName { get; set; }
    
    public virtual string ServiceSecret { get; set; }
    
    public virtual string Hostname { get; set; }
    
    public virtual string Username { get; set; }
    
    public virtual string? Instance { get; set; }
    
    public virtual DateTime RegistrationTime { get; set; }
}