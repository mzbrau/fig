namespace Fig.WebHooks.Contracts;

public class ClientRegistrationDataContract : IWebHookContract
{
    public ClientRegistrationDataContract(string clientName, string? instance, List<string> settings, RegistrationType registrationType, Uri? link, bool isTest = false)
    {
        ClientName = clientName;
        Instance = instance;
        Settings = settings;
        RegistrationType = registrationType;
        Link = link;
        IsTest = isTest;
    }

    public string ClientName { get; set; }
    
    public string? Instance { get; set; }

    public List<string> Settings { get; set; }
    
    public RegistrationType RegistrationType { get; set; }
    
    public Uri? Link { get; set; }
    
    public bool IsTest { get; }
}