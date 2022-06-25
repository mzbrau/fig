using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface IEventLogRepository
{
    void Add(EventLogBusinessEntity log);

    IEnumerable<EventLogBusinessEntity> GetAllLogs(DateTime startDate, DateTime endDate, bool includeUserEvents);

    DateTime GetEarliestEntry();
}