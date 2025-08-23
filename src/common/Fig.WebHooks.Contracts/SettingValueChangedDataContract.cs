namespace Fig.WebHooks.Contracts;

public class SettingValueChangedDataContract : IWebHookContract
{
    public SettingValueChangedDataContract(string clientName, string? instance, List<string> updatedSettings, string? username, string changeMessage, Uri? link, bool isTest = false)
    {
        if (clientName == null)
            throw new ArgumentNullException(nameof(clientName));
        if (string.IsNullOrWhiteSpace(clientName))
            throw new ArgumentException("Client name cannot be empty or whitespace.", nameof(clientName));
        if (updatedSettings == null)
            throw new ArgumentNullException(nameof(updatedSettings));
        if (updatedSettings.Count == 0)
            throw new ArgumentException("Updated settings cannot be empty.", nameof(updatedSettings));
        if (changeMessage == null)
            throw new ArgumentNullException(nameof(changeMessage));
        
        ClientName = clientName;
        Instance = instance;
        UpdatedSettings = new List<string>(updatedSettings);
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