using Fig.Api.ExtensionMethods;
using Fig.Api.Services;
using Fig.Datalayer.BusinessEntities;
using NHibernate.Criterion;

namespace Fig.Api.Datalayer.Repositories;

public class EventLogRepository : RepositoryBase<EventLogBusinessEntity>, IEventLogRepository
{
    private readonly IEncryptionService _encryptionService;

    public EventLogRepository(IFigSessionFactory sessionFactory, IEncryptionService encryptionService) 
        : base(sessionFactory)
    {
        _encryptionService = encryptionService;
    }

    public void Add(EventLogBusinessEntity log)
    {
        log.Encrypt(_encryptionService);
        Save(log);
    }

    public IEnumerable<EventLogBusinessEntity> GetAllLogs(DateTime startDate, DateTime endDate)
    {
        using var session = SessionFactory.OpenSession();
        var criteria = session.CreateCriteria<EventLogBusinessEntity>();
        criteria.Add(Restrictions.Ge(nameof(EventLogBusinessEntity.Timestamp), startDate));
        criteria.Add(Restrictions.Le(nameof(EventLogBusinessEntity.Timestamp), endDate));
        criteria.AddOrder(Order.Desc(nameof(EventLogBusinessEntity.Timestamp)));
        var result = criteria.List<EventLogBusinessEntity>().ToList();
        result.ForEach(c => c.Decrypt(_encryptionService));
        return result;
    }

    public DateTime GetEarliestEntry()
    {
        using var session = SessionFactory.OpenSession();
        var result = session.Query<EventLogBusinessEntity>().FirstOrDefault();
        return result?.Timestamp ?? DateTime.UtcNow;
    }
}