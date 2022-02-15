using Fig.Api.ExtensionMethods;
using Fig.Api.Services;
using Fig.Datalayer.BusinessEntities;

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

    public IEnumerable<EventLogBusinessEntity> GetAllLogs()
    {
        var logs = GetAll().ToList();
        logs.ForEach(l => l.Decrypt(_encryptionService));
        return logs;
    }
}