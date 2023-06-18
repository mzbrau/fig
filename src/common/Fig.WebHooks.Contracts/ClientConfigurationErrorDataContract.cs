namespace Fig.WebHooks.Contracts;

public class ClientConfigurationErrorDataContract
{
    public ClientConfigurationErrorDataContract(string clientName,
        string? instance,
        ConfigurationErrorStatus status,
        string figVersion,
        string applicationVersion,
        List<string> configurationErrors,
        Uri? link)
    {
        ClientName = clientName;
        Instance = instance;
        Status = status;
        FigVersion = figVersion;
        ApplicationVersion = applicationVersion;
        ConfigurationErrors = configurationErrors;
        Link = link;
    }

    public string ClientName { get; set; }
    
    public string? Instance { get; set; }
    
    public ConfigurationErrorStatus Status { get; set; }

    public string FigVersion { get; set; }

    public string ApplicationVersion { get; set; }
    
    public List<string> ConfigurationErrors { get; set; }
    
    public Uri? Link { get; set; }
}