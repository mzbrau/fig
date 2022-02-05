namespace Fig.Web.Events
{
    public class SettingEvent
    {
        public SettingEvent(string name, SettingEventType eventType)
        {
            Name = name;
            EventType = eventType;
        }

        public string Name { get; set; }

        public SettingEventType EventType { get; set; }
    }
}
