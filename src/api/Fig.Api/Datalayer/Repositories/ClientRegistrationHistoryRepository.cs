using System.Diagnostics;
using Fig.Api.Observability;
using Fig.Datalayer.BusinessEntities;
using NHibernate;
using NHibernate.Linq;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories;

public class ClientRegistrationHistoryRepository : RepositoryBase<ClientRegistrationHistoryBusinessEntity>, IClientRegistrationHistoryRepository
{
    public ClientRegistrationHistoryRepository(ISession session)
        : base(session)
    {
    }

    public async Task Add(ClientRegistrationHistoryBusinessEntity history)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        await Save(history);
    }

    public async Task<IList<ClientRegistrationHistoryBusinessEntity>> GetAll()
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        return await Session.Query<ClientRegistrationHistoryBusinessEntity>()
            .OrderByDescending(x => x.RegistrationDateUtc)
            .ToListAsync();
    }

    public async Task ClearAll()
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        await Session.CreateQuery("delete from ClientRegistrationHistoryBusinessEntity")
            .ExecuteUpdateAsync();
        await Session.FlushAsync();
    }
}
