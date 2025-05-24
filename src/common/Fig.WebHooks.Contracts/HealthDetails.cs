namespace Fig.WebHooks.Contracts;

public class HealthDetails
{
    public HealthStatus Status { get; set; }
    
    public List<ComponentHealthDetails> Components { get; set; } = new();
}