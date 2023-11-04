namespace Fig.Web.Models.Setting;

public class ChangeModel
{
    public ChangeModel(string clientName, string settingName, string? newValue, string runSessionsToBeUpdated)
    {
        ClientName = clientName;
        SettingName = settingName;
        NewValue = newValue;
        RunSessionsToBeUpdated = runSessionsToBeUpdated;
    }

    public string ClientName { get; }
    
    public string SettingName { get; }
    
    public string? NewValue { get; }
    
    public string RunSessionsToBeUpdated { get; set; }
}