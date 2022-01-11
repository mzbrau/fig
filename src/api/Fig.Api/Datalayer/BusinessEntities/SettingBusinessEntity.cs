using Newtonsoft.Json;

namespace Fig.Api.Datalayer.BusinessEntities;

public class SettingBusinessEntity
{
    private string? _defaultValueAsJson;
    private string? _validValuesAsJson;
    private string? _valueAsJson;

    public virtual Guid Id { get; set; }

    public virtual string Name { get; set; }

    public virtual string Description { get; set; }

    public virtual bool IsSecret { get; set; }

    public virtual Type ValueType { get; set; }

    public virtual dynamic? Value { get; set; }

    public virtual string? ValueAsJson
    {
        get
        {
            if (Value == null) return null;

            _valueAsJson = JsonConvert.SerializeObject(Value);
            return _valueAsJson;
        }
        set
        {
            if (_valueAsJson != value && value != null) Value = JsonConvert.DeserializeObject(value, ValueType);
        }
    }

    public virtual dynamic? DefaultValue { get; set; }


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
            if (ValidValues == null) return null;

            _validValuesAsJson = JsonConvert.SerializeObject(ValidValues);
            return _validValuesAsJson;
        }
        set
        {
            if (_validValuesAsJson != value)
                ValidValues = JsonConvert.DeserializeObject<IList<string>>(value) ?? Array.Empty<string>();
        }
    }

    public virtual string? Group { get; set; }

    public virtual int? DisplayOrder { get; set; }

    public virtual bool Advanced { get; set; }

    public virtual string? StringFormat { get; set; }

    public virtual int EditorLineCount { get; set; }
}