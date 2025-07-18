using Fig.Contracts.Settings;
using Fig.Web.Models.Setting.ConfigurationModels.DataGrid;
using Fig.Web.Scripting;
using Microsoft.AspNetCore.Components;

namespace Fig.Web.Models.Setting;

public interface ISetting : IScriptableSetting
{
    new string Name { get; }
    
    string DisplayName { get; }

    MarkupString Description { get; }
    
    string RawDescription { get; }

    bool IsSecret { get; }

    bool IsGroupManaged { get; set; }

    bool IsDirty { get; }

    new bool IsValid { get; }
    
    new bool Advanced { get; }

    new int? DisplayOrder { get; }

    string? Group { get; }

    bool IsNotDirty { get; }

    bool ResetToDefaultDisabled { get; }

    bool IsHistoryVisible { get; }

    new bool Hidden { get; }

    bool IsDeleted { get; set; }
    
    bool IsCompactView { get; set; }
    
    bool IsEnabledByOtherSetting { get; }
    
    string StringValue { get; }
    
    bool IsBaseSetting { get; set; }
    
    bool? EnvironmentSpecific { get; }
    
    DateTime? LastChanged { get; }
    
    string LastChangedRelative { get; }

    DataGridConfigurationModel? DataGridConfiguration { get; set; }

    SettingClientConfigurationModel Parent { get; }
    
    string ParentName { get; }
    
    string? ParentInstance { get; }

    List<ISetting>? GroupManagedSettings { get; }

    List<SettingHistoryModel>? History { get; }
    
    bool SupportsLiveUpdate { get; }
    
    new string CategoryColor { get; }
    
    new string CategoryName { get; }
    
    new bool IsReadOnly { get; }
    
    bool HasDisplayScript { get; }
    
    string? DisplayScript { get; }
    
    bool IsExternallyManaged { get; }
    
    int? Indent { get; }
    
    ISetting? BaseSetting { get; set; }
    
    bool? MatchesBaseValue { get; }

    string? ScheduledChangeDescription { get; set; }

    Task PopulateHistoryData();

    new void SetValue(object? value);

    SettingValueBaseDataContract? GetValueDataContract();

    void MarkAsSaved();

    ISetting Clone(SettingClientConfigurationModel client, bool markDirty, bool isReadOnly);

    void SetGroupManagedSettings(List<ISetting> matches);

    void ShowAdvancedChanged(bool showAdvanced);
    
    void FilterByBaseValueMatch(bool showModifiedOnly);

    void EnabledByChanged(bool isEnabled);

    void UndoChanges();

    void ResetToDefault();

    Task ShowHistory();

    Task RequestSettingClientIsShown(string? settingGroup);

    void MarkAsSavedBasedOnGroupManagedSettings();

    void EvaluateDirty();

    void UpdateEnabledStatus();

    void FilterChanged(string filter);

    string GetStringValue(int maxLength = 200);

    void ToggleCompactView(bool controlPressed);
    
    void Initialize();

    void RunDisplayScript();

    string GetChangeDiff();
    
    void Unlock();

    void SubscribeToValueChanges(Action<ActionType> instanceSubscription);
    
    void PushValueToBase();
    
    void PullValueFromBase();
    
    void PushValueToInstances();
    
    void NotifyAboutScheduledChange(SettingValueBaseDataContract? changeSetValue, DateTime changeExecuteAtUtc, string changeRequestingUser, string? changeSetChangeMessage);
    
    void ClearScheduledChange();
}