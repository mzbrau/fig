namespace Fig.WebHooks.Contracts;

public class SettingValueChangedDataContract
{
    public SettingValueChangedDataContract(string clientName, string? instance, List<string> updatedSettings, string? username)
    {
        ClientName = clientName;
        Instance = instance;
        UpdatedSettings = updatedSettings;
        Username = username;
    }

    public string ClientName { get; set; }
    
    public string? Instance { get; set; }
    
    public List<string> UpdatedSettings { get; set; }
    
    public string? Username { get; set; }
}