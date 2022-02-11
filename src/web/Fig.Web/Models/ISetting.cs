namespace Fig.Web.Models;

public interface ISetting
{
    string Name { get; }
    
    bool IsGroupManaged { get; set; }
    
    bool IsDirty { get; }
    
    bool IsValid { get; }
    
    int? DisplayOrder { get; }
    
    string Group { get; }
    
    SettingClientConfigurationModel Parent { get; }
    List<string> LinkedVerifications { get; }
    bool IsNotDirty { get; }
    bool ResetToDefaultDisabled { get; }
    string Description { get; }
    List<ISetting> GroupManagedSettings { get; }
    bool IsHistoryVisible { get; }
    List<SettingHistoryModel> History { get; }
    
    bool IsDeleted { get; set; }

    Task PopulateHistoryData();

    void SetValue(dynamic value);

    dynamic GetValue();

    void MarkAsSaved();

    void SetLinkedVerifications(List<string> verificationNames);

    ISetting Clone(SettingClientConfigurationModel client, bool markDirty);
    
    void SetGroupManagedSettings(List<ISetting> matches);
    
    void UndoChanges();
    void ResetToDefault();
    Task ShowHistory();
    Task RequestSettingClientIsShown(string settingGroup);
    void MarkAsSavedBasedOnGroupManagedSettings();
}