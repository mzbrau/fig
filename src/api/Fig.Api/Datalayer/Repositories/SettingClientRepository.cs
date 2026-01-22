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

    public async Task<Guid> RegisterClient(SettingClientBusinessEntity client)
    {
        client.SerializeAndEncrypt(_encryptionService);
        client.HashCode(_codeHasher);
        return await Save(client);
    }

    public async Task UpdateClient(SettingClientBusinessEntity client)
    {
        client.SerializeAndEncrypt(_encryptionService);
        client.HashCode(_codeHasher);
        await Update(client);
    }

    public async Task<IList<SettingClientBusinessEntity>> GetAllClients(UserDataContract? requestingUser, bool upgradeLock)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var clients = (await GetAll(upgradeLock))
            .Where(client => requestingUser?.HasAccess(client.Name) == true)
            .ToList();

        Parallel.ForEach(clients, 
            new ParallelOptions { MaxDegreeOfParallelism = 8 },
            c =>
            {
                c.DeserializeAndDecrypt(_encryptionService);
                c.ValidateCodeHash(_codeHasher, _logger);
            });

        if (!upgradeLock)
        {
            await Session.EvictAsync(clients);
        }
        
        return clients;
    }

    public async Task<SettingClientBusinessEntity?> GetClient(string name, string? instance = null)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<SettingClientBusinessEntity>();
        criteria.Add(Restrictions.Eq(nameof(SettingClientBusinessEntity.Name), name));
        criteria.Add(Restrictions.Eq(nameof(SettingClientBusinessEntity.Instance), instance));
        criteria.SetLockMode(LockMode.Upgrade);
        var client = await criteria.UniqueResultAsync<SettingClientBusinessEntity>();
        client?.DeserializeAndDecrypt(_encryptionService);
        client?.ValidateCodeHash(_codeHasher, _logger);
        return client;
    }

    public async Task<SettingClientBusinessEntity?> GetClientReadOnly(string name, string? instance = null)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<SettingClientBusinessEntity>();
        criteria.Add(Restrictions.Eq(nameof(SettingClientBusinessEntity.Name), name));
        criteria.Add(Restrictions.Eq(nameof(SettingClientBusinessEntity.Instance), instance));
        // No LockMode.Upgrade for read-only operations to reduce contention
        var client = await criteria.UniqueResultAsync<SettingClientBusinessEntity>();
        client?.DeserializeAndDecrypt(_encryptionService);
        client?.ValidateCodeHash(_codeHasher, _logger);
        
        if (client != null)
        {
            await Session.EvictAsync(client);
        }
        
        return client;
    }

    public async Task<IList<SettingClientBusinessEntity>> GetAllInstancesOfClient(string name)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<SettingClientBusinessEntity>();
        criteria.Add(Restrictions.Eq("Name", name));
        criteria.SetLockMode(LockMode.Upgrade);
        var clients = (await criteria.ListAsync<SettingClientBusinessEntity>()).ToList();

        Parallel.ForEach(clients, 
            new ParallelOptions { MaxDegreeOfParallelism = 4 },
            c =>
            {
                c.DeserializeAndDecrypt(_encryptionService);
                c.ValidateCodeHash(_codeHasher, _logger);
            });
        
        return clients;
    }

    public async Task<IList<SettingClientBusinessEntity>> GetAllInstancesOfClientReadOnly(string name)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<SettingClientBusinessEntity>();
        criteria.Add(Restrictions.Eq(nameof(SettingClientBusinessEntity.Name), name));
        // No LockMode.Upgrade for read-only operations to reduce contention
        var clients = (await criteria.ListAsync<SettingClientBusinessEntity>()).ToList();

        Parallel.ForEach(clients, 
            new ParallelOptions { MaxDegreeOfParallelism = 4 },
            c =>
            {
                c.DeserializeAndDecrypt(_encryptionService);
                c.ValidateCodeHash(_codeHasher, _logger);
            });
        
        // Evict entities since they won't be modified
        foreach (var client in clients)
        {
            await Session.EvictAsync(client);
        }
        
        return clients;
    }

    public async Task DeleteClient(SettingClientBusinessEntity client)
    {
        await Delete(client);
    }

    public async Task<IList<(string Name, string Description)>> GetClientDescriptions(UserDataContract? requestingUser)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        
        // Use Criteria API with projections to handle lazy-loaded Description field properly
        var criteria = Session.CreateCriteria<SettingClientBusinessEntity>();
        criteria.SetProjection(Projections.ProjectionList()
            .Add(Projections.Property("Name"), "Name")
            .Add(Projections.Property("Description"), "Description"));
        criteria.AddOrder(Order.Asc("Name"));
        
        var results = await criteria.ListAsync<object[]>();
        
        var clientDescriptions = results
            .Select(row => (Name: (string)row[0], Description: (string)(row[1] ?? string.Empty)))
            .Where(client => requestingUser?.HasAccess(client.Name) == true)
            .ToList();

        return clientDescriptions;
    }
}