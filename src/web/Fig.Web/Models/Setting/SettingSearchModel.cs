namespace Fig.Web.Models.Setting;

public class SettingSearchModel
{
    public SettingSearchModel(string clientName, string? instanceName, string settingName)
    {
        ClientName = clientName;
        InstanceName = instanceName ?? string.Empty;
        SettingName = settingName;
    }

    public string ClientName { get; }
    
    public string InstanceName { get; }
    
    public string SettingName { get; }

    public string CategoryColor { get; } = "Red";

    public string Description { get; } = "This setting is good...";

    public string SettingType { get; } = "String";
}