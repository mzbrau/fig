using Newtonsoft.Json;

namespace Fig.Datalayer.BusinessEntities;

public class CommonEnumerationBusinessEntity
{
    private string? _enumerationAsJson;
    
    public virtual Guid? Id { get; set; }
    
    public virtual string Name { get; set; }

    public virtual Dictionary<string, string> Enumeration { get; set; }

    public virtual string? EnumerationAsJson
    {
        get
        {
            _enumerationAsJson = JsonConvert.SerializeObject(Enumeration);
            return _enumerationAsJson;
        }
        set
        {
            if (_enumerationAsJson != value && value != null)
                Enumeration = JsonConvert.DeserializeObject<Dictionary<string, string>>(value) ?? new Dictionary<string, string>();
        }
    }
}