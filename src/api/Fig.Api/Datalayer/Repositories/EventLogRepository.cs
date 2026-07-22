using System.Diagnostics;
using Fig.Api.ExtensionMethods;
using Fig.Api.Observability;
using Fig.Api.Services;
using Fig.Common.Constants;
using Fig.Contracts.Authentication;
using Fig.Datalayer.BusinessEntities;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Linq;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories;

public class EventLogRepository : RepositoryBase<EventLogBusinessEntity>, IEventLogRepository
{
    private readonly IEncryptionService _encryptionService;
    private readonly ISessionFactory _sessionFactory;

    public EventLogRepository(ISession session, ISessionFactory sessionFactory, IEncryptionService encryptionService)
        : base(session)
    {
        _sessionFactory = sessionFactory;
        _encryptionService = encryptionService;
    }

    public async Task Add(EventLogBusinessEntity log)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        log.Encrypt(_encryptionService);
        log.LastEncrypted = log.Timestamp;
        await Save(log);
    }

    public async Task AddCommitted(EventLogBusinessEntity log)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        log.Encrypt(_encryptionService);
        log.LastEncrypted = log.Timestamp;

        using var independentSession = _sessionFactory.OpenSession();
        using var transaction = independentSession.BeginTransaction();
        try
        {
            await independentSession.SaveAsync(log);
            await transaction.CommitAsync();
            await independentSession.FlushAsync();
        }
        catch
        {
            if (transaction.IsActive)
                await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<IList<EventLogBusinessEntity>> GetAllLogs(DateTime startDate,
        DateTime endDate,
        bool onlyUnrestricted,
        UserDataContract requestingUser)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<EventLogBusinessEntity>();
        criteria.Add(Restrictions.Ge(nameof(EventLogBusinessEntity.Timestamp), startDate));
        criteria.Add(Restrictions.Le(nameof(EventLogBusinessEntity.Timestamp), endDate));

        if (onlyUnrestricted)
            criteria.Add(Restrictions.In(nameof(EventLogBusinessEntity.EventType), EventMessage.UnrestrictedEvents));

        criteria.AddOrder(Order.Desc(nameof(EventLogBusinessEntity.Timestamp)));
        var result = (await criteria.ListAsync<EventLogBusinessEntity>())
            .Where(log => string.IsNullOrWhiteSpace(log.ClientName) || requestingUser.HasAccess(log.ClientName))
            .ToList();
        result.ForEach(c => c.Decrypt(_encryptionService));
        return result;
    }

    public async Task<DateTime> GetEarliestEntry()
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var result = await Session.Query<EventLogBusinessEntity>().FirstOrDefaultAsync();
        return result?.Timestamp ?? DateTime.UtcNow;
    }

    public async Task<IList<EventLogBusinessEntity>> GetSettingChanges(DateTime startDate, DateTime endDate, string clientName, string? instance)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<EventLogBusinessEntity>();
        criteria.Add(Restrictions.Ge(nameof(EventLogBusinessEntity.Timestamp), startDate));
        criteria.Add(Restrictions.Le(nameof(EventLogBusinessEntity.Timestamp), endDate));
        criteria.Add(Restrictions.Eq(nameof(EventLogBusinessEntity.ClientName), clientName));
        criteria.Add(Restrictions.Eq(nameof(EventLogBusinessEntity.Instance), instance));
        criteria.Add(Restrictions.Eq(nameof(EventLogBusinessEntity.EventType), EventMessage.SettingValueUpdated));

        var result = (await criteria.ListAsync<EventLogBusinessEntity>()).ToList();
        result.ForEach(c => c.Decrypt(_encryptionService));
        return result;
    }

    public async Task<IList<EventLogBusinessEntity>> GetClientSettingChanges(DateTime startDate, DateTime endDate, string clientName, string? instance, UserDataContract requestingUser)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<EventLogBusinessEntity>();
        criteria.Add(Restrictions.Ge(nameof(EventLogBusinessEntity.Timestamp), startDate));
        criteria.Add(Restrictions.Le(nameof(EventLogBusinessEntity.Timestamp), endDate));
        criteria.Add(Restrictions.Eq(nameof(EventLogBusinessEntity.ClientName), clientName));
        criteria.Add(Restrictions.Eq(nameof(EventLogBusinessEntity.Instance), instance));
        criteria.Add(Restrictions.In(nameof(EventLogBusinessEntity.EventType), [
            EventMessage.SettingValueUpdated,
            EventMessage.InitialRegistration,
            EventMessage.ExternallyManagedSettingUpdatedByUser,
            EventMessage.ClientDeleted
        ]));
        criteria.AddOrder(Order.Desc(nameof(EventLogBusinessEntity.Timestamp)));
        
        var result = (await criteria.ListAsync<EventLogBusinessEntity>())
            .Where(log => string.IsNullOrWhiteSpace(log.ClientName) || requestingUser.HasAccess(log.ClientName))
            .ToList();
        result.ForEach(c => c.Decrypt(_encryptionService));
        return result;
    }

    public async Task<IList<EventLogBusinessEntity>> GetClientEvents(
        DateTime startDate,
        DateTime endDate,
        string clientName,
        string? instance,
        IReadOnlyCollection<string>? eventTypes = null)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<EventLogBusinessEntity>();
        criteria.Add(Restrictions.Ge(nameof(EventLogBusinessEntity.Timestamp), startDate));
        criteria.Add(Restrictions.Le(nameof(EventLogBusinessEntity.Timestamp), endDate));
        criteria.Add(Restrictions.Eq(nameof(EventLogBusinessEntity.ClientName), clientName));
        if (string.IsNullOrWhiteSpace(instance))
            criteria.Add(Restrictions.IsNull(nameof(EventLogBusinessEntity.Instance)));
        else
            criteria.Add(Restrictions.Eq(nameof(EventLogBusinessEntity.Instance), instance));

        if (eventTypes is { Count: > 0 })
            criteria.Add(Restrictions.In(nameof(EventLogBusinessEntity.EventType), eventTypes.ToArray()));

        criteria.AddOrder(Order.Asc(nameof(EventLogBusinessEntity.Timestamp)));
        var result = (await criteria.ListAsync<EventLogBusinessEntity>()).ToList();
        result.ForEach(c => c.Decrypt(_encryptionService));
        return result;
    }

    public async Task<IList<EventLogBusinessEntity>> GetLogsForAuthenticatedUser(
        DateTime startDate,
        DateTime endDate,
        string username)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<EventLogBusinessEntity>();
        criteria.Add(Restrictions.Ge(nameof(EventLogBusinessEntity.Timestamp), startDate));
        criteria.Add(Restrictions.Le(nameof(EventLogBusinessEntity.Timestamp), endDate));
        criteria.Add(Restrictions.InsensitiveLike(
            nameof(EventLogBusinessEntity.AuthenticatedUser),
            username,
            MatchMode.Exact));
        criteria.AddOrder(Order.Desc(nameof(EventLogBusinessEntity.Timestamp)));

        var result = (await criteria.ListAsync<EventLogBusinessEntity>()).ToList();
        result.ForEach(c => c.Decrypt(_encryptionService));
        return result;
    }

    public async Task<IList<EventLogBusinessEntity>> GetEventsByTypes(
        DateTime startDate,
        DateTime endDate,
        IReadOnlyCollection<string> eventTypes,
        UserDataContract requestingUser,
        string? clientName = null,
        string? instance = null)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<EventLogBusinessEntity>();
        criteria.Add(Restrictions.Ge(nameof(EventLogBusinessEntity.Timestamp), startDate));
        criteria.Add(Restrictions.Le(nameof(EventLogBusinessEntity.Timestamp), endDate));

        if (eventTypes.Count > 0)
            criteria.Add(Restrictions.In(nameof(EventLogBusinessEntity.EventType), eventTypes.ToArray()));

        if (!string.IsNullOrWhiteSpace(clientName))
        {
            criteria.Add(Restrictions.Eq(nameof(EventLogBusinessEntity.ClientName), clientName));
            if (string.IsNullOrWhiteSpace(instance))
                criteria.Add(Restrictions.IsNull(nameof(EventLogBusinessEntity.Instance)));
            else
                criteria.Add(Restrictions.Eq(nameof(EventLogBusinessEntity.Instance), instance));
        }

        criteria.AddOrder(Order.Desc(nameof(EventLogBusinessEntity.Timestamp)));
        var result = (await criteria.ListAsync<EventLogBusinessEntity>())
            .Where(log => string.IsNullOrWhiteSpace(log.ClientName) || requestingUser.HasAccess(log.ClientName))
            .ToList();
        result.ForEach(c => c.Decrypt(_encryptionService));
        return result;
    }

    public async Task<IList<EventLogBusinessEntity>> GetLogsForEncryptionMigration(DateTime secretChangeDate)
    {
        var result = await GetEncryptedLogsForEncryptionMigration(secretChangeDate);
        foreach (var log in result)
            log.Decrypt(_encryptionService, true);
        return result;
    }

    public async Task<IList<EventLogBusinessEntity>> GetEncryptedLogsForEncryptionMigration(DateTime secretChangeDate)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<EventLogBusinessEntity>();
        criteria.Add(Restrictions.Le(nameof(EventLogBusinessEntity.LastEncrypted), secretChangeDate));
        criteria.SetMaxResults(1000);
        criteria.SetLockMode(LockMode.Upgrade);

        return (await criteria.ListAsync<EventLogBusinessEntity>()).ToList();
    }

    public async Task UpdateLogsAfterEncryptionMigration(List<EventLogBusinessEntity> updatedLogs)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var existingTransaction = Session.GetCurrentTransaction();
        var needsTransaction = existingTransaction == null || !existingTransaction.IsActive;
        var transaction = needsTransaction ? Session.BeginTransaction() : null;
        try
        {
            foreach (var log in updatedLogs)
            {
                log.LastEncrypted = DateTime.UtcNow;
                log.Encrypt(_encryptionService);
                await Session.UpdateAsync(log);
            }

            if (transaction != null)
                await transaction.CommitAsync();
            await Session.FlushAsync();
            
            foreach (var log in updatedLogs)
                await Session.EvictAsync(log);
        }
        finally
        {
            transaction?.Dispose();
        }
    }

    public async Task<long> GetEventLogCount()
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var count = await Session.QueryOver<EventLogBusinessEntity>()
            .Select(Projections.RowCountInt64())
            .SingleOrDefaultAsync<long>();

        return count;
    }
    
    public async Task<int> DeleteOlderThan(DateTime cutoffDate)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();

        const int maxAttempts = 3;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var deleteCount = await Session.CreateQuery(
                        "delete from EventLogBusinessEntity where Timestamp < :cutoffDate")
                    .SetParameter("cutoffDate", cutoffDate)
                    .ExecuteUpdateAsync();

                await Session.FlushAsync();
                return deleteCount;
            }
            catch (Exception ex) when (attempt < maxAttempts && ex.IsLockContention())
            {
                await Task.Delay(100 * attempt);
            }
        }

        throw new InvalidOperationException("DeleteOlderThan should not reach this point.");
    }
}