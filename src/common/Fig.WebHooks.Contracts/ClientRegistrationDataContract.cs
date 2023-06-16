namespace Fig.WebHooks.Contracts;

public class ClientRegistrationDataContract
{
    public ClientRegistrationDataContract(string clientName, string? instance, List<string> settings, RegistrationType registrationType, Uri? link)
    {
        ClientName = clientName;
        Instance = instance;
        Settings = settings;
        RegistrationType = registrationType;
        Link = link;
    }

    public string ClientName { get; set; }
    
    public string? Instance { get; set; }

    public List<string> Settings { get; set; }
    
    public RegistrationType RegistrationType { get; set; }
    
    public Uri? Link { get; set; }
}