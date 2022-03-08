using Fig.Contracts.EventHistory;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Converters;

public class EventsConverter : IEventsConverter
{
    public EventLogDataContract Convert(EventLogBusinessEntity eventLog)
    {
        return new EventLogDataContract
        {
            Timestamp = eventLog.Timestamp,
            ClientName = eventLog.ClientName,
            Instance = eventLog.Instance,
            SettingName = eventLog.SettingName,
            EventType = eventLog.EventType,
            OriginalValue = eventLog.OriginalValue,
            NewValue = eventLog.NewValue,
            AuthenticatedUser = eventLog.AuthenticatedUser,
            VerificationName = eventLog.VerificationName,
            IpAddress = eventLog.IpAddress,
            Hostname = eventLog.Hostname
        };
    }
}