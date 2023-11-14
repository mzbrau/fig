using System.Text;
using Fig.Contracts.Settings;
using Fig.Web.Events;

namespace Fig.Web.Models.Setting;

public class SettingClientConfigurationModel
{
    private int _invalidSettingsCount;
    private Func<SettingEventModel, Task<object>>? _settingEvent;

    public SettingClientConfigurationModel(string name, string description, string? instance, bool isGroup = false)
    {
        Name = name;
        Description = description;
        Instance = instance;
        IsGroup = isGroup;

        UpdateDisplayName();
    }

    public string Name { get; }

    public string? DisplayName { get; set; }
    
    public string Description { get; }

    public string? Instance { get; }

    public bool IsGroup { get; }

    public List<ISetting> Settings { get; set; } = null!;

    public List<SettingVerificationModel> Verifications { get; set; } = new();

    public bool IsDirty => DirtySettingCount > 0;

    public bool IsValid => _invalidSettingsCount > 0;
    
    public int CurrentRunSessions { get; set; }
    
    public bool HasConfigurationError { get; set; }

    public int DirtySettingCount { get; private set; }

    public void RegisterEventAction(Func<SettingEventModel, Task<object>> settingEvent)
    {
        _settingEvent = settingEvent;
    }

    public async Task<object> SettingEvent(SettingEventModel settingEventArgs)
    {
        switch (settingEventArgs.EventType)
        {
            case SettingEventType.DirtyChanged:
                DirtySettingCount = Settings.Count(a => a.IsDirty);
                break;
            case SettingEventType.ValidChanged:
                _invalidSettingsCount = Settings.Count(a => !a.IsValid);
                break;
        }

        settingEventArgs.Client = this;
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

        DirtySettingCount = Settings.Count(a => a.IsDirty);
        UpdateDisplayName();
    }

    public void Initialize()
    {
        UpdateDisplayName();
        CalculateSettingVerificationRelationship();
        UpdateEnabledStatus();
    }
    
    public Dictionary<SettingClientConfigurationModel, List<SettingDataContract>> GetChangedSettings()
    {
        var result = new Dictionary<SettingClientConfigurationModel, List<SettingDataContract>>();

        if (IsGroup)
        {
            foreach (var setting in Settings.Where(s => s.IsDirty))
            foreach (var settingGroups in setting.GroupManagedSettings?.GroupBy(s => s.Parent) ??
                                          Array.Empty<IGrouping<SettingClientConfigurationModel, ISetting>>())
                if (result.ContainsKey(settingGroups.Key))
                    result[settingGroups.Key].AddRange(GetChanges(settingGroups.ToList()));
                else
                    result.Add(settingGroups.Key, GetChanges(settingGroups.ToList()).ToList());
        }
        else
        {
            result.Add(this, GetChanges(Settings).ToList());
        }

        return result;
    }

    public void Refresh()
    {
        DirtySettingCount = Settings.Count(a => a.IsDirty);
        UpdateDisplayName();

        if (!IsGroup)
            return;

        // Remove any settings that have been deleted.
        foreach (var removedSetting in Settings.Where(a => a.GroupManagedSettings?.All(b => b.IsDeleted) == true).ToList())
            Settings.Remove(removedSetting);

        foreach (var setting in Settings)
            setting.MarkAsSavedBasedOnGroupManagedSettings();
    }

    public void MarkAsDeleted()
    {
        Settings.ForEach(a => a.IsDeleted = true);
    }

    public void ShowAdvancedChanged(bool showAdvanced)
    {
        Settings.ForEach(a => a.ShowAdvancedChanged(showAdvanced));
    }

    public bool IsFilterMatch(string filterText)
    {
        var loweredFilteredText = filterText.ToLower();
        if (Name.ToLower().Contains(loweredFilteredText))
            return true;

        var settingNames = string.Join(",", Settings.Select(a => a.Name.ToLower()));
        if (settingNames.Contains(loweredFilteredText))
            return true;

        return false;
    }

    public string? GetFilterSettingMatch(string filterText)
    {
        return Settings.FirstOrDefault(a => a.Name.ToLower().Contains(filterText.ToLower()))?.Name;
    }

    public void FilterSettings(string filter)
    {
        foreach (var setting in Settings)
        {
            setting.FilterChanged(filter);
        }
    }

    public void CollapseAll()
    {
        Settings.ForEach(a => a.IsCompactView = true);
        Verifications.ForEach(a => a.IsCompactView = true);
    }

    public void ExpandAll()
    {
        Settings.ForEach(a => a.IsCompactView = false);
        Verifications.ForEach(a => a.IsCompactView = false);
    }
    
    internal async Task<SettingClientConfigurationModel> CreateInstance(string instanceName)
    {
        var instance = new SettingClientConfigurationModel(Name, Description, instanceName)
        {
            Verifications = Verifications.Select(a => a.Clone(SettingEvent)).ToList()
        };

        instance.Settings = Settings.Select(a => a.Clone(instance, true, a.IsReadOnly)).ToList();
        await instance.SettingEvent(new SettingEventModel(Name, SettingEventType.DirtyChanged));
        instance.Initialize();

        return instance;
    }

    private void UpdateDisplayName()
    {
        var builder = new StringBuilder();
        builder.Append(Name);

        if (!string.IsNullOrWhiteSpace(Instance))
            builder.Append($" [{Instance}]");

        DisplayName = builder.ToString();
    }

    private void CalculateSettingVerificationRelationship()
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

    private IEnumerable<SettingDataContract> GetChanges(List<ISetting> settings)
    {
        foreach (var setting in settings.Where(s => s.IsDirty))
            yield return new SettingDataContract(setting.Name, setting.GetValueDataContract());
    }

    private void UpdateEnabledStatus()
    {
        Settings.ForEach(a => a.UpdateEnabledStatus());
    }

    public void SetCompactViewForCategory(string? categoryName, bool isCompactView)
    {
        if (!string.IsNullOrWhiteSpace(categoryName))
        {
            foreach (var setting in Settings.Where(a => a.CategoryName == categoryName))
                setting.IsCompactView = isCompactView;
        }
    }
}