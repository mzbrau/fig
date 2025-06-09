using Fig.Contracts.EventHistory;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Converters;

public class EventsConverter : IEventsConverter
{
    public EventLogDataContract Convert(EventLogBusinessEntity eventLog)
    {
        return new EventLogDataContract(eventLog.Timestamp,
            eventLog.ClientName,
            eventLog.Instance,
            eventLog.SettingName,
            eventLog.EventType,
            eventLog.OriginalValue,
            eventLog.NewValue,
            eventLog.AuthenticatedUser,
            eventLog.Message,
            eventLog.IpAddress,
            eventLog.Hostname);
    }
}