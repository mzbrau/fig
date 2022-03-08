using Fig.Contracts.EventHistory;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Converters;

public interface IEventsConverter
{ 
    EventLogDataContract Convert(EventLogBusinessEntity eventLog);
}