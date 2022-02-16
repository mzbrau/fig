namespace Fig.Web.Models;

public interface ISetting
{
    string Name { get; }

    string Description { get; }

    bool IsGroupManaged { get; set; }

    bool IsDirty { get; }

    bool IsValid { get; }

    int? DisplayOrder { get; }

    string Group { get; }

    bool IsNotDirty { get; }

    bool ResetToDefaultDisabled { get; }

    bool IsHistoryVisible { get; }

    bool Hide { get; }

    bool IsDeleted { get; set; }

    SettingClientConfigurationModel Parent { get; }

    List<string> LinkedVerifications { get; }

    List<ISetting>? GroupManagedSettings { get; }

    List<SettingHistoryModel>? History { get; }

    Task PopulateHistoryData();

    void SetValue(dynamic value);

    dynamic? GetValue();

    void MarkAsSaved();

    void SetLinkedVerifications(List<string> verificationNames);

    ISetting Clone(SettingClientConfigurationModel client, bool markDirty);

    void SetGroupManagedSettings(List<ISetting> matches);

    void ShowAdvancedChanged(bool showAdvanced);

    void UndoChanges();

    void ResetToDefault();

    Task ShowHistory();

    Task RequestSettingClientIsShown(string settingGroup);

    void MarkAsSavedBasedOnGroupManagedSettings();
}