using Fig.Contracts.Settings;
using Fig.Web.Events;
using System.Text;

namespace Fig.Web.Models
{
    public class SettingClientConfigurationModel
    {
        private int _dirtySettingsCount;
        private int _invalidSettingsCount;
        public event EventHandler StateChanged;

        public string Name { get; set; }

        public string? DisplayName { get; set; }

        public string? Instance { get; set; }

        public bool IsGroup { get; set; }

        public List<SettingConfigurationModel> Settings { get; set; }

        public bool IsDirty => _dirtySettingsCount > 0;

        public bool IsValid => _invalidSettingsCount > 0;

        public void SettingStateChanged(SettingEvent settingEvent)
        {
            if (settingEvent.EventType == SettingEventType.DirtyChanged)
            {
                _dirtySettingsCount = Settings?.Count(a => a.IsDirty) ?? 0;
            }
            else if (settingEvent.EventType == SettingEventType.ValidChanged)
            {
                _invalidSettingsCount = Settings?.Count(a => !a.IsValid) ?? 0;
            }

            UpdateDisplayName();
            StateChanged?.Invoke(this, new EventArgs());
        }

        public void ClearDirty()
        {
            Settings.ForEach(x => x.ClearDirty());
            _dirtySettingsCount = 0;
            UpdateDisplayName();
        }

        public void UpdateDisplayName()
        {
            var builder = new StringBuilder();
            if (IsGroup)
                builder.Append($"{Name}: ");

            builder.Append(Name);

            if (!string.IsNullOrWhiteSpace(Instance))
                builder.Append($" [{Instance}]");

            if (_dirtySettingsCount > 0)
                builder.Append($" ({_dirtySettingsCount}*)");

            DisplayName = builder.ToString();
        }

        public IEnumerable<SettingDataContract> GetChangedSettings()
        {
            foreach (var setting in Settings)
            {
                if (setting.IsDirty && setting.IsValid)
                    yield return new SettingDataContract()
                    {
                        Name = setting.Name,
                        Value = setting.GetValue()
                    };
            }
        }

        internal SettingClientConfigurationModel CreateInstance(string instanceName)
        {
            var instance = new SettingClientConfigurationModel()
            {
                Name = Name,
                Instance = instanceName,
                Settings = Settings.Select(a => a.Clone(SettingStateChanged)).ToList(),
            };
            instance.SettingStateChanged(new SettingEvent(Name, SettingEventType.DirtyChanged));
            instance.UpdateDisplayName();

            return instance;
        }
    }
}
