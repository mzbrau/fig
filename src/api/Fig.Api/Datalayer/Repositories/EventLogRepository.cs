using Fig.Api.Constants;
using Fig.Api.ExtensionMethods;
using Fig.Api.Services;
using Fig.Contracts.Authentication;
using Fig.Datalayer.BusinessEntities;
using NHibernate.Criterion;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories;

public class EventLogRepository : RepositoryBase<EventLogBusinessEntity>, IEventLogRepository
{
    private readonly IEncryptionService _encryptionService;

    public EventLogRepository(ISession session, IEncryptionService encryptionService)
        : base(session)
    {
        _encryptionService = encryptionService;
    }

    public void Add(EventLogBusinessEntity log)
    {
        log.Encrypt(_encryptionService);
        log.LastEncrypted = DateTime.UtcNow;
        Save(log);
    }

    public IEnumerable<EventLogBusinessEntity> GetAllLogs(DateTime startDate,
        DateTime endDate,
        bool onlyUnrestricted,
        UserDataContract? requestingUser)
    {
        var criteria = Session.CreateCriteria<EventLogBusinessEntity>();
        criteria.Add(Restrictions.Ge(nameof(EventLogBusinessEntity.Timestamp), startDate));
        criteria.Add(Restrictions.Le(nameof(EventLogBusinessEntity.Timestamp), endDate));

        if (onlyUnrestricted)
            criteria.Add(Restrictions.In(nameof(EventLogBusinessEntity.EventType), EventMessage.UnrestrictedEvents));

        criteria.AddOrder(Order.Desc(nameof(EventLogBusinessEntity.Timestamp)));
        var result = criteria.List<EventLogBusinessEntity>()
            .Where(log => string.IsNullOrWhiteSpace(log.ClientName) || requestingUser?.HasAccess(log.ClientName) == true)
            .ToList();
        result.ForEach(c => c.Decrypt(_encryptionService));
        return result;
    }

    public DateTime GetEarliestEntry()
    {
        var result = Session.Query<EventLogBusinessEntity>().FirstOrDefault();
        return result?.Timestamp ?? DateTime.UtcNow;
    }

    public IEnumerable<EventLogBusinessEntity> GetSettingChanges(DateTime startDate, DateTime endDate, string clientName, string? instance)
    {
        var criteria = Session.CreateCriteria<EventLogBusinessEntity>();
        criteria.Add(Restrictions.Ge(nameof(EventLogBusinessEntity.Timestamp), startDate));
        criteria.Add(Restrictions.Le(nameof(EventLogBusinessEntity.Timestamp), endDate));
        criteria.Add(Restrictions.Eq(nameof(EventLogBusinessEntity.ClientName), clientName));
        criteria.Add(Restrictions.Eq(nameof(EventLogBusinessEntity.Instance), instance));
        criteria.Add(Restrictions.Eq(nameof(EventLogBusinessEntity.EventType), EventMessage.SettingValueUpdated));

        var result = criteria.List<EventLogBusinessEntity>().ToList();
        result.ForEach(c => c.Decrypt(_encryptionService));
        return result;
    }

    public IEnumerable<EventLogBusinessEntity> GetLogsForEncryptionMigration(DateTime secretChangeDate)
    {
        var criteria = Session.CreateCriteria<EventLogBusinessEntity>();
        criteria.Add(Restrictions.Le(nameof(EventLogBusinessEntity.LastEncrypted), secretChangeDate));
        criteria.SetMaxResults(1000);

        var result = criteria.List<EventLogBusinessEntity>().ToList();
        result.ForEach(c => c.Decrypt(_encryptionService, true));
        return result;
    }

    public void UpdateLogsAfterEncryptionMigration(List<EventLogBusinessEntity> updatedLogs)
    {
        using var transaction = Session.BeginTransaction();
        foreach (var log in updatedLogs)
        {
            log.LastEncrypted = DateTime.UtcNow;
            log.Encrypt(_encryptionService);
            Session.Update(log);
        }
            
        transaction.Commit();
        Session.Flush();
        
        foreach (var log in updatedLogs)
            Session.Evict(log);
    }

    public long GetEventLogCount()
    {
        var count = Session.QueryOver<EventLogBusinessEntity>()
            .Select(Projections.RowCountInt64())
            .SingleOrDefault<long>();

        return count;
    }
}