using Fig.Api.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface IEventLogRepository
{
    void Add(EventLogBusinessEntity log);

    IEnumerable<EventLogBusinessEntity> GetAllLogs();
}