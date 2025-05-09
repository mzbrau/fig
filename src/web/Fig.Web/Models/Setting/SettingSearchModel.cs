namespace Fig.Web.Models.Setting;

public class SettingSearchModel
{
    public SettingSearchModel(string clientName, string? instanceName, string settingName)
    {
        ClientName = clientName;
        InstanceName = instanceName;
        SettingName = settingName;
    }

    public string ClientName { get; }
    
    public string? InstanceName { get; }
    
    public string SettingName { get; }
}