using Fig.Web.Models;

namespace Fig.Web.Events;

public class SettingEventModel : EventArgs
{
    public SettingEventModel(string name, SettingEventType eventType)
    {
        Name = name;
        EventType = eventType;
    }

    public string Name { get; }

    public SettingClientConfigurationModel? Client { get; set; }

    public SettingEventType EventType { get; }
}