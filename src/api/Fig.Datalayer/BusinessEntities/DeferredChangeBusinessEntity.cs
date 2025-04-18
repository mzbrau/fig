using Fig.Common.NetStandard.Json;
using Fig.Contracts.Settings;
using Fig.Datalayer.BusinessEntities.SettingValues;
using Newtonsoft.Json;

namespace Fig.Datalayer.BusinessEntities;

public class DeferredChangeBusinessEntity
{
    private string? _changeSetAsJson;
    
    public virtual Guid? Id { get; init; }
    
    public virtual DateTime ExecuteAtUtc { get; set; }
    
    public virtual string? RequestingUser { get; set; }

    public virtual string ClientName { get; set; } = default!;
    
    public virtual string? Instance { get; set; }
    
    public virtual string? ChangeSetAsJson { get; set; }
    
    public virtual SettingValueUpdatesDataContract? ChangeSet { get; set; }
    
    public virtual string? HandlingInstance { get; set; }
}