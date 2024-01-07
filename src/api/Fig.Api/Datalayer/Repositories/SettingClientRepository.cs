using Fig.Api.ExtensionMethods;
using Fig.Api.Services;
using Fig.Api.Validators;
using Fig.Contracts.Authentication;
using Fig.Datalayer.BusinessEntities;
using NHibernate.Criterion;

namespace Fig.Api.Datalayer.Repositories;

public class SettingClientRepository : RepositoryBase<SettingClientBusinessEntity>, ISettingClientRepository
{
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<SettingClientRepository> _logger;
    private readonly ICodeHasher _codeHasher;

    public SettingClientRepository(IFigSessionFactory sessionFactory,
        IEncryptionService encryptionService,
        ILogger<SettingClientRepository> logger,
        ICodeHasher codeHasher)
        : base(sessionFactory)
    {
        _encryptionService = encryptionService;
        _logger = logger;
        _codeHasher = codeHasher;
    }

    public Guid RegisterClient(SettingClientBusinessEntity client)
    {
        client.SerializeAndEncrypt(_encryptionService);
        client.HashCode(_codeHasher);
        return Save(client);
    }

    public void UpdateClient(SettingClientBusinessEntity client)
    {
        client.SerializeAndEncrypt(_encryptionService);
        client.HashCode(_codeHasher);
        Update(client);
    }

    public IEnumerable<SettingClientBusinessEntity> GetAllClients(UserDataContract? requestingUser)
    {
        var clients = GetAll()
            .Where(client => requestingUser?.HasAccess(client.Name) == true)
            .ToList();
        clients.ForEach(c =>
        {
            c.DeserializeAndDecrypt(_encryptionService);
            c.ValidateCodeHash(_codeHasher, _logger);
        });
        return clients;
    }

    public SettingClientBusinessEntity? GetClient(Guid id)
    {
        var client = Get(id);
        client?.DeserializeAndDecrypt(_encryptionService);
        client?.ValidateCodeHash(_codeHasher, _logger);
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
        client?.ValidateCodeHash(_codeHasher, _logger);
        return client;
    }

    public IEnumerable<SettingClientBusinessEntity> GetAllInstancesOfClient(string name)
    {
        using var session = SessionFactory.OpenSession();
        var criteria = session.CreateCriteria<SettingClientBusinessEntity>();
        criteria.Add(Restrictions.Eq("Name", name));
        var clients = criteria.List<SettingClientBusinessEntity>().ToList();
        clients.ForEach(c =>
        {
            c.DeserializeAndDecrypt(_encryptionService);
            c.ValidateCodeHash(_codeHasher, _logger);
        });
        return clients;
    }

    public void DeleteClient(SettingClientBusinessEntity client)
    {
        Delete(client);
    }
}