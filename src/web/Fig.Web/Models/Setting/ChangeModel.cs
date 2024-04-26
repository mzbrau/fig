namespace Fig.Web.Models.Setting;

public class ChangeModel
{
    public ChangeModel(string clientName, string settingName, string change, string runSessionsToBeUpdated)
    {
        ClientName = clientName;
        SettingName = settingName;
        Change = change;
        RunSessionsToBeUpdated = runSessionsToBeUpdated;
    }

    public string ClientName { get; }
    
    public string SettingName { get; }
    
    public string Change { get; }
    
    public string RunSessionsToBeUpdated { get; set; }
}