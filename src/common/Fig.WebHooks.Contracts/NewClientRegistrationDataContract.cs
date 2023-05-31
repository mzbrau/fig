namespace Fig.WebHooks.Contracts;

public class ClientRegistrationDataContract
{
    public ClientRegistrationDataContract(string clientName, string? instance, List<string> settings)
    {
        ClientName = clientName;
        Instance = instance;
        Settings = settings;
    }

    public string ClientName { get; set; }
    
    public string? Instance { get; set; }

    public List<string> Settings { get; set; }
}