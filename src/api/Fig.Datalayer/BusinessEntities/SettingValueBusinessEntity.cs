namespace Fig.Datalayer.BusinessEntities;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by nhibernate.
public class SettingValueBusinessEntity
{
    private Type? _valueType;

    public virtual Guid Id { get; init; }

    public virtual Guid ClientId { get; set; }

    public virtual string SettingName { get; set; } = default!;

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

    public virtual string ChangedBy { get; set; } = default!;
}