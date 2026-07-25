using Fig.Api.Datalayer.Repositories;
using Fig.Contracts.EventHistory;

namespace Fig.Api.Services;

public interface IEventsService : IAuthenticatedService
{
    Task<EventLogCollectionDataContract> GetEventLogs(DateTime startTime, DateTime endTime);

    Task<EventLogCollectionDataContract> GetEventLogs(
        DateTime startTime,
        DateTime endTime,
        EventLogQuery query);
    
    Task<EventLogCountDataContract> GetEventLogCount();
    
    Task<EventLogCollectionDataContract> GetClientSettingChanges(DateTime startTime, DateTime endTime, string clientName, string? instance);
}