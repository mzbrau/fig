using Newtonsoft.Json;

namespace Fig.Datalayer.BusinessEntities;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by nhibernate.
public class LookupTableBusinessEntity
{
    private string? _lookupTableAsJson;
    
    public virtual Guid? Id { get; init; }
    
    public virtual string Name { get; set; } = default!;

    public virtual Dictionary<string, string> LookupTable { get; set; } = default!;

    public virtual string? LookupTableAsJson
    {
        get
        {
            _lookupTableAsJson = JsonConvert.SerializeObject(LookupTable);
            return _lookupTableAsJson;
        }
        set
        {
            if (_lookupTableAsJson != value && value != null)
                LookupTable = JsonConvert.DeserializeObject<Dictionary<string, string>>(value) ?? new Dictionary<string, string>();
        }
    }
}