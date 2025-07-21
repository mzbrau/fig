using Fig.Common.NetStandard.Data;
using Fig.Common.NetStandard.Json;
using Fig.Contracts.SettingDefinitions;
using Fig.Datalayer.BusinessEntities.SettingValues;
using Newtonsoft.Json;

namespace Fig.Datalayer.BusinessEntities;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by nhibernate.
public class SettingBusinessEntity
{
    private string? _defaultValueAsJson;
    private string? _validValuesAsJson;
    private string? _enablesSettingsAsJson;
    private string? _dependsOnValidValuesAsJson;
    private string? _headingAsJson;

    public virtual Guid Id { get; init; }

    public virtual string Name { get; set; } = default!;

    public virtual string Description { get; set; } = default!;

    public virtual bool IsSecret { get; set; }

    public virtual Type? ValueType { get; set; }
    
    public virtual SettingValueBaseBusinessEntity? Value { get; set; }

    public virtual string? ValueAsJson { get; set; }

    public virtual SettingValueBaseBusinessEntity? DefaultValue { get; set; }

    public virtual string? JsonSchema { get; set; }

    public virtual string? DefaultValueAsJson
    {
        get
        {
            if (DefaultValue == null) return null;
            
            _defaultValueAsJson = JsonConvert.SerializeObject(DefaultValue, JsonSettings.FigDefault);
            return _defaultValueAsJson;
        }
        set
        {
            if (_defaultValueAsJson != value && value != null)
            {
                DefaultValue = (SettingValueBaseBusinessEntity?)JsonConvert.DeserializeObject(value, JsonSettings.FigDefault);
            }
                
        }
    }

    public virtual string? ValidationRegex { get; set; }

    public virtual string? ValidationExplanation { get; set; }

    public virtual IList<string>? ValidValues { get; set; }

    public virtual string? ValidValuesAsJson
    {
        get
        {
            if (ValidValues == null)
                return null;

            _validValuesAsJson = JsonConvert.SerializeObject(ValidValues);
            return _validValuesAsJson;
        }
        set
        {
            if (_validValuesAsJson != value)
                ValidValues = value != null
                    ? JsonConvert.DeserializeObject<IList<string>>(value)
                    : Array.Empty<string>();
        }
    }
    
    public virtual IList<string>? EnablesSettings { get; set; }

    public virtual string? EnablesSettingsAsJson
    {
        get
        {
            if (EnablesSettings == null)
                return null;

            _enablesSettingsAsJson = JsonConvert.SerializeObject(EnablesSettings);
            return _enablesSettingsAsJson;
        }
        set
        {
            if (_enablesSettingsAsJson != value)
                EnablesSettings = value != null
                    ? JsonConvert.DeserializeObject<IList<string>>(value)
                    : Array.Empty<string>();
        }
    }

    public virtual string? Group { get; set; }

    public virtual int? DisplayOrder { get; set; }

    public virtual bool Advanced { get; set; }

    public virtual string? LookupTableKey { get; set; }

    public virtual int? EditorLineCount { get; set; }

    public virtual string? DataGridDefinitionJson { get; set; }
    
    public virtual bool SupportsLiveUpdate { get; set; }
    
    public virtual DateTime? LastChanged { get; set; }
    
    public virtual string? CategoryName { get; set; }
        
    public virtual string? CategoryColor { get; set; }

    public virtual string? DisplayScript { get; set; }
    
    public virtual string? DisplayScriptHash { get; set; }
    
    public virtual bool DisplayScriptHashRequired { get; set; }
    
    public virtual bool IsExternallyManaged { get; set; }
    
    public virtual Classification Classification { get; set; }
    
    public virtual bool? EnvironmentSpecific { get; set; }
    
    public virtual string? LookupKeySettingName { get; set; }
    
    public virtual int? Indent { get; set; }
    
    public virtual string? DependsOnProperty { get; set; }
    
    public virtual IList<string>? DependsOnValidValues { get; set; }

    public virtual string? DependsOnValidValuesAsJson
    {
        get
        {
            if (DependsOnValidValues == null)
                return null;

            _dependsOnValidValuesAsJson = JsonConvert.SerializeObject(DependsOnValidValues);
            return _dependsOnValidValuesAsJson;
        }
        set
        {
            if (_dependsOnValidValuesAsJson != value)
                DependsOnValidValues = value != null
                    ? JsonConvert.DeserializeObject<IList<string>>(value)
                    : Array.Empty<string>();
        }
    }
    
    public virtual HeadingDataContract? Heading { get; set; }

    public virtual string? HeadingAsJson
    {
        get
        {
            if (Heading == null)
                return null;

            _headingAsJson = JsonConvert.SerializeObject(Heading);
            return _headingAsJson;
        }
        set
        {
            if (_headingAsJson != value)
            {
                Heading = value != null
                    ? JsonConvert.DeserializeObject<HeadingDataContract>(value)
                    : null;
            }
        }
    }
}