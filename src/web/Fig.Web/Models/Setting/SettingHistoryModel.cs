namespace Fig.Web.Models.Setting;

public class SettingHistoryModel
{
    public SettingHistoryModel(DateTime dateTime, string value, string user, string? changeMessage)
    {
        DateTime = dateTime;
        Value = value;
        User = user;
        ChangeMessage = changeMessage;
    }

    public DateTime DateTime { get; }

    public string Value { get; }

    public string User { get; }

    public string? ChangeMessage { get; }
}