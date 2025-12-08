using System.Globalization;
using System.Text.RegularExpressions;
using Fig.Client.Abstractions.Data;
using Fig.Common.NetStandard.Json;
using Fig.Common.NetStandard.Scripting;
using Fig.Contracts;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Web.Events;
using Fig.Web.ExtensionMethods;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;

namespace Fig.Web.Models.Setting;

public abstract class SettingConfigurationModel<T> : ISetting, ISearchableSetting
{
    private const string Transparent = "#00000000";
    protected readonly SettingDefinitionDataContract DefinitionDataContract;
    protected readonly SettingPresentation Presentation;
    private readonly IList<string>? _enablesSettings;
    private bool _isReadOnly;
    private bool _isDirty;
    private bool _isValid;
    private bool _showAdvanced;
    private bool _isEnabled = true;
    private bool _matchesFilter = true;
    private bool _matchesCategoryFilter = true;
    private bool _isVisibleFromScript;
    private static readonly SettingFilterParser _filterParser = new();
    private bool _showModifiedOnly;
    private readonly string _lowerName;
    private readonly string _lowerParentInstance;
    private readonly string _lowerDescription;
    private readonly string _lowerParentName;
    private readonly Dictionary<int, string> _cachedStringValues = new();
    private ISetting? _baseSetting;
    private readonly List<Action<ActionType>> _instanceSubscriptions = new();
    protected bool _hasBeenValidated;
    private bool _hasDescriptionBeenConverted;
    private MarkupString _description;
    private readonly string _rawMarkdownDescription;

    private T? _value;
    protected T? OriginalValue;

    internal SettingConfigurationModel(SettingDefinitionDataContract dataContract,
        SettingClientConfigurationModel parent, SettingPresentation presentation)
    {
        Presentation = presentation;
        Name = dataContract.Name;
        DisplayName = Name.SplitCamelCase();
        _rawMarkdownDescription = dataContract.Description;
        RawDescription = dataContract.Description;
        TruncatedDescription = dataContract.Description.StripImagesAndSimplifyLinks().Truncate(90);
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
        CategoryName = dataContract.CategoryName ?? string.Empty;
        Classification = dataContract.Classification;
        DisplayScript = dataContract.DisplayScript;
        IsExternallyManaged = dataContract.IsExternallyManaged;
        LookupKeySettingName = dataContract.LookupKeySettingName;
        _enablesSettings = dataContract.EnablesSettings;
        DefinitionDataContract = dataContract;
        _isReadOnly = Presentation.IsReadOnly || dataContract.IsExternallyManaged;
        _value = (T?)dataContract.GetEditableValue(this);
        OriginalValue = (T?)dataContract.GetEditableValue(this);
        LastChanged = dataContract.LastChanged?.ToLocalTime();
        ScrollId = $"{parent.Name}-{parent.Instance}-{Name}";
        _isValid = true;
        Indent = dataContract.Indent;
        DependsOnProperty = dataContract.DependsOnProperty;
        DependsOnValidValues = dataContract.DependsOnValidValues;
        Heading = dataContract.Heading != null ? new HeadingModel(dataContract.Heading) : null;
        
        UpdateVisibility();
        _isVisibleFromScript = Hidden;
        
        _lowerName = DisplayName.ToLowerInvariant();
        _lowerParentInstance = parent.Instance?.ToLowerInvariant() ?? string.Empty;
        _lowerDescription = TruncatedDescription.ToLowerInvariant();
        _lowerParentName = parent.Name.ToLowerInvariant();
    }

    public bool IsSecret { get; }

    public T? UpdatedValue { get; set; }

    public string ValidationExplanation { get; set; }

    public bool InSecretEditMode { get; set; }
    
    public bool? EnvironmentSpecific => DefinitionDataContract.EnvironmentSpecific;

    public bool IsCompactView { get; set; }
    
    public bool IsEnabledByOtherSetting { get; private set; }
    
    public DateTime? LastChanged { get; }

    public string LastChangedRelative => LastChanged is null ? "Never" : LastChanged.Humanize();

    public bool SupportsLiveUpdate { get; }

    public string CategoryColor { get; set; }
    
    public string TruncatedDescription { get; }

    public abstract string IconKey { get; }

    public string CategoryName { get; set; }
    
    public Classification Classification { get; set; }

    public string? ValidationRegex { get; }
    
    public string? DisplayScript { get; }
    
    public bool IsExternallyManaged { get; }
    
    public string? LookupKeySettingName { get; }
    
    public int? Indent { get; set; }
    
    public string? DependsOnProperty { get; set; }
    
    public IList<string>? DependsOnValidValues { get; set; }
    
    public HeadingModel? Heading { get; }

    public ISetting? BaseSetting
    {
        get => _baseSetting;
        set
        {
            _baseSetting = value;
            UpdateBaseValueComparison();
            if (_baseSetting is not null)
            {
                _baseSetting.SubscribeToValueChanges(HandleInstanceAction);
                _baseSetting.IsBaseSetting = true;
            }
        }
    }

    public Type ValueType => typeof(T);

    public bool? MatchesBaseValue { get; private set; }

    public T? Value
    {
        get
        {
            // Lazy validation: validate on first access
            if (!_hasBeenValidated && !string.IsNullOrWhiteSpace(ValidationRegex))
            {
                Validate(Convert.ToString(_value, CultureInfo.InvariantCulture) ?? string.Empty);
                _hasBeenValidated = true;
            }
            return _value;
        }
        set
        {
            if (!EqualityComparer<T>.Default.Equals(_value, value))
            {
                _value = value;
                _hasBeenValidated = false; // Reset validation flag when value changes
                _cachedStringValues.Clear();
                EvaluateDirty(_value);
                UpdateGroupManagedSettings(_value);
                UpdateEnabledSettings(_value);
                UpdateDependentSettings();
                UpdateBaseValueComparison();
                NotifySubscribers(ActionType.ValueChanged);
                if (!string.IsNullOrWhiteSpace(DisplayScript))
                    Task.Run(async () => await Parent.SettingEvent(new SettingEventModel(Name, SettingEventType.RunScript, DisplayScript)));
            }
        }
    }

    public bool IsReadOnly => _isReadOnly || IsGroupManaged;

    public bool HasDisplayScript => !string.IsNullOrWhiteSpace(DisplayScript);

    public string ScrollId { get; }
    public bool Advanced { get; set; }

    public string? JsonSchemaString { get; set; }

    public string Name { get; }
    
    public string DisplayName { get; }

    public MarkupString Description
    {
        get
        {
            // Lazy load HTML description on first access to improve initial load performance
            if (!_hasDescriptionBeenConverted)
            {
                _description = (MarkupString)ConvertMarkdownToHtmlWithTimeout(_rawMarkdownDescription, TimeSpan.FromSeconds(2));
                _hasDescriptionBeenConverted = true;
            }
            return _description;
        }
    }
    
    public string RawDescription { get; }

    public string? Group { get; }

    public int? DisplayOrder { get; set; }
    
    public int? EditorLineCount { get; set; }
    
    public List<ISetting>? EnablesSettings { get; private set; }

    public IDataGridConfigurationModel? DataGridConfiguration { get; set; }

    public SettingClientConfigurationModel Parent { get; }

    public string ParentName => Parent.Name;

    public string ParentInstance => Parent.Instance ?? string.Empty;

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
    
    public bool ShouldAnimateVisibilityChanges { get; set; } = true;

    public bool IsVisible => !Hidden;

    public string StringValue => GetStringValue();
    
    public string TruncatedStringValue => GetStringValue(80);
    
    public bool IsBaseSetting { get; set; }
    
    public string? ScheduledChangeDescription { get; set; }

    public virtual string GetStringValue(int maxLength = 200)
    {
        if (_cachedStringValues.TryGetValue(maxLength, out var value))
        {
            return value;
        }
        
        if (string.IsNullOrWhiteSpace(Value?.ToString()))
        {
            var notSet = "<NOT SET>";
            _cachedStringValues[maxLength] = notSet;
            return notSet;
        }

        var val = IsSecret ? 
            "**********" : 
            Value.ToString()!.Truncate(maxLength);
        _cachedStringValues[maxLength] = val;
        return val;
    }
    
    public void Expand()
    {
        if (IsCompactView)
            ToggleCompactView(false);
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

    public virtual void Initialize()
    {
        // Lazy validation and description conversion are now performed on first access
        // This improves initial load performance
        // Trigger description conversion on initialize to pre-load it
        if (!_hasDescriptionBeenConverted)
        {
            _ = Description; // Access property to trigger lazy load
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
        UpdateVisibility();
    }

    public void EnabledByChanged(bool isEnabled)
    {
        IsEnabledByOtherSetting = true;
        _isEnabled = isEnabled;
        UpdateVisibility();
    }

    public void FilterChanged(string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            _matchesFilter = true;
            UpdateVisibility();
            return;
        }

        var criteria = _filterParser.Parse(filter);
        
        // If all criteria are empty, show all settings
        if (criteria.IsEmpty)
        {
            _matchesFilter = true;
            UpdateVisibility();
            return;
        }

        // Check each criterion
        var matchesAdvanced = criteria.Advanced == null || Advanced == criteria.Advanced;
        var matchesCategory = criteria.Category == null || CategoryName.Contains(criteria.Category, StringComparison.OrdinalIgnoreCase);
        // Classification is an enum/property on the setting; compare against the setting's Classification string
        var matchesClassification = criteria.Classification == null ||
                        Classification.ToString().Contains(criteria.Classification, StringComparison.OrdinalIgnoreCase);
        var matchesSecret = criteria.Secret == null || IsSecret == criteria.Secret;
        var matchesValid = criteria.Valid == null || IsValid == criteria.Valid;
        var matchesDirty = criteria.Modified == null || IsDirty == criteria.Modified;

        // Check general search terms (match any)
        var matchesGeneralSearch = !criteria.GeneralSearchTerms.Any() || 
                                   criteria.GeneralSearchTerms.Any(term =>
                                       Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                                       Description.ToString().Contains(term, StringComparison.OrdinalIgnoreCase) ||
                                       StringValue.Contains(term, StringComparison.OrdinalIgnoreCase));

        // All property criteria must match (AND), and at least one general search term must match if present
        _matchesFilter = matchesAdvanced &&
                         matchesCategory &&
                         matchesClassification &&
                         matchesSecret &&
                         matchesValid &&
                         matchesDirty &&
                         matchesGeneralSearch;
        UpdateVisibility();
    }
    
    public void CategoryFilterChanged(string? categoryName)
    {
        _matchesCategoryFilter = string.IsNullOrWhiteSpace(categoryName) ||
                                 CategoryName.Equals(categoryName, StringComparison.OrdinalIgnoreCase);
        UpdateVisibility();
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
        UpdateVisibility();
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
        {
            Value = (T?)DefinitionDataContract.GetDefaultValue();
            Validate(Convert.ToString(Value, CultureInfo.InvariantCulture) ?? string.Empty);
        }
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
        UpdateDependentSettings();
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

    public void SubscribeToValueChanges(Action<ActionType> instanceSubscription)
    {
        _instanceSubscriptions.Add(instanceSubscription);
    }

    public void PushValueToBase()
    {
        if (BaseSetting is not null)
        {
            var update = JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(Value, JsonSettings.FigDefault), JsonSettings.FigDefault);
            BaseSetting.SetValue(update);
        }
    }

    public void PullValueFromBase()
    {
        if (BaseSetting is not null)
        {
            var update = JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(BaseSetting.GetValue(true), JsonSettings.FigDefault), JsonSettings.FigDefault);
            Value = update;
        }
    }

    public void PushValueToInstances()
    {
        NotifySubscribers(ActionType.TakeBaseValue);
    }

    public void NotifyAboutScheduledChange(SettingValueBaseDataContract? changeSetValue, DateTime changeExecuteAtUtc,
        string changeRequestingUser, string? changeSetChangeMessage)
    {
        var value = changeSetValue?.GetValue();
        if (value is List<Dictionary<string, object?>> dataGrid)
        {
            value = dataGrid.ToDataGridStringValue(5, false);
        }
        ScheduledChangeDescription = $"Scheduled to change to '{value}' on {changeExecuteAtUtc.ToLocalTime()} as requested by {changeRequestingUser}. {changeSetChangeMessage}";
    }

    public void ClearScheduledChange()
    {
        ScheduledChangeDescription = null;
    }

    public void FilterByBaseValueMatch(bool showModifiedOnly)
    {
        _showModifiedOnly = showModifiedOnly;
        UpdateVisibility();
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
                _hasBeenValidated = true;
            }
            catch (RegexMatchTimeoutException)
            {
                IsValid = false;
                _hasBeenValidated = true;
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
    
    protected void NotifySubscribers(ActionType actionType)
    {
        foreach (var subscription in _instanceSubscriptions)
        {
            subscription(actionType);
        }
    }

    private void HandleInstanceAction(ActionType actionType)
    {
        if (actionType == ActionType.ValueChanged)
        {
            UpdateBaseValueComparison();
        }
        else if (actionType == ActionType.TakeBaseValue)
        {
            PullValueFromBase();
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
    
    private void UpdateDependentSettings()
    {
        // Find all settings that depend on this setting
        var dependentSettings = Parent?.Settings?.Where(s => s.DependsOnProperty == Name).ToList() ?? [];
        foreach (var dependentSetting in dependentSettings)
        {
            dependentSetting.UpdateVisibility();
        }
        
        // Trigger UI refresh if any dependent settings were updated
        if (dependentSettings.Any() && Parent is not null)
        {
            Task.Run(async () => await Parent.SettingEvent(new SettingEventModel(Name, SettingEventType.DependencyVisibilityChanged)));
        }
    }
    
    public void UpdateVisibility()
    {
        Hidden = !_isVisibleFromScript || 
                IsAdvancedAndAdvancedHidden() || 
                IsNotEnabled() || 
                IsFilteredOut() ||
                IsCategoryFilteredOut() ||
                IsHiddenByBaseValueMatch() ||
                IsHiddenByDependency();

        bool IsAdvancedAndAdvancedHidden() => Advanced && !_showAdvanced;
        bool IsNotEnabled() => !_isEnabled;
        bool IsFilteredOut() => !_matchesFilter;
        bool IsCategoryFilteredOut() => !_matchesCategoryFilter;
        bool IsHiddenByBaseValueMatch() => _showModifiedOnly && MatchesBaseValue == true;
        bool IsHiddenByDependency() => !IsVisibleBasedOnDependency();
    }
    
    private bool IsVisibleBasedOnDependency()
    {
        // If no dependency is defined, the setting is always visible
        if (string.IsNullOrEmpty(DependsOnProperty) || DependsOnValidValues == null || !DependsOnValidValues.Any())
            return true;
            
        // Find the dependent setting
        var dependentSetting = Parent.Settings.FirstOrDefault(s => s.Name == DependsOnProperty);
        if (dependentSetting == null)
            return true; // If dependent setting not found, show by default
            
        // Check if the dependent setting's current value is in the list of valid values
        var dependentValue = dependentSetting.StringValue;
        return DependsOnValidValues.Contains(dependentValue);
    }
    
    private void ApplyUpdatedSecretValue()
    {
        Value = UpdatedValue;
    }

    public bool IsSearchMatch(string? clientToken, string? settingToken, string? descriptionToken, string? instanceToken, string? valueToken, List<string> generalTokens)
    {
        var match = true;
        
        if (generalTokens.Any())
            match = match && generalTokens.All(token =>
                _lowerName.Contains(token) ||
                _lowerParentName.Contains(token) ||
                _lowerParentInstance.Contains(token));

        if (clientToken != null)
            match = match && _lowerParentName.Contains(clientToken);
        
        if (settingToken != null) 
            match = match && _lowerName.Contains(settingToken);

        if (descriptionToken != null)
            match = match && _lowerDescription.Contains(descriptionToken);
        
        if (instanceToken != null) 
            match = match && _lowerParentInstance.Contains(instanceToken);

        if (valueToken != null)
            match = match && TruncatedStringValue.ToLowerInvariant().Contains(valueToken);

        return match;
    }
    
    /// <summary>
    /// Converts markdown to HTML with a timeout to avoid Markdig bug that can cause infinite loops.
    /// If the conversion takes longer than the timeout, returns the raw markdown text instead.
    /// </summary>
    /// <param name="markdown">The markdown text to convert</param>
    /// <param name="timeout">Maximum time to wait for conversion</param>
    /// <returns>HTML string, or raw markdown if timeout occurred</returns>
    private static string ConvertMarkdownToHtmlWithTimeout(string markdown, TimeSpan timeout)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        string? result = null;
        var conversionTask = Task.Run(() =>
        {
            try
            {
                return markdown.ToHtml();
            }
            catch
            {
                return markdown; // Fallback to raw markdown on error
            }
        });

        if (conversionTask.Wait(timeout))
        {
            result = conversionTask.Result;
        }
        else
        {
            // Timeout occurred - return raw markdown instead
            result = markdown;
        }

        return result ?? markdown;
    }
}