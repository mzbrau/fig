namespace Fig.Web.Events
{
    public class SettingEventArgs : EventArgs
    {
        public SettingEventArgs(string name, SettingEventType eventType)
        {
            SettingName = name;
            EventType = eventType;
        }

        public string SettingName { get; set; }

        public string ClientName { get; set; }

        public object? CallbackData { get; set; }

        public SettingEventType EventType { get; set; }
    }
}
