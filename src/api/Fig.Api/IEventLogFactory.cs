using Fig.Datalayer.BusinessEntities;

namespace Fig.Api;

public interface IEventLogFactory
{
    EventLogBusinessEntity InitialRegistration(Guid clientId, string clientName);
    
    EventLogBusinessEntity IdenticalRegistration(Guid clientId, string clientName);

    EventLogBusinessEntity UpdatedRegistration(Guid clientId, string clientName);

    EventLogBusinessEntity SettingValueUpdate(Guid clientId,
        string clientName,
        string? instance,
        string settingName,
        object originalValue,
        object newValue);
}