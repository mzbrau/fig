using System.Text.RegularExpressions;
using Fig.Contracts.SettingDefinitions;
using Fig.Web.Events;
using Fig.Web.ExtensionMethods;
using Fig.Web.Models.Setting.ConfigurationModels.DataGrid;

namespace Fig.Web.Models.Setting;

public abstract class SettingConfigurationModel<T> : ISetting
{
    private readonly Regex? _regex;
    protected readonly SettingDefinitionDataContract DefinitionDataContract;
    private readonly IList<string>? _enablesSettings;
    private bool _isDirty;
    private bool _isValid;
    private bool _showAdvanced;
    private bool _isEnabled = true;

    private T? _value;
    protected T? OriginalValue;

    internal SettingConfigurationModel(SettingDefinitionDataContract dataContract,
        SettingClientConfigurationModel parent)
    {
        Name = dataContract.Name;
        Description = dataContract.Description;
        var validationRegex = dataContract.ValidationRegex;
        ValidationExplanation = string.IsNullOrWhiteSpace(dataContract.ValidationExplanation)
            ? $"Did not match validation regex ({validationRegex})"
            : dataContract.ValidationExplanation;
        IsSecret = dataContract.IsSecret;
        Group = dataContract.Group;
        DisplayOrder = dataContract.DisplayOrder ?? int.MaxValue;
        Parent = parent;
        DefaultValue = dataContract.DefaultValue;
        Advanced = dataContract.Advanced;
        JsonSchemaString = dataContract.JsonSchema;
        EditorLineCount = dataContract.EditorLineCount;
        _enablesSettings = dataContract.EnablesSettings;

        DefinitionDataContract = dataContract;
        _value = dataContract.GetEditableValue();
        OriginalValue = dataContract.GetEditableValue();
        _isValid = true;

        if (!string.IsNullOrWhiteSpace(validationRegex))
        {
            _regex = new Regex(validationRegex, RegexOptions.Compiled);
            Validate(dataContract.Value?.ToString());
        }
    }

    public bool IsSecret { get; }

    public T? UpdatedValue { get; set; }

    public string ValidationExplanation { get; protected set; }

    public bool InSecretEditMode { get; set; }

    public T? Value
    {
        get => _value;
        set
        {
            if (!EqualityComparer<T>.Default.Equals(_value, value))
            {
                _value = value;
                EvaluateDirty(_value);
                UpdateGroupManagedSettings(_value);
                UpdateEnabledSettings(_value);
            }
        }
    }

    public bool IsReadOnly => IsGroupManaged;

    protected T? DefaultValue { get; set; }

    public bool Advanced { get; }

    public string? JsonSchemaString { get; set; }

    public string Name { get; }

    public string Description { get; }

    public string Group { get; }

    public int? DisplayOrder { get; }
    
    public int? EditorLineCount { get; }
    
    public List<ISetting>? EnablesSettings { get; private set; }

    public DataGridConfigurationModel? DataGridConfiguration { get; set; }

    public SettingClientConfigurationModel Parent { get; }

    public bool IsValid
    {
        get => _isValid;
        protected set
        {
            if (_isValid != value)
            {
                _isValid = value;
#pragma warning disable CS4014
                Parent.SettingEvent(new SettingEventModel(Name, SettingEventType.ValidChanged));
#pragma warning restore CS4014
            }
        }
    }

    public bool IsGroupManaged { get; set; }

    public List<SettingHistoryModel>? History { get; set; }

    public bool IsHistoryVisible { get; set; }

    public bool IsDeleted { get; set; }

    public List<string> LinkedVerifications { get; set; } = new();

    public bool ResetToDefaultDisabled => DefinitionDataContract.DefaultValue == null ||
                                          GetValue(true) == DefinitionDataContract.DefaultValue;

    public List<ISetting>? GroupManagedSettings { get; set; } = new();

    public bool IsDirty
    {
        get => _isDirty;
        protected set
        {
            if (_isDirty != value)
            {
                _isDirty = value;
#pragma warning disable CS4014
                Parent.SettingEvent(new SettingEventModel(Name, SettingEventType.DirtyChanged));
#pragma warning restore CS4014
            }
        }
    }

    public bool IsNotDirty => !IsDirty;

    public bool Hide { get; private set; }

    public void MarkAsSaved()
    {
        IsDirty = false;
        OriginalValue = GetValue(true);
    }

    public void ShowAdvancedChanged(bool showAdvanced)
    {
        _showAdvanced = showAdvanced;
        SetHideStatus();
    }

    public void EnabledByChanged(bool isEnabled)
    {
        _isEnabled = isEnabled;
        SetHideStatus();
    }

    public void SetLinkedVerifications(List<string> verificationNames)
    {
        LinkedVerifications = verificationNames;
    }

    public abstract ISetting Clone(SettingClientConfigurationModel parent, bool setDirty);

    public void SetValue(dynamic value)
    {
        Value = value;
    }

    public virtual dynamic? GetValue(bool formatAsT = false)
    {
        return Value;
    }

    public void UndoChanges()
    {
        Value = OriginalValue;
    }

    public void UpdateEnabledStatus()
    {
        UpdateEnabledSettings(Value);
    }

    public async Task ShowHistory()
    {
        IsHistoryVisible = !IsHistoryVisible;

        if (!IsHistoryVisible)
            return;

        if (GroupManagedSettings?.Any() == true)
        {
            foreach (var setting in GroupManagedSettings)
                await setting.PopulateHistoryData();

            return;
        }

        await PopulateHistoryData();
    }

    public async Task PopulateHistoryData()
    {
        var settingEvent = new SettingEventModel(Name, SettingEventType.SettingHistoryRequested);
        var result = await Parent.SettingEvent(settingEvent);
        if (result is List<SettingHistoryModel> history)
            History = history;
    }

    public void ResetToDefault()
    {
        if (DefinitionDataContract.DefaultValue != null)
            Value = DefinitionDataContract.DefaultValue;
    }

    public void SetGroupManagedSettings(List<ISetting> groupManagedSettings)
    {
        GroupManagedSettings = groupManagedSettings;
        foreach (var setting in GroupManagedSettings)
            setting.IsGroupManaged = true;
    }

    public async Task RequestSettingClientIsShown(string settingToSelect)
    {
        await Parent.RequestSettingIsShown(settingToSelect);
    }

    public void MarkAsSavedBasedOnGroupManagedSettings()
    {
        if (GroupManagedSettings?.All(a => !a.IsDirty) == true)
            MarkAsSaved();
    }

    public virtual void EvaluateDirty()
    {
        // For data grid override
    }

    public void SetUpdatedSecretValue()
    {
        if (IsUpdatedSecretValueValid())
        {
            ApplyUpdatedSecretValue();
            InSecretEditMode = false;
            IsDirty = true;
        }
        else
        {
#pragma warning disable CS4014
            Parent.SettingEvent(new SettingEventModel(Name, "Password values do not match", SettingEventType.ShowErrorNotification));
#pragma warning restore CS4014
        }
    }

    public void ValueChanged(string value)
    {
        Validate(value);
    }

    protected virtual bool IsUpdatedSecretValueValid()
    {
        return true;
    }

    protected virtual void EvaluateDirty(dynamic? value)
    {
        IsDirty = OriginalValue != value;
    }

    protected virtual void Validate(string value)
    {
        if (_regex != null)
            IsValid = _regex.IsMatch(value);
    }

    private void UpdateGroupManagedSettings(dynamic? value)
    {
        if (GroupManagedSettings != null)
            foreach (var setting in GroupManagedSettings)
                setting.SetValue(value);
    }
    
    private void UpdateEnabledSettings(T? value)
    {
        if (_enablesSettings != null && EnablesSettings == null)
        {
            EnablesSettings = Parent.Settings.Where(a => _enablesSettings.Contains(a.Name)).ToList();
        }

        if (EnablesSettings is not null && value is bool isEnabled)
        {
            EnablesSettings.ForEach(a => a.EnabledByChanged(isEnabled));
        }
    }
    
    private void SetHideStatus()
    {
        Hide = Advanced && !_showAdvanced || !_isEnabled;
    }
    
    private void ApplyUpdatedSecretValue()
    {
        Value = UpdatedValue;
    }
}