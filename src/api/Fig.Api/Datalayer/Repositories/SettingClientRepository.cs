using System.Diagnostics;
using Fig.Api.ExtensionMethods;
using Fig.Api.Observability;
using Fig.Api.Services;
using Fig.Api.Validators;
using Fig.Contracts.Authentication;
using Fig.Datalayer.BusinessEntities;
using NHibernate;
using NHibernate.Criterion;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories;

public class SettingClientRepository : RepositoryBase<SettingClientBusinessEntity>, ISettingClientRepository
{
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<SettingClientRepository> _logger;
    private readonly ICodeHasher _codeHasher;

    public SettingClientRepository(ISession session,
        IEncryptionService encryptionService,
        ILogger<SettingClientRepository> logger,
        ICodeHasher codeHasher)
        : base(session)
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

    public IList<SettingClientBusinessEntity> GetAllClients(UserDataContract? requestingUser, bool upgradeLock)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var clients = GetAll(upgradeLock)
            .Where(client => requestingUser?.HasAccess(client.Name) == true)
            .ToList();
        clients.ForEach(c =>
        {
            c.DeserializeAndDecrypt(_encryptionService);
            c.ValidateCodeHash(_codeHasher, _logger);
        });
        return clients;
    }

    public SettingClientBusinessEntity? GetClient(string name, string? instance = null)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<SettingClientBusinessEntity>();
        criteria.Add(Restrictions.Eq("Name", name));
        criteria.Add(Restrictions.Eq("Instance", instance));
        criteria.SetLockMode(LockMode.Upgrade);
        var client = criteria.UniqueResult<SettingClientBusinessEntity>();
        client?.DeserializeAndDecrypt(_encryptionService);
        client?.ValidateCodeHash(_codeHasher, _logger);
        return client;
    }

    public IList<SettingClientBusinessEntity> GetAllInstancesOfClient(string name)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<SettingClientBusinessEntity>();
        criteria.Add(Restrictions.Eq("Name", name));
        criteria.SetLockMode(LockMode.Upgrade);
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