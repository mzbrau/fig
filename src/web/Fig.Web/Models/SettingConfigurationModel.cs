using System.Text.RegularExpressions;
using Fig.Contracts;
using Fig.Contracts.SettingDefinitions;
using Fig.Web.Events;

namespace Fig.Web.Models;

public abstract class SettingConfigurationModel<T> : ISetting
{
    protected readonly SettingDefinitionDataContract _definitionDataContract;
    private readonly Regex? _regex;
    private bool _isDirty;
    private bool _isValid;
    private dynamic? _originalValue;

    private T _value;

    internal SettingConfigurationModel(SettingDefinitionDataContract dataContract,
        SettingClientConfigurationModel parent)
    {
        _definitionDataContract = dataContract;
        Name = dataContract.Name;
        Description = dataContract.Description;
        ValidationType = dataContract.ValidationType;
        ValidationRegex = dataContract.ValidationRegex;
        ValidationExplanation = string.IsNullOrWhiteSpace(dataContract.ValidationExplanation)
            ? $"Did not match validation regex ({ValidationRegex})"
            : dataContract.ValidationExplanation;
        IsSecret = dataContract.IsSecret;
        Group = dataContract.Group;
        DisplayOrder = dataContract.DisplayOrder;
        Parent = parent;
        _value = dataContract.Value;
        _originalValue = dataContract.Value;
        _isValid = true;
        if (!string.IsNullOrWhiteSpace(ValidationRegex))
        {
            _regex = new Regex(ValidationRegex, RegexOptions.Compiled);
            Validate(dataContract.Value?.ToString());
        }
    }

    public T DefaultValue { get; set; }

    public T UpdatedValue { get; set; }

    public ValidationType ValidationType { get; }

    public string ValidationRegex { get; }

    public string ValidationExplanation { get; }

    public bool IsSecret { get; }

    public bool InSecretEditMode { get; set; }

    public T Value
    {
        get => _value;
        set
        {
            if (!EqualityComparer<T>.Default.Equals(_value, value))
            {
                _value = value;
                EvaluateDirty(_value);
                UpdateGroupManagedSettings(_value);
            }
        }
    }

    public bool IsReadOnly => IsGroupManaged;

    public string Name { get; }

    public string Description { get; }

    public string Group { get; set; }

    public int? DisplayOrder { get; set; }

    public bool IsHistoryVisible { get; set; }

    public bool IsDeleted { get; set; }

    public List<string> LinkedVerifications { get; set; } = new();

    public bool ResetToDefaultDisabled => _definitionDataContract.DefaultValue == null ||
                                          GetValue() == _definitionDataContract.DefaultValue;

    public List<ISetting>? GroupManagedSettings { get; set; } = new();

    public SettingClientConfigurationModel Parent { get; set; }

    public bool IsDirty
    {
        get => _isDirty;
        set
        {
            if (_isDirty != value)
            {
                _isDirty = value;
                Parent.SettingEvent(new SettingEventModel(Name, SettingEventType.DirtyChanged));
            }
        }
    }

    public bool IsNotDirty => !IsDirty;

    public bool IsValid
    {
        get => _isValid;
        set
        {
            if (_isValid != value)
            {
                _isValid = value;
                Parent.SettingEvent(new SettingEventModel(Name, SettingEventType.ValidChanged));
            }
        }
    }

    public bool IsGroupManaged { get; set; }

    public List<SettingHistoryModel>? History { get; set; }

    public void MarkAsSaved()
    {
        IsDirty = false;
        _originalValue = GetValue();
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

    public void UndoChanges()
    {
        Value = _originalValue;
    }

    public async Task ShowHistory()
    {
        IsHistoryVisible = !IsHistoryVisible;

        if (!IsHistoryVisible)
            return;

        if (GroupManagedSettings.Any())
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

    public abstract dynamic GetValue();

    public void ResetToDefault()
    {
        if (_definitionDataContract.DefaultValue != null)
            Value = _definitionDataContract.DefaultValue;
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

    public void SetUpdatedSecretValue()
    {
        if (IsUpdatedSecretValueValid())
        {
            ApplyUpdatedSecretValue();
            InSecretEditMode = false;
            IsDirty = true;
        }
    }

    public void ValueChanged(string value)
    {
        Validate(value);
    }

    private void ApplyUpdatedSecretValue()
    {
        Value = UpdatedValue;
    }

    protected virtual bool IsUpdatedSecretValueValid()
    {
        return true;
    }

    private void EvaluateDirty(dynamic value)
    {
        IsDirty = _originalValue != value;
    }

    private void UpdateGroupManagedSettings(dynamic value)
    {
        if (GroupManagedSettings != null)
            foreach (var setting in GroupManagedSettings)
                setting.SetValue(value);
    }

    private void Validate(string value)
    {
        if (_regex != null)
            IsValid = _regex.IsMatch(value);
    }
}