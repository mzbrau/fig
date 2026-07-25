using System.Diagnostics;
using Fig.Api.Observability;
using Fig.Datalayer.BusinessEntities;
using NHibernate;
using NHibernate.Criterion;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories;

public class ClientRunSessionRepository : RepositoryBase<ClientRunSessionBusinessEntity>, IClientRunSessionRepository
{
    public ClientRunSessionRepository(ISession session) : base(session)
    {
    }

    public async Task<ClientRunSessionBusinessEntity?> GetRunSession(Guid id)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<ClientRunSessionBusinessEntity>();
        criteria.Add(Restrictions.Eq(nameof(ClientRunSessionBusinessEntity.RunSessionId), id));
        criteria.SetLockMode(LockMode.Upgrade);
        var client = await criteria.UniqueResultAsync<ClientRunSessionBusinessEntity>();
        return client;
    }

    public async Task UpdateRunSession(ClientRunSessionBusinessEntity runSession)
    {
        await Update(runSession);
    }

    public async Task TouchLastSettingLoadUtc(Guid runSessionId, DateTime loadedUtc)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var existingTransaction = Session.GetCurrentTransaction();
        var needsTransaction = existingTransaction == null || !existingTransaction.IsActive;
        var transaction = needsTransaction ? Session.BeginTransaction() : null;
        try
        {
            await Session.CreateQuery(
                    "update ClientRunSessionBusinessEntity s set s.LastSettingLoadUtc = :utc where s.RunSessionId = :id")
                .SetParameter("utc", loadedUtc)
                .SetParameter("id", runSessionId)
                .ExecuteUpdateAsync();

            if (transaction != null)
                await transaction.CommitAsync();
        }
        catch
        {
            if (transaction?.IsActive == true)
                await transaction.RollbackAsync();
            throw;
        }
        finally
        {
            transaction?.Dispose();
        }
    }
}
