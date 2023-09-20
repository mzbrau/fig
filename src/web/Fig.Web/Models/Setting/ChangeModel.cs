namespace Fig.Web.Models.Setting;

public class ChangeModel
{
    public ChangeModel(string clientName, string settingName, string? newValue)
    {
        ClientName = clientName;
        SettingName = settingName;
        NewValue = newValue;
    }

    public string ClientName { get; }
    
    public string SettingName { get; }
    
    public string? NewValue { get; }
}