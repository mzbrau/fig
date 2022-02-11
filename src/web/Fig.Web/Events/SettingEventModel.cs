namespace Fig.Web.Events;

public class SettingEventModel : EventArgs
{
    public SettingEventModel(string name, SettingEventType eventType)
    {
        Name = name;
        EventType = eventType;
    }

    public string Name { get; set; }

    public string ClientName { get; set; }

    public SettingEventType EventType { get; set; }
}