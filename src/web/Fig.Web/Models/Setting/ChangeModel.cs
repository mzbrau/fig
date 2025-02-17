namespace Fig.Web.Models.Setting;

public class ChangeModel
{
    public ChangeModel(string clientName, string settingName, string change, string runSessionsToBeUpdated, bool isValid, bool isExternallyManaged)
    {
        ClientName = clientName;
        SettingName = settingName;
        Change = change;
        RunSessionsToBeUpdated = runSessionsToBeUpdated;
        IsValid = isValid;
        IsExternallyManaged = isExternallyManaged;
    }

    public string ClientName { get; }
    public string SettingName { get; }
    public string Change { get; }
    public string RunSessionsToBeUpdated { get; }
    public bool IsValid { get; }
    public bool IsExternallyManaged { get; }
}