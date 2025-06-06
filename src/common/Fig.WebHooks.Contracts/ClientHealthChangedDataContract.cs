namespace Fig.WebHooks.Contracts;

public class ClientHealthChangedDataContract
{
    public ClientHealthChangedDataContract(string clientName,
        string? instance,
        string? hostname,
        string? ipAddress,
        HealthStatus status,
        string figVersion,
        string applicationVersion,
        HealthDetails healthDetails,
        Uri? link)
    {
        ClientName = clientName;
        Instance = instance;
        Hostname = hostname;
        IpAddress = ipAddress;
        Status = status;
        FigVersion = figVersion;
        ApplicationVersion = applicationVersion;
        HealthDetails = healthDetails;
        Link = link;
    }
    
    public string ClientName { get; }
    
    public string? Instance { get; }
    
    public string? Hostname { get; }
    
    public string? IpAddress { get; }
    
    public HealthStatus Status { get; }
    
    public string FigVersion { get; }
    
    public string ApplicationVersion { get; }
    
    public HealthDetails HealthDetails { get; }
    
    public Uri? Link { get; }
}