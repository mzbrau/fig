using System.Text.RegularExpressions;
using Fig.Contracts;
using Fig.Contracts.SettingDefinitions;
using Fig.Web.Events;

namespace Fig.Web.Models;

public abstract class SettingConfigurationModel
{
    private readonly Func<SettingEventModel, Task<object>> _settingEvent;
    protected SettingDefinitionDataContract _definitionDataContract;
    private bool _isDirty;
    private bool _isValid;
    private dynamic? _originalValue;
    private readonly Regex _regex;

    internal SettingConfigurationModel(SettingDefinitionDataContract dataContract,
        Func<SettingEventModel, Task<object>> settingEvent)
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
        _settingEvent = settingEvent;
        _originalValue = dataContract.Value;
        _isValid = true;
        if (!string.IsNullOrWhiteSpace(ValidationRegex))
        {
            _regex = new Regex(ValidationRegex, RegexOptions.Compiled);
            Validate(GetValue()?.ToString());
        }
    }

    public string Name { get; set; }

    public string Description { get; set; }

    public ValidationType ValidationType { get; set; }

    public string ValidationRegex { get; set; }

    public string ValidationExplanation { get; set; }

    public bool IsSecret { get; set; }

    public string Group { get; set; }

    public int? DisplayOrder { get; set; }

    public bool InSecretEditMode { get; set; }

    public bool IsHistoryVisible { get; set; }

    public List<string> LinkedVerifications { get; set; } = new();

    public bool ResetToDefaultDisabled => _definitionDataContract.DefaultValue == null ||
                                          GetValue() == _definitionDataContract.DefaultValue;

    public bool IsDirty
    {
        get => _isDirty;
        set
        {
            if (_isDirty != value)
            {
                _isDirty = value;
                _settingEvent(new SettingEventModel(Name, SettingEventType.DirtyChanged));
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
                _settingEvent(new SettingEventModel(Name, SettingEventType.ValidChanged));
            }
        }
    }

    public List<SettingHistoryModel> History { get; set; }

    public void SetUpdatedSecretValue()
    {
        if (IsUpdatedSecretValueValid())
        {
            ApplyUpdatedSecretValue();
            InSecretEditMode = false;
            IsDirty = true;
        }
    }

    public void MarkAsSaved()
    {
        IsDirty = false;
        _originalValue = GetValue();
    }

    internal abstract SettingConfigurationModel Clone(Func<SettingEventModel, Task<object>> stateChanged);

    public void ValueChanged(string value)
    {
        IsDirty = _originalValue?.ToString() != value;
        Validate(value);
    }

    public void UndoChanges()
    {
        SetValue(_originalValue);
        ValueChanged(GetValue().ToString());
    }

    public async Task ShowHistory()
    {
        IsHistoryVisible = !IsHistoryVisible;

        if (IsHistoryVisible)
        {
            var settingEvent = new SettingEventModel(Name, SettingEventType.SettingHistoryRequested);
            var result = await _settingEvent(settingEvent);
            if (result is List<SettingHistoryModel> history)
                History = history;
        }
    }

    public abstract dynamic GetValue();

    public void ResetToDefault()
    {
        if (_definitionDataContract.DefaultValue != null)
        {
            SetValue(_definitionDataContract.DefaultValue);
            ValueChanged(GetValue().ToString());
        }
    }

    protected abstract bool IsUpdatedSecretValueValid();

    protected abstract void ApplyUpdatedSecretValue();

    protected abstract void SetValue(dynamic value);

    protected void Validate(string value)
    {
        if (_regex != null)
            IsValid = _regex.IsMatch(value);
    }
}