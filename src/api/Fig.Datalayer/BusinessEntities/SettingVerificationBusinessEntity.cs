using Newtonsoft.Json;

namespace Fig.Datalayer.BusinessEntities;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by nhibernate.
public class SettingVerificationBusinessEntity
{
    private string? _propertyArgumentsAsJson;

    public virtual Guid Id { get; init; }

    public virtual string Name { get; set; } = default!;

    public virtual IList<string>? PropertyArguments { get; set; }

    public virtual string? PropertyArgumentsAsJson
    {
        get
        {
            if (PropertyArguments == null)
                return null;

            _propertyArgumentsAsJson = JsonConvert.SerializeObject(PropertyArguments);
            return _propertyArgumentsAsJson;
        }
        set
        {
            if (_propertyArgumentsAsJson != value)
                PropertyArguments = value != null
                    ? JsonConvert.DeserializeObject<IList<string>>(value)
                    : Array.Empty<string>();
        }
    }
}