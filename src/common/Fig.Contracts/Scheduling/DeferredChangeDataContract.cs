using System;
using Fig.Contracts.Settings;

namespace Fig.Contracts.Scheduling;

public class DeferredChangeDataContract
{
    public DeferredChangeDataContract(Guid id,
        DateTime executeAtUtc,
        string requestingUser,
        string clientName,
        string? instance,
        SettingValueUpdatesDataContract? changeSet)
    {
        Id = id;
        ExecuteAtUtc = executeAtUtc;
        RequestingUser = requestingUser;
        ClientName = clientName;
        Instance = instance;
        ChangeSet = changeSet;
    }

    public Guid Id { get; }
    
    public DateTime ExecuteAtUtc { get; }
    
    public string RequestingUser { get; }

    public string ClientName { get; }
    
    public string? Instance { get; }
    
    public virtual SettingValueUpdatesDataContract? ChangeSet { get; }
}