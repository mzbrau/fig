using Fig.Contracts.EventHistory;

namespace Fig.Api.Services;

public interface IEventsService : IAuthenticatedService
{
    Task<EventLogCollectionDataContract> GetEventLogs(DateTime startTime, DateTime endTime);
    
    Task<EventLogCountDataContract> GetEventLogCount();
}