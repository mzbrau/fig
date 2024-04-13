using Fig.Api.ExtensionMethods;
using Fig.Contracts.Authentication;
using Fig.Datalayer.BusinessEntities;
using NHibernate.Criterion;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories;

public class DeferredClientImportRepository : RepositoryBase<DeferredClientImportBusinessEntity>, IDeferredClientImportRepository
{
    public DeferredClientImportRepository(ISession session) 
        : base(session)
    {
    }

    public IEnumerable<DeferredClientImportBusinessEntity> GetClients(string name, string? instance)
    {
        var criteria = Session.CreateCriteria<DeferredClientImportBusinessEntity>();
        criteria.Add(Restrictions.Eq("Name", name));
        criteria.Add(Restrictions.Eq("Instance", instance));
        var clients = criteria.List<DeferredClientImportBusinessEntity>();
        return clients;
    }

    public void AddClient(DeferredClientImportBusinessEntity client)
    {
        Save(client);
    }

    public void DeleteClient(Guid id)
    {
        var existing = Get(id, true);
        if (existing != null)
            Delete(existing);
    }

    public IEnumerable<DeferredClientImportBusinessEntity> GetAllClients(UserDataContract? requestingUser)
    {
        return GetAll(false).Where(client => requestingUser?.HasAccess(client.Name) == true);
    }
}