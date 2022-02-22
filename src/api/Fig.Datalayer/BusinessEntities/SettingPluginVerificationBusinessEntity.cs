using Newtonsoft.Json;

namespace Fig.Datalayer.BusinessEntities;

public class SettingPluginVerificationBusinessEntity : SettingVerificationBase
{
    private string? _propertyArgumentsAsJson;

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