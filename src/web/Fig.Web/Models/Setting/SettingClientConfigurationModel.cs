using System.Text;
using Fig.Common.NetStandard.Scripting;
using Fig.Contracts.Health;
using Fig.Contracts.Settings;
using Fig.Web.Events;
using Fig.Web.Models.CustomActions;
using Fig.Web.Models.Scheduling;
using Fig.Web.Scripting;

namespace Fig.Web.Models.Setting;

public class SettingClientConfigurationModel
{
    private readonly IScriptRunner _scriptRunner;
    private int _invalidSettingsCount;
    private Func<SettingEventModel, Task<object>>? _settingEvent;

    public SettingClientConfigurationModel(string name,
        string description,
        string? instance,
        bool hasDisplayScripts,
        IScriptRunner scriptRunner,
        bool isGroup = false)
    {
        _scriptRunner = scriptRunner;
        Name = name;
        Description = description;
        Instance = instance;
        HasDisplayScripts = hasDisplayScripts;
        IsGroup = isGroup;

        UpdateDisplayName();
    }

    public Guid Id { get; } = Guid.NewGuid();
    
    public string Name { get; }

    public string? DisplayName { get; set; }
    
    public string Description { get; set; }

    public string? Instance { get; }
    
    public bool HasDisplayScripts { get; }

    public bool IsGroup { get; }

    public List<ISetting> Settings { get; set; } = new();

    public List<CustomActionModel> CustomActions { get; set; } = new();

    public bool IsDirty => DirtySettingCount > 0;
    
    public int CurrentRunSessions { get; set; }

    public bool AllRunSessionsRunningLatest { get; set; }
    
    public DateTime? LastRunSessionDisconnected { get; set; }
    
    public string? LastRunSessionMachineName { get; set; }

    public int DirtySettingCount { get; private set; }
    
    public List<string> Instances { get; set; } = new();

    public FigHealthStatus CurrentHealth { get; set; } = FigHealthStatus.Uninitialized;

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
            case SettingEventType.RunScript:
                _scriptRunner.RunScript(settingEventArgs.DisplayScript, new ScriptableClientAdapter(this));
                break;
        }

        settingEventArgs.Client = this;
        UpdateDisplayName();

        if (_settingEvent != null)
            return await _settingEvent(settingEventArgs);

        return Task.CompletedTask;
    }

    public async Task RequestSettingIsShown(string clientName, string? settingName = null, string? clientInstance = null)
    {
        if (_settingEvent != null)
        {
            var settingEvent = new SettingEventModel(clientName, SettingEventType.SelectSetting)
            {
                Client = this,
                TargetSettingName = settingName,
                TargetClientInstance = clientInstance
            };
            await _settingEvent(settingEvent);
        }
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
        UpdateEnabledStatus();
        Settings.ForEach(s => s.Initialize());
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
    
    public void FilterSettingsByCategory(string? categoryName)
    {
        foreach (var setting in Settings)
        {
            setting.CategoryFilterChanged(categoryName);
        }
    }
    
    public List<CategoryFilterModel> GetUniqueCategories()
    {
        var categories = Settings
            .Where(s => !string.IsNullOrWhiteSpace(s.CategoryName))
            .Select(s => new CategoryFilterModel(s.CategoryName, s.CategoryColor))
            .Distinct()
            .OrderBy(c => c.Name)
            .ToList();
            
        return categories;
    }

    public void CollapseAll()
    {
        Settings.ForEach(a => a.IsCompactView = true);
        CustomActions.ForEach(a => a.IsCompactView = true);
    }

    public void ExpandAll()
    {
        Settings.ForEach(a => a.IsCompactView = false);
        CustomActions.ForEach(a => a.IsCompactView = false);
    }
    
    internal async Task<SettingClientConfigurationModel> CreateInstance(string instanceName)
    {
        var instance = new SettingClientConfigurationModel(Name, Description, instanceName, HasDisplayScripts, _scriptRunner)
        {
            CustomActions = CustomActions.Select(a => a.Clone()).ToList()
        };

        instance.Settings = Settings.Select(a => a.Clone(instance, true, false)).ToList();
        await instance.SettingEvent(new SettingEventModel(Name, SettingEventType.DirtyChanged));
        instance.Initialize();
        
        Instances.Add(instanceName);

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

    private IEnumerable<SettingDataContract> GetChanges(List<ISetting> settings)
    {
        foreach (var setting in settings.Where(s => s.IsDirty))
            yield return new SettingDataContract(setting.Name, setting.GetValueDataContract(), setting.IsSecret);
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

    public void NotifyAboutScheduledChange(DeferredChangeModel change)
    {
        foreach (var changeSet in change.ChangeSet?.ValueUpdates ?? [])
        {
            var setting = Settings.FirstOrDefault(a => a.Name == changeSet.Name);
            if (setting != null)
            {
                setting.NotifyAboutScheduledChange(changeSet.Value, change.ExecuteAtUtc, change.RequestingUser, change.ChangeSet?.ChangeMessage);
            }
        }
    }
    
    public void ClearScheduledChanges()
    {
        Settings.ForEach(a => a.ClearScheduledChange());
    }
}