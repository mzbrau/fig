using Newtonsoft.Json;

namespace Fig.Datalayer.BusinessEntities;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by nhibernate.
public class SettingBusinessEntity
{
    private string? _defaultValueAsJson;
    private string? _validValuesAsJson;

    public virtual Guid Id { get; init; }

    public virtual string Name { get; set; } = default!;

    public virtual string Description { get; set; } = default!;

    public virtual bool IsSecret { get; set; }

    public virtual Type ValueType { get; set; } = default!;

    public virtual dynamic? Value { get; set; }

    public virtual string? ValueAsJson { get; set; }

    public virtual dynamic? DefaultValue { get; set; }

    public virtual string? JsonSchema { get; set; }

    public virtual string? DefaultValueAsJson
    {
        get
        {
            if (DefaultValue == null) return null;

            _defaultValueAsJson = JsonConvert.SerializeObject(DefaultValue);
            return _defaultValueAsJson;
        }
        set
        {
            if (_defaultValueAsJson != value && value != null)
                DefaultValue = JsonConvert.DeserializeObject(value, ValueType);
        }
    }

    public virtual string ValidationType { get; set; }

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

    public virtual string? Group { get; set; }

    public virtual int? DisplayOrder { get; set; }

    public virtual bool Advanced { get; set; }

    public virtual string? CommonEnumerationKey { get; set; }

    public virtual int? EditorLineCount { get; set; }

    public virtual string? DataGridDefinitionJson { get; set; }
}