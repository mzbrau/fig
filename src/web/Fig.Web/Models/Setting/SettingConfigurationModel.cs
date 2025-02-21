using System.Globalization;
using System.Text.RegularExpressions;
using Fig.Common.NetStandard.Json;
using Fig.Contracts;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Web.Events;
using Fig.Web.ExtensionMethods;
using Fig.Web.Models.Setting.ConfigurationModels.DataGrid;
using Fig.Web.Utils;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;

namespace Fig.Web.Models.Setting;

public abstract class SettingConfigurationModel<T> : ISetting
{
    private const string Transparent = "#00000000";
    protected readonly SettingDefinitionDataContract DefinitionDataContract;
    private readonly IList<string>? _enablesSettings;
    private bool _isReadOnly;
    private bool _isDirty;
    private bool _isValid;
    private bool _showAdvanced;
    private bool _isEnabled = true;
    private bool _matchesFilter = true;
    private bool _isVisibleFromScript;
    private bool _showModifiedOnly;
    private ISetting? _baseSetting;
    protected Action? _valueChanged;

    private T? _value;
    protected T? OriginalValue;

    internal SettingConfigurationModel(SettingDefinitionDataContract dataContract,
        SettingClientConfigurationModel parent, bool isReadOnly)
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
        DisplayScript = dataContract.DisplayScript;
        IsExternallyManaged = dataContract.IsExternallyManaged;
        _enablesSettings = dataContract.EnablesSettings;
        DefinitionDataContract = dataContract;
        _isReadOnly = isReadOnly || dataContract.IsExternallyManaged;
        _value = (T?)dataContract.GetEditableValue(this);
        OriginalValue = (T?)dataContract.GetEditableValue(this);
        LastChanged = dataContract.LastChanged?.ToLocalTime();
        _isValid = true;
        
        SetHideStatus();
        _isVisibleFromScript = Hidden;
    }

    public bool IsSecret { get; }

    public T? UpdatedValue { get; set; }

    public string ValidationExplanation { get; set; }

    public bool InSecretEditMode { get; set; }

    public bool IsCompactView { get; set; }
    
    public bool IsEnabledByOtherSetting { get; private set; }
    
    public DateTime? LastChanged { get; }

    public string LastChangedRelative => LastChanged is null ? "Never" : LastChanged.Humanize();

    public bool SupportsLiveUpdate { get; }

    public string? CategoryColor { get; set; }
    
    public string? CategoryName { get; set; }

    public string? ValidationRegex { get; }
    
    public string? DisplayScript { get; }
    
    public bool IsExternallyManaged { get; }

    public ISetting? BaseSetting
    {
        get => _baseSetting;
        set
        {
            _baseSetting = value;
            UpdateBaseValueComparison();
            if (_baseSetting is not null)
            {
                _baseSetting.SubscribeToValueChanges(UpdateBaseValueComparison);
            }
        }
    }

    public Type ValueType => typeof(T);

    public bool? MatchesBaseValue { get; private set; }

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
                UpdateBaseValueComparison();
                _valueChanged?.Invoke();
                if (!string.IsNullOrWhiteSpace(DisplayScript))
                    Task.Run(async () => await Parent.SettingEvent(new SettingEventModel(Name, SettingEventType.RunScript, DisplayScript)));
            }
        }
    }

    public bool IsReadOnly => _isReadOnly || IsGroupManaged;

    public bool HasDisplayScript => !string.IsNullOrWhiteSpace(DisplayScript);

    public bool Advanced { get; set; }

    public string? JsonSchemaString { get; set; }

    public string Name { get; }

    public MarkupString Description { get; }

    public string? Group { get; }

    public int? DisplayOrder { get; set; }
    
    public int? EditorLineCount { get; set; }
    
    public List<ISetting>? EnablesSettings { get; private set; }

    public DataGridConfigurationModel? DataGridConfiguration { get; set; }

    public SettingClientConfigurationModel Parent { get; }

    public string ParentName => Parent.Name;

    public string? ParentInstance => Parent.Instance;

    public bool IsValid
    {
        get => _isValid;
        set
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

    public bool Hidden { get; private set; }

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

    public void ToggleCompactView(bool controlPressed)
    {
        if (controlPressed)
        {
            Parent.SetCompactViewForCategory(CategoryName, !IsCompactView);
        }
        else
        {
            IsCompactView = !IsCompactView;
        }
    }

    public void Initialize()
    {
        if (!string.IsNullOrWhiteSpace(ValidationRegex))
        {
            Validate(Convert.ToString(Value, CultureInfo.InvariantCulture) ?? string.Empty);
        }

        RunDisplayScript();
    }

    public void RunDisplayScript()
    {
        if (HasDisplayScript)
            Task.Run(async () => await Parent.SettingEvent(new SettingEventModel(Name, SettingEventType.RunScript, DisplayScript!)));
    }

    public virtual void MarkAsSaved()
    {
        IsDirty = false;
        OriginalValue = JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(Value, JsonSettings.FigDefault), JsonSettings.FigDefault);
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
                         Name.ToLower().Contains(filter.ToLower()) || 
                         Description.ToString().ToLower().Contains(filter.ToLower()) ||
                         StringValue.ToLower().Contains(filter.ToLower());
        SetHideStatus();
    }

    public void SetLinkedVerifications(List<string> verificationNames)
    {
        LinkedVerifications = verificationNames;
    }

    public abstract ISetting Clone(SettingClientConfigurationModel parent, bool setDirty, bool isReadOnly);

    public void SetValue(object? value)
    {
        Value = (T?)value;
    }

    public virtual SettingValueBaseDataContract? GetValueDataContract()
    {
        return ValueDataContractFactory.CreateContract(Value, typeof(T));
    }

    public virtual object? GetValue(bool formatAsT = false)
    {
        return Value;
    }

    public void UndoChanges()
    {
        Value = OriginalValue;
    }

    public void SetVisibilityFromScript(bool isVisible)
    {
        _isVisibleFromScript = isVisible;
        SetHideStatus();
    }

    public void SetReadOnly(bool isReadOnly)
    {
        _isReadOnly = isReadOnly;
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
            IsValid = true;
        }
        else
        {
            IsValid = false;
#pragma warning disable CS4014
            Parent.SettingEvent(new SettingEventModel(Name, "Password values do not match", SettingEventType.ShowErrorNotification));
#pragma warning restore CS4014
        }
    }

    public void ValueChanged(string? value)
    {
        Validate(value);
    }

    public virtual string GetChangeDiff()
    {
        var originalVal = OriginalValue?.ToString() ?? string.Empty;
        var currentVal = Value?.ToString() ?? string.Empty;

        if (string.IsNullOrEmpty(originalVal))
            return currentVal;

        if (string.IsNullOrEmpty(currentVal))
            return $"- {currentVal}";

        return $"-  {originalVal}{Environment.NewLine}+ {currentVal}";
    }

    public void Unlock()
    {
        SetReadOnly(false);
    }

    public void SubscribeToValueChanges(Action valueChanged)
    {
        _valueChanged = valueChanged;
    }

    public void FilterByBaseValueMatch(bool showModifiedOnly)
    {
        _showModifiedOnly = showModifiedOnly;
        SetHideStatus();
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
            IsDirty = !EqualityComparer<T>.Default.Equals(OriginalValue, value);
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
    
    protected void UpdateBaseValueComparison()
    {
        if (BaseSetting == null)
        {
            MatchesBaseValue = null;
            return;
        }

        var baseValue = BaseSetting.GetStringValue();
        var ownValue = GetStringValue();
        MatchesBaseValue = ownValue == baseValue; // Note that this will be an approximation for non-string types
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
        Hidden = !_isVisibleFromScript || 
                IsAdvancedAndAdvancedHidden() || 
                IsNotEnabled() || 
                IsFilteredOut() ||
                IsHiddenByBaseValueMatch();

        bool IsAdvancedAndAdvancedHidden() => Advanced && !_showAdvanced;
        bool IsNotEnabled() => !_isEnabled;
        bool IsFilteredOut() => !_matchesFilter;
        bool IsHiddenByBaseValueMatch() => _showModifiedOnly && MatchesBaseValue == true;
    }
    
    private void ApplyUpdatedSecretValue()
    {
        Value = UpdatedValue;
    }
}