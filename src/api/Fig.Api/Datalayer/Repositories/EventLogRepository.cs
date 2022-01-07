using Fig.Api.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public class EventLogRepository : RepositoryBase<EventLogBusinessEntity>, IEventLogRepository
{
    public EventLogRepository(IFigSessionFactory sessionFactory) : base(sessionFactory)
    {
    }

    public void Add(EventLogBusinessEntity log)
    {
        Save(log);
    }

    public IEnumerable<EventLogBusinessEntity> GetAllLogs()
    {
        return GetAll();
    }
}