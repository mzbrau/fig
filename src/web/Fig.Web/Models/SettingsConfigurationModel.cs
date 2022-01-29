using System.Text;

namespace Fig.Web.Models
{
    public class SettingsConfigurationModel
    {
        private List<string> _dirtySettings = new List<string>();

        public string Name { get; set; }

        public string? DisplayName { get; set; }

        public string? Instance { get; set; }

        public bool IsGroup { get; set; }

        public List<SettingConfigurationModel> Settings { get; set; }

        public bool IsDirty { get; set; }

        public void SettingValueChanged(bool isDirty, string settingName)
        {
            IsDirty = true;
            if (isDirty)
            {
                if (!_dirtySettings.Contains(settingName))
                    _dirtySettings.Add(settingName);
            }
            else
            {
                _dirtySettings.Remove(settingName);
            }

            UpdateDisplayName();
        }

        public void ClearDirty()
        {
            IsDirty = false;
            Settings.ForEach(x => x.ClearDirty());
            _dirtySettings.Clear();
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

            if (_dirtySettings.Any())
                builder.Append($" ({_dirtySettings.Count}*)");

            DisplayName = builder.ToString();
        }
    }
}
