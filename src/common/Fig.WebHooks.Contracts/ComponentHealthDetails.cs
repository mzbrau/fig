namespace Fig.WebHooks.Contracts;

public class ComponentHealthDetails
{
    public ComponentHealthDetails(string name, HealthStatus status, string? message)
    {
        Name = name;
        Status = status;
        Message = message;
    }

    public string Name { get; }
    
    public HealthStatus Status { get; }
    
    public string? Message { get; set; }
}
