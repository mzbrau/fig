using Fig.Contracts.Settings;
using Fig.Web.Events;
using System.Text;

namespace Fig.Web.Models
{
    public class SettingClientConfigurationModel
    {
        private int _dirtySettingsCount;
        private int _invalidSettingsCount;
        public event EventHandler<SettingEventArgs> StateChanged;

        public string Name { get; set; }

        public string? DisplayName { get; set; }

        public string? Instance { get; set; }

        public bool IsGroup { get; set; }

        public List<SettingConfigurationModel> Settings { get; set; }

        public bool IsDirty => _dirtySettingsCount > 0;

        public bool IsValid => _invalidSettingsCount > 0;

        public void SettingStateChanged(SettingEventArgs settingEventArgs)
        {
            if (settingEventArgs.EventType == SettingEventType.DirtyChanged)
            {
                _dirtySettingsCount = Settings?.Count(a => a.IsDirty) ?? 0;
            }
            else if (settingEventArgs.EventType == SettingEventType.ValidChanged)
            {
                _invalidSettingsCount = Settings?.Count(a => !a.IsValid) ?? 0;
            }

            settingEventArgs.ClientName = Name;
            UpdateDisplayName();
            StateChanged?.Invoke(this, settingEventArgs);
        }

        public void MarkAsSaved()
        {
            Settings.ForEach(x => x.MarkAsSaved());
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
            instance.SettingStateChanged(new SettingEventArgs(Name, SettingEventType.DirtyChanged));
            instance.UpdateDisplayName();

            return instance;
        }
    }
}
