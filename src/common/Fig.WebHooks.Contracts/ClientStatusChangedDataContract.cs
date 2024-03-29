namespace Fig.WebHooks.Contracts;

public class ClientStatusChangedDataContract
{
    public ClientStatusChangedDataContract(string clientName,
        string? instance,
        ConnectionEvent connectionEvent,
        DateTime startTime,
        string? ipAddress,
        string? hostname,
        string figVersion,
        string applicationVersion,
        Uri? link)
    {
        ClientName = clientName;
        Instance = instance;
        ConnectionEvent = connectionEvent;
        StartTime = startTime;
        IpAddress = ipAddress;
        Hostname = hostname;
        FigVersion = figVersion;
        ApplicationVersion = applicationVersion;
        Link = link;
    }

    public string ClientName { get; set; }
    
    public string? Instance { get; set; }
    
    public ConnectionEvent ConnectionEvent { get; set; }
    
    public DateTime StartTime { get; set; }

    public string? IpAddress { get; set; }

    public string? Hostname { get; set; }

    public string FigVersion { get; set; }

    public string ApplicationVersion { get; set; }
    
    public Uri? Link { get; set; }
}