namespace Fig.WebHooks.Contracts;

public class SettingValueChangedDataContract : IWebHookContract
{
    public SettingValueChangedDataContract(string clientName, string? instance, List<string> updatedSettings, string? username, string changeMessage, Uri? link, bool isTest = false)
    {
        ClientName = clientName;
        Instance = instance;
        UpdatedSettings = updatedSettings;
        Username = username;
        ChangeMessage = changeMessage;
        Link = link;
        IsTest = isTest;
    }

    public string ClientName { get; set; }
    
    public string? Instance { get; set; }
    
    public List<string> UpdatedSettings { get; set; }
    
    public string? Username { get; set; }
    
    public Uri? Link { get; set; }
    
    public string ChangeMessage { get; set; }
    
    public bool IsTest { get; }
}