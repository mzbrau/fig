using System.Text.Json;

namespace Fig.Api.Datalayer.BusinessEntities;

public class HistoricalSettingValueBusinessEntity
{
    private string _valueAsJson;
    private Type? _valueType;
    
    public virtual Guid Id { get; set; }
    
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

    public virtual DateTime ChangedAt { get; set; }
}