namespace Fig.WebHooks.Contracts;

public class SettingValueChangedDataContract
{
    public SettingValueChangedDataContract(string clientName, string? instance, List<string> updatedSettings, string? username, string changeMessage, Uri? link)
    {
        ClientName = clientName;
        Instance = instance;
        UpdatedSettings = updatedSettings;
        Username = username;
        ChangeMessage = changeMessage;
        Link = link;
    }

    public string ClientName { get; set; }
    
    public string? Instance { get; set; }
    
    public List<string> UpdatedSettings { get; set; }
    
    public string? Username { get; set; }
    
    public Uri? Link { get; set; }
    
    public string ChangeMessage { get; set; }
}