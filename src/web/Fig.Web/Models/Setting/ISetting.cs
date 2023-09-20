using Fig.Contracts.Settings;
using Fig.Web.Models.Setting.ConfigurationModels.DataGrid;
using Microsoft.AspNetCore.Components;

namespace Fig.Web.Models.Setting;

public interface ISetting
{
    string Name { get; }

    MarkupString Description { get; }

    bool IsGroupManaged { get; set; }

    bool IsDirty { get; }

    bool IsValid { get; }
    
    bool Advanced { get; }

    int? DisplayOrder { get; }

    string? Group { get; }

    bool IsNotDirty { get; }

    bool ResetToDefaultDisabled { get; }

    bool IsHistoryVisible { get; }

    bool Hide { get; }

    bool IsDeleted { get; set; }
    
    bool IsCompactView { get; set; }
    
    bool IsEnabledByOtherSetting { get; }
    
    string StringValue { get; }
    
    DateTime? LastChanged { get; }
    
    string LastChangedRelative { get; }

    DataGridConfigurationModel? DataGridConfiguration { get; set; }

    SettingClientConfigurationModel Parent { get; }
    
    string ParentName { get; }
    
    string? ParentInstance { get; }

    List<string> LinkedVerifications { get; }

    List<ISetting>? GroupManagedSettings { get; }

    List<SettingHistoryModel>? History { get; }
    
    bool SupportsLiveUpdate { get; }
    
    string CategoryColor { get; }
    
    string CategoryName { get; }
    
    bool IsReadOnly { get; }

    Task PopulateHistoryData();

    void SetValue(object? value);

    SettingValueBaseDataContract? GetValueDataContract();

    void MarkAsSaved();

    void SetLinkedVerifications(List<string> verificationNames);

    ISetting Clone(SettingClientConfigurationModel client, bool markDirty, bool isReadOnly);

    void SetGroupManagedSettings(List<ISetting> matches);

    void ShowAdvancedChanged(bool showAdvanced);

    void EnabledByChanged(bool isEnabled);

    void UndoChanges();

    void ResetToDefault();

    Task ShowHistory();

    Task RequestSettingClientIsShown(string? settingGroup);

    void MarkAsSavedBasedOnGroupManagedSettings();

    void EvaluateDirty();

    void UpdateEnabledStatus();

    void FilterChanged(string filter);

    string GetStringValue();

    void ToggleCompactView();
}