namespace Fig.Web.Models.Setting;

public class SettingSearchModel
{
    public SettingSearchModel(string clientName, string? instanceName, string settingName)
    {
        ClientName = clientName;
        SettingName = settingName;
    }

    public string ClientName { get; }

    public string InstanceName { get; } = "Instance1";
    
    public string SettingName { get; }

    public string CategoryColor { get; } = "Red";

    public string Description { get; } = "This is a long description that has lots of lines in it";

    public string SettingType { get; } = "String";
    
    public string SettingValue { get; } = "This is a setting value and a";
}