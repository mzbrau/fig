namespace Fig.Web.Models.Setting;

public class SettingHistoryModel
{
    public SettingHistoryModel(DateTime dateTime, string value, string user)
    {
        DateTime = dateTime;
        Value = value;
        User = user;
    }

    public DateTime DateTime { get; }

    public string Value { get; }

    public string User { get; }
}