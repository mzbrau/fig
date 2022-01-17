using Fig.Datalayer.BusinessEntities;

namespace Fig.Api;

public class EventLogFactory : IEventLogFactory
{
    public EventLogBusinessEntity InitialRegistration(Guid clientId, string clientName)
    {
        return Create("Initial Registration", clientId, clientName);
    }

    public EventLogBusinessEntity IdenticalRegistration(Guid clientId, string clientName)
    {
        return Create("Registration - No Change", clientId, clientName);
    }

    public EventLogBusinessEntity UpdatedRegistration(Guid clientId, string clientName)
    {
        return Create("Registration - Definition Changed", clientId, clientName);
    }

    public EventLogBusinessEntity SettingValueUpdate(Guid clientId, string clientName, string? instance, string settingName, object originalValue,
        object newValue)
    {
        // TODO: Values are hacked here, maybe fix?
        return Create("Value Changed", clientId, clientName, settingName, originalValue?.ToString(), newValue?.ToString());
    }
    
    private EventLogBusinessEntity Create(string eventType,
        Guid clientId,
        string clientName,
        string? settingName = null,
        string? originalValue = null,
        string? newValue = null)
    {
        return new EventLogBusinessEntity
        {
            Timestamp = DateTime.UtcNow,
            ClientId = clientId,
            ClientName = clientName,
            SettingName = settingName,
            EventType = eventType,
            OriginalValue = originalValue,
            NewValue = newValue
        };
    }
}