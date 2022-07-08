using Newtonsoft.Json;

namespace Fig.Datalayer.BusinessEntities;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by nhibernate.
public class CommonEnumerationBusinessEntity
{
    private string? _enumerationAsJson;
    
    public virtual Guid? Id { get; init; }
    
    public virtual string Name { get; set; } = default!;

    public virtual Dictionary<string, string> Enumeration { get; set; } = default!;

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