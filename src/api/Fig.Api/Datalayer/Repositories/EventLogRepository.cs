using System.Diagnostics;
using Fig.Api.Constants;
using Fig.Api.ExtensionMethods;
using Fig.Api.Observability;
using Fig.Api.Services;
using Fig.Contracts.Authentication;
using Fig.Datalayer.BusinessEntities;
using NHibernate;
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
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        log.Encrypt(_encryptionService);
        log.LastEncrypted = log.Timestamp;
        Save(log);
    }

    public IList<EventLogBusinessEntity> GetAllLogs(DateTime startDate,
        DateTime endDate,
        bool onlyUnrestricted,
        UserDataContract? requestingUser)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
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
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var result = Session.Query<EventLogBusinessEntity>().FirstOrDefault();
        return result?.Timestamp ?? DateTime.UtcNow;
    }

    public IList<EventLogBusinessEntity> GetSettingChanges(DateTime startDate, DateTime endDate, string clientName, string? instance)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
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

    public IList<EventLogBusinessEntity> GetLogsForEncryptionMigration(DateTime secretChangeDate)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<EventLogBusinessEntity>();
        criteria.Add(Restrictions.Le(nameof(EventLogBusinessEntity.LastEncrypted), secretChangeDate));
        criteria.SetMaxResults(1000);
        criteria.SetLockMode(LockMode.Upgrade);

        var result = criteria.List<EventLogBusinessEntity>().ToList();
        result.ForEach(c => c.Decrypt(_encryptionService, true));
        return result;
    }

    public void UpdateLogsAfterEncryptionMigration(List<EventLogBusinessEntity> updatedLogs)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        foreach (var log in updatedLogs)
        {
            log.LastEncrypted = DateTime.UtcNow;
            log.Encrypt(_encryptionService);
            Session.Update(log);
        }

        Session.Flush();
        
        foreach (var log in updatedLogs)
            Session.Evict(log);
    }

    public long GetEventLogCount()
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var count = Session.QueryOver<EventLogBusinessEntity>()
            .Select(Projections.RowCountInt64())
            .SingleOrDefault<long>();

        return count;
    }
}