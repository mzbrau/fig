using Newtonsoft.Json;

namespace Fig.Datalayer.BusinessEntities;

public class SettingValueBusinessEntity
{
    private string? _valueAsJson;
    private Type? _valueType;

    public virtual Guid Id { get; set; }

    public virtual Guid ClientId { get; set; }

    public virtual string SettingName { get; set; }

    public virtual Type ValueType
    {
        get
        {
            _valueType = Value?.GetType() ?? typeof(string);
            return _valueType;
        }
        set => _valueType = value;
    }

    public virtual dynamic? Value { get; set; }

    public virtual string? ValueAsJson { get; set; }

    public virtual DateTime ChangedAt { get; set; }
    
    public virtual string ChangedBy { get; set; }
    
    public virtual bool IsEncrypted { get; set; }
}