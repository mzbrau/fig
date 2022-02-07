namespace Fig.Web.Events
{
    public class SettingEventModel : EventArgs
    {
        public SettingEventModel(string name, SettingEventType eventType)
        {
            SettingName = name;
            EventType = eventType;
        }

        public string SettingName { get; set; }

        public string ClientName { get; set; }

        public SettingEventType EventType { get; set; }
    }
}
