namespace Fig.Api.BusinessEntities;

public class ServiceRegistrationBusinessEntity
{
    public string ServiceName { get; set; }
    
    public string ServiceSecret { get; set; }
    
    public string Hostname { get; set; }
    
    public string Username { get; set; }
    
    public string? Instance { get; set; }
    
    public DateTime RegistrationTime { get; set; }
}