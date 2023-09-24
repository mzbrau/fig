using Fig.Contracts.EventHistory;

namespace Fig.Api.Services;

public interface IEventsService : IAuthenticatedService
{
    EventLogCollectionDataContract GetEventLogs(DateTime startTime, DateTime endTime);
    
    EventLogCountDataContract GetEventLogCount();
}