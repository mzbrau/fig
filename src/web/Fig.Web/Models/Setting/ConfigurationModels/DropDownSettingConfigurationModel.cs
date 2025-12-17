using Fig.Contracts.SettingDefinitions;
using Fig.Common.NetStandard.Scripting;

namespace Fig.Web.Models.Setting.ConfigurationModels;

public class DropDownSettingConfigurationModel : SettingConfigurationModel<string>, IDropDownSettingModel
{
    private readonly List<string>? _originalValidValues;
    private ISetting? _lookupKeySetting;
    private bool _hasSubscribedToLookupSetting;
    private List<string>? _validValues;

    public DropDownSettingConfigurationModel(SettingDefinitionDataContract dataContract,
        SettingClientConfigurationModel parent, SettingPresentation presentation)
        : base(dataContract, parent, presentation)
    {
        _originalValidValues = dataContract.ValidValues;
    }

    public List<string> ValidValues
    {
        get
        {
            _validValues ??= GetFilteredValidValues();
            return _validValues;
        }
        set => _validValues = value;
    }
    
    /// <summary>
    /// Gets or sets the display value (without prefix) for the dropdown
    /// </summary>
    public string? DisplayValue
    {
        get => Value;
        set => Value = value ?? string.Empty;
    }

    public override string IconKey => "top_panel_open";

    public override ISetting Clone(SettingClientConfigurationModel parent, bool setDirty, bool isReadOnly)
    {
        var clone = new DropDownSettingConfigurationModel(DefinitionDataContract, parent, Presentation)
        {
            IsDirty = setDirty
        };

        return clone;
    }
    
    private List<string> GetFilteredValidValues()
    {
        if (string.IsNullOrWhiteSpace(LookupKeySettingName) || _originalValidValues == null)
        {
            return _originalValidValues ?? [];
        }

        // Lazy loading: find the lookup setting if we haven't already
        if (_lookupKeySetting == null && !_hasSubscribedToLookupSetting)
        {
            _lookupKeySetting = Parent.Settings.FirstOrDefault(s => s.Name == LookupKeySettingName);
            if (_lookupKeySetting != null)
            {
                _lookupKeySetting.SubscribeToValueChanges(_ => UpdateValidValues());
                _hasSubscribedToLookupSetting = true;
            }
        }

        if (_lookupKeySetting?.GetValue() == null)
        {
            return _originalValidValues;
        }

        var lookupValue = _lookupKeySetting.GetValue()?.ToString();
        if (string.IsNullOrWhiteSpace(lookupValue))
        {
            return _originalValidValues;
        }

        // Filter valid values based on the pattern [LookupValue]ActualValue
        var filteredValues = new List<string>();
        var prefix = $"[{lookupValue}]";
        
        foreach (var validValue in _originalValidValues)
        {
            if (validValue.StartsWith(prefix))
            {
                // Extract the part after [LookupValue]
                var actualValue = validValue.Substring(prefix.Length);
                filteredValues.Add(actualValue);
            }
        }

        return filteredValues;
    }
    
    private void UpdateValidValues()
    {
        _validValues = GetFilteredValidValues();
        
        // If current value is no longer valid after filtering, clear it or find a match
        if (!string.IsNullOrWhiteSpace(Value))
        {
            if (!ValidValues.Contains(Value) && ValidValues.Any())
            {
                // Set to first available option
                Value = ValidValues.FirstOrDefault();
            }
        }
    }
}