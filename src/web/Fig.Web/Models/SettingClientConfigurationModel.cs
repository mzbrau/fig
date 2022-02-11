using System.Text;
using Fig.Contracts.Settings;
using Fig.Web.Events;

namespace Fig.Web.Models;

public class SettingClientConfigurationModel
{
    private int _dirtySettingsCount;
    private int _invalidSettingsCount;
    private Func<SettingEventModel, Task<object>>? _settingEvent;

    public SettingClientConfigurationModel(string name, string? instance, bool isGroup = false)
    {
        Name = name;
        Instance = instance;
        IsGroup = isGroup;

        UpdateDisplayName();
    }

    public string Name { get; set; } = "UNDEFINED";

    public string? DisplayName { get; set; }

    public string? Instance { get; set; }

    public bool IsGroup { get; set; }

    public List<ISetting> Settings { get; set; }

    public List<SettingVerificationModel> Verifications { get; set; } = new();

    public bool IsDirty => _dirtySettingsCount > 0;

    public bool IsValid => _invalidSettingsCount > 0;

    public void RegisterEventAction(Func<SettingEventModel, Task<object>> settingEvent)
    {
        _settingEvent = settingEvent;
    }

    public async Task<object> SettingEvent(SettingEventModel settingEventArgs)
    {
        if (settingEventArgs.EventType == SettingEventType.DirtyChanged)
            _dirtySettingsCount = Settings?.Count(a => a.IsDirty) ?? 0;
        else if (settingEventArgs.EventType == SettingEventType.ValidChanged)
            _invalidSettingsCount = Settings?.Count(a => !a.IsValid) ?? 0;

        settingEventArgs.ClientName = Name;
        UpdateDisplayName();

        if (_settingEvent != null)
            return await _settingEvent(settingEventArgs);

        return Task.CompletedTask;
    }

    public async Task RequestSettingIsShown(string settingName)
    {
        if (_settingEvent != null)
            await _settingEvent(new SettingEventModel(settingName, SettingEventType.SelectSetting));
    }

    public void MarkAsSaved(List<string> changedSettings)
    {
        foreach (var setting in Settings.Where(a => changedSettings.Contains(a.Name)))
            setting.MarkAsSaved();

        _dirtySettingsCount = Settings.Count(a => a.IsDirty);
        UpdateDisplayName();
    }

    public void UpdateDisplayName()
    {
        var builder = new StringBuilder();
        builder.Append(Name);

        if (!string.IsNullOrWhiteSpace(Instance))
            builder.Append($" [{Instance}]");

        if (_dirtySettingsCount > 0)
            builder.Append($" ({_dirtySettingsCount}*)");

        DisplayName = builder.ToString();
    }

    public void CalculateSettingVerificationRelationship()
    {
        var settingsToVerifications = new Dictionary<string, List<string>>();
        foreach (var verification in Verifications)
        foreach (var setting in verification.SettingsVerified)
            if (settingsToVerifications.ContainsKey(setting))
                settingsToVerifications[setting].Add(verification.Name);
            else
                settingsToVerifications.Add(setting, new List<string> {verification.Name});

        foreach (var setting in settingsToVerifications.Keys)
        {
            var match = Settings.FirstOrDefault(a => a.Name == setting);
            match?.SetLinkedVerifications(settingsToVerifications[setting]);
        }
    }

    public Dictionary<SettingClientConfigurationModel, List<SettingDataContract>> GetChangedSettings()
    {
        var result = new Dictionary<SettingClientConfigurationModel, List<SettingDataContract>>();

        if (IsGroup)
            foreach (var setting in Settings.Where(s => s.IsDirty && s.IsValid))
            foreach (var settingGroups in setting.GroupManagedSettings.GroupBy(s => s.Parent))
                if (result.ContainsKey(settingGroups.Key))
                    result[settingGroups.Key].AddRange(GetChanges(settingGroups.ToList()));
                else
                    result.Add(settingGroups.Key, GetChanges(settingGroups.ToList()).ToList());
        else
            result.Add(this, GetChanges(Settings).ToList());

        return result;
    }

    private IEnumerable<SettingDataContract> GetChanges(List<ISetting> settings)
    {
        foreach (var setting in settings.Where(s => s.IsDirty && s.IsValid))
            yield return new SettingDataContract
            {
                Name = setting.Name,
                Value = setting.GetValue()
            };
    }

    internal SettingClientConfigurationModel CreateInstance(string instanceName)
    {
        var instance = new SettingClientConfigurationModel(Name, instanceName)
        {
            Verifications = Verifications.Select(a => a.Clone(SettingEvent)).ToList()
        };

        instance.Settings = Settings.Select(a => a.Clone(instance, true)).ToList();
        instance.SettingEvent(new SettingEventModel(Name, SettingEventType.DirtyChanged));
        instance.UpdateDisplayName();
        instance.CalculateSettingVerificationRelationship();

        return instance;
    }

    public void EvaluateDirtyStatus()
    {
    }

    public void Refresh()
    {
        _dirtySettingsCount = Settings.Count(a => a.IsDirty);
        UpdateDisplayName();

        if (!IsGroup)
            return;

        // Remove any settings that have been deleted.
        foreach (var removedSetting in Settings.Where(a => a.GroupManagedSettings.All(b => b.IsDeleted)).ToList())
            Settings.Remove(removedSetting);

        foreach (var setting in Settings)
            setting.MarkAsSavedBasedOnGroupManagedSettings();
    }

    public void MarkAsDeleted()
    {
        Settings.ForEach(a => a.IsDeleted = true);
    }
}