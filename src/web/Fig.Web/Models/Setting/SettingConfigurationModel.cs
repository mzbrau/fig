using System.Data.Common;
using System.Text.RegularExpressions;
using Fig.Contracts;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Web.Events;
using Fig.Web.ExtensionMethods;
using Fig.Web.Models.Setting.ConfigurationModels.DataGrid;
using Humanizer;
using Microsoft.AspNetCore.Components;

namespace Fig.Web.Models.Setting;

public abstract class SettingConfigurationModel<T> : ISetting
{
    private const string Transparent = "#00000000";
    protected readonly SettingDefinitionDataContract DefinitionDataContract;
    private readonly IList<string>? _enablesSettings;
    private bool _isDirty;
    private bool _isValid;
    private bool _showAdvanced;
    private bool _isEnabled = true;
    private bool _matchesFilter = true;

    private T? _value;
    protected T? OriginalValue;

    internal SettingConfigurationModel(SettingDefinitionDataContract dataContract,
        SettingClientConfigurationModel parent)
    {
        Name = dataContract.Name;
        Description = (MarkupString)dataContract.Description.ToHtml();
        SupportsLiveUpdate = dataContract.SupportsLiveUpdate;
        ValidationRegex = dataContract.ValidationRegex;
        ValidationExplanation = string.IsNullOrWhiteSpace(dataContract.ValidationExplanation)
            ? $"Did not match validation regex ({ValidationRegex})"
            : dataContract.ValidationExplanation;
        IsSecret = dataContract.IsSecret;
        Group = dataContract.Group;
        DisplayOrder = dataContract.DisplayOrder ?? int.MaxValue;
        Parent = parent;
        Advanced = dataContract.Advanced;
        JsonSchemaString = dataContract.JsonSchema;
        EditorLineCount = dataContract.EditorLineCount;
        CategoryColor = dataContract.CategoryColor ?? Transparent;
        CategoryName = dataContract.CategoryName;
        _enablesSettings = dataContract.EnablesSettings;
        Console.WriteLine($"Loading {Name}. Color {CategoryColor} Cateogory:{CategoryName}");
        DefinitionDataContract = dataContract;
        _value = (T?)dataContract.GetEditableValue();
        OriginalValue = (T?)dataContract.GetEditableValue();
        LastChanged = dataContract.LastChanged?.ToLocalTime();
        _isValid = true;

        if (!string.IsNullOrWhiteSpace(ValidationRegex))
        {
            Validate(dataContract.Value?.GetValue()?.ToString() ?? string.Empty);
        }
    }

    public bool IsSecret { get; }

    public T? UpdatedValue { get; set; }

    public string ValidationExplanation { get; protected set; }

    public bool InSecretEditMode { get; set; }

    public bool IsCompactView { get; set; }
    
    public bool IsEnabledByOtherSetting { get; private set; }
    
    public DateTime? LastChanged { get; }

    public string LastChangedRelative => LastChanged is null ? "Never" : LastChanged.Humanize();

    public bool SupportsLiveUpdate { get; }

    public string? CategoryColor { get; }
    
    public string? CategoryName { get; }

    public string? ValidationRegex { get; }

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

    public bool Advanced { get; }

    public string? JsonSchemaString { get; set; }

    public string Name { get; }

    public MarkupString Description { get; }

    public string? Group { get; }

    public int? DisplayOrder { get; }
    
    public int? EditorLineCount { get; }
    
    public List<ISetting>? EnablesSettings { get; private set; }

    public DataGridConfigurationModel? DataGridConfiguration { get; set; }

    public SettingClientConfigurationModel Parent { get; }

    public string ParentName => Parent.Name;

    public string? ParentInstance => Parent.Instance;

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

    public string StringValue => GetStringValue();

    public virtual string GetStringValue()
    {
        if (string.IsNullOrWhiteSpace(Value?.ToString()))
        {
            return "<NOT SET>";
        }

        return IsSecret ? 
            "**********" : 
            Value.ToString()!.Truncate(200);
    }

    public void ToggleCompactView()
    {
        IsCompactView = !IsCompactView;
    }

    public virtual void MarkAsSaved()
    {
        IsDirty = false;
        OriginalValue = (T?)GetValue(true);
    }

    public void ShowAdvancedChanged(bool showAdvanced)
    {
        _showAdvanced = showAdvanced;
        SetHideStatus();
    }

    public void EnabledByChanged(bool isEnabled)
    {
        IsEnabledByOtherSetting = true;
        _isEnabled = isEnabled;
        SetHideStatus();
    }

    public void FilterChanged(string filter)
    {
        _matchesFilter = string.IsNullOrWhiteSpace(filter) || 
                         Name.ToLower().Contains(filter.ToLower());
        SetHideStatus();
    }

    public void SetLinkedVerifications(List<string> verificationNames)
    {
        LinkedVerifications = verificationNames;
    }

    public abstract ISetting Clone(SettingClientConfigurationModel parent, bool setDirty);

    public void SetValue(object? value)
    {
        Value = (T?)value;
    }

    public virtual SettingValueBaseDataContract? GetValueDataContract()
    {
        return ValueDataContractFactory.CreateContract(Value, typeof(T));
    }

    protected virtual object? GetValue(bool formatAsT = false)
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

    public virtual void ResetToDefault()
    {
        if (DefinitionDataContract.DefaultValue != null)
            Value = (T?)DefinitionDataContract.GetDefaultValue();
    }

    public void SetGroupManagedSettings(List<ISetting> groupManagedSettings)
    {
        GroupManagedSettings = groupManagedSettings;
        foreach (var setting in GroupManagedSettings)
            setting.IsGroupManaged = true;
    }

    public async Task RequestSettingClientIsShown(string? settingToSelect)
    {
        if (settingToSelect is not null)
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

    public void ValueChanged(string? value)
    {
        Validate(value);
    }

    protected virtual bool IsUpdatedSecretValueValid()
    {
        return true;
    }

    protected virtual void EvaluateDirty(T? value)
    {
        if (OriginalValue is null && value is null)
            IsDirty = false;
        else
            IsDirty = OriginalValue?.Equals(value) != true;
    }

    protected virtual void Validate(string? value)
    {
        if (value is not null && ValidationRegex is not null)
        {
            try
            {
                IsValid = Regex.IsMatch(value, ValidationRegex);
            }
            catch (RegexMatchTimeoutException)
            {
                IsValid = false;
            }
        }
    }

    private void UpdateGroupManagedSettings(object? value)
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
        Hide = IsAdvancedAndAdvancedHidden() || IsNotEnabled() || IsFilteredOut();

        bool IsAdvancedAndAdvancedHidden() => Advanced && !_showAdvanced;

        bool IsNotEnabled() => !_isEnabled;

        bool IsFilteredOut() => !_matchesFilter;
    }
    
    private void ApplyUpdatedSecretValue()
    {
        Value = UpdatedValue;
    }
}