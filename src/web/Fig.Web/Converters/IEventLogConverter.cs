using Fig.Contracts.EventHistory;
using Fig.Web.Models.Events;

namespace Fig.Web.Converters;

public interface IEventLogConverter
{
    EventLogModel Convert(EventLogDataContract eventLog);
}