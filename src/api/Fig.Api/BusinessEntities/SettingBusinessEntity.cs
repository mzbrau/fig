using System.Text.Json;

namespace Fig.Api.BusinessEntities;

public class SettingBusinessEntity
{
    private string? _valueAsJson;
    private string? _defaultValueAsJson;
    private string? _validValuesAsJson;
    private Type? _valueType;
    private Type? _defaultValueType;

    public virtual Guid Id { get; set; }
    
    public virtual string Name { get; set; }

    public virtual string Description { get; set; }
    
    public virtual bool IsSecret { get; set; }
    
    public virtual Type ValueType
    {
        get
        {
            _valueType = Value?.GetType();
            return _valueType;
        }
        set
        {
            _valueType = value;
        }
    }
    
    public virtual dynamic? Value { get; set; }

    public virtual string? ValueAsJson
    {
        get
        {
            if (Value == null)
            {
                return null;
            }

            _valueAsJson = JsonSerializer.Serialize(Value);
            return _valueAsJson;
        }
        set
        {
            if (_valueAsJson != value && value != null && _valueType != null)
            {
                Value = JsonSerializer.Deserialize(value, _valueType);
            }
        }
    }
    
    public virtual Type DefaultValueType
    {
        get
        {
            _defaultValueType = DefaultValue?.GetType();
            return _defaultValueType;
        }
        set
        {
            _defaultValueType = value;
        }
    }

    public virtual dynamic? DefaultValue { get; set; }
    

    public virtual string? DefaultValueAsJson
    {
        get
        {
            if (DefaultValue == null)
            {
                return null;
            }

            _defaultValueAsJson = JsonSerializer.Serialize(DefaultValue);
            return _defaultValueAsJson;
        }
        set
        {
            if (_defaultValueAsJson != value && value != null && _defaultValueType != null)
            {
                DefaultValue = JsonSerializer.Deserialize(value, _defaultValueType);
            }
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
            {
                return null;
            }
            
            _validValuesAsJson = JsonSerializer.Serialize(ValidValues);
            return _validValuesAsJson;
        }
        set
        {
            if (_validValuesAsJson != value)
            {
                ValidValues = JsonSerializer.Deserialize<IList<string>>(value) ?? Array.Empty<string>();
            }
        }
    }

    public virtual string? Group { get; set; }

    public virtual int? DisplayOrder { get; set; }

    public virtual bool Advanced { get; set; }
    
    public virtual string? StringFormat { get; set; }
}