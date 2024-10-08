using System.Diagnostics;
using Fig.Api.ExtensionMethods;
using Fig.Api.Observability;
using Fig.Contracts.Authentication;
using Fig.Datalayer.BusinessEntities;
using NHibernate;
using NHibernate.Criterion;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories;

public class ClientStatusRepository : RepositoryBase<ClientStatusBusinessEntity>, IClientStatusRepository
{
    public ClientStatusRepository(ISession session)
        : base(session)
    {
    }

    public ClientStatusBusinessEntity? GetClient(string name, string? instance = null)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<ClientStatusBusinessEntity>();
        criteria.Add(Restrictions.Eq("Name", name));
        criteria.Add(Restrictions.Eq("Instance", instance));
        criteria.SetLockMode(LockMode.Upgrade);
        var client = criteria.UniqueResult<ClientStatusBusinessEntity>();
        return client;
    }

    public void UpdateClientStatus(ClientStatusBusinessEntity clientStatus)
    {
        Update(clientStatus);
    }

    public IList<ClientStatusBusinessEntity> GetAllClients(UserDataContract? requestingUser)
    {
        return GetAll(false)
            .Where(session => requestingUser?.HasAccess(session.Name) == true)
            .ToList();
    }
}