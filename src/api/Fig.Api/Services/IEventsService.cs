using Fig.Contracts.EventHistory;

namespace Fig.Api.Services;

public interface IEventsService
{
    EventLogCollectionDataContract GetEventLogs(DateTime startTime, DateTime endTime);
}