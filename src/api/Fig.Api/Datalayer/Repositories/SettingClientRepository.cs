using Fig.Api.ExtensionMethods;
using Fig.Api.Services;
using Fig.Contracts.Authentication;
using Fig.Datalayer.BusinessEntities;
using NHibernate.Criterion;

namespace Fig.Api.Datalayer.Repositories;

public class SettingClientRepository : RepositoryBase<SettingClientBusinessEntity>, ISettingClientRepository
{
    private readonly IEncryptionService _encryptionService;

    public SettingClientRepository(IFigSessionFactory sessionFactory, IEncryptionService encryptionService)
        : base(sessionFactory)
    {
        _encryptionService = encryptionService;
    }

    public Guid RegisterClient(SettingClientBusinessEntity client)
    {
        client.SerializeAndEncrypt(_encryptionService);
        return Save(client);
    }

    public void UpdateClient(SettingClientBusinessEntity client)
    {
        client.SerializeAndEncrypt(_encryptionService);
        Update(client);
    }

    public IEnumerable<SettingClientBusinessEntity> GetAllClients(UserDataContract? requestingUser)
    {
        var clients = GetAll()
            .Where(client => requestingUser?.HasAccess(client.Name) == true)
            .ToList();
        clients.ForEach(c => c.DeserializeAndDecrypt(_encryptionService));
        return clients;
    }

    public SettingClientBusinessEntity? GetClient(Guid id)
    {
        var client = Get(id);
        client?.DeserializeAndDecrypt(_encryptionService);
        return client;
    }

    public SettingClientBusinessEntity? GetClient(string name, string? instance = null)
    {
        using var session = SessionFactory.OpenSession();
        var criteria = session.CreateCriteria<SettingClientBusinessEntity>();
        criteria.Add(Restrictions.Eq("Name", name));
        criteria.Add(Restrictions.Eq("Instance", instance));
        var client = criteria.UniqueResult<SettingClientBusinessEntity>();
        client?.DeserializeAndDecrypt(_encryptionService);
        return client;
    }

    public IEnumerable<SettingClientBusinessEntity> GetAllInstancesOfClient(string name)
    {
        using var session = SessionFactory.OpenSession();
        var criteria = session.CreateCriteria<SettingClientBusinessEntity>();
        criteria.Add(Restrictions.Eq("Name", name));
        var clients = criteria.List<SettingClientBusinessEntity>().ToList();
        clients.ForEach(c => c.DeserializeAndDecrypt(_encryptionService));
        return clients;
    }

    public void DeleteClient(SettingClientBusinessEntity client)
    {
        Delete(client);
    }
}