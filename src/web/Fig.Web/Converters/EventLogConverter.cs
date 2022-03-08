using Fig.Contracts.EventHistory;
using Fig.Web.Models.Events;

namespace Fig.Web.Converters;

public class EventLogConverter : IEventLogConverter
{
    public EventLogModel Convert(EventLogDataContract eventLog)
    {
        return new EventLogModel
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