using Fig.Datalayer.BusinessEntities;
using NHibernate.Criterion;

namespace Fig.Api.Datalayer.Repositories;

public class DeferredClientImportRepository : RepositoryBase<DeferredClientImportBusinessEntity>, IDeferredClientImportRepository
{
    public DeferredClientImportRepository(IFigSessionFactory sessionFactory) 
        : base(sessionFactory)
    {
    }

    public DeferredClientImportBusinessEntity? GetClient(string name, string? instance)
    {
        using var session = SessionFactory.OpenSession();
        var criteria = session.CreateCriteria<DeferredClientImportBusinessEntity>();
        criteria.Add(Restrictions.Eq("Name", name));
        criteria.Add(Restrictions.Eq("Instance", instance));
        var client = criteria.UniqueResult<DeferredClientImportBusinessEntity>();
        return client;
    }

    public void SaveClient(DeferredClientImportBusinessEntity client)
    {
        var existing = GetClient(client.Name, client.Instance);
        if (existing != null)
            Delete(existing);
        
        Save(client);
    }

    public void DeleteClient(string clientName, string? instance)
    {
        var existing = GetClient(clientName, instance);
        if (existing != null)
            Delete(existing);
    }

    public IEnumerable<DeferredClientImportBusinessEntity> GetAllClients()
    {
        return GetAll();
    }
}