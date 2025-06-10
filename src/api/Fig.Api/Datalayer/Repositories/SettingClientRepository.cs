using System.Diagnostics;
using Fig.Api.ExtensionMethods;
using Fig.Api.Observability;
using Fig.Api.Services;
using Fig.Api.Validators;
using Fig.Contracts.Authentication;
using Fig.Datalayer.BusinessEntities;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Linq;
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
        if (client.DescriptionWrapper != null)
        {
            client.DescriptionWrapper.Client = client;
        }
        return await Save(client);
    }

    public async Task UpdateClient(SettingClientBusinessEntity client)
    {
        client.SerializeAndEncrypt(_encryptionService);
        client.HashCode(_codeHasher);
        if (client.DescriptionWrapper != null)
        {
            client.DescriptionWrapper.Client = client;
        }
        await Update(client);
    }

    public async Task<IList<SettingClientBusinessEntity>> GetAllClients(UserDataContract? requestingUser, bool upgradeLock)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var clients = (await GetAllClients(upgradeLock))
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

    public async Task<IList<SettingClientBusinessEntity>> GetAllClientsWithoutDescription(UserDataContract? requestingUser)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var clients = await Session.Query<SettingClientBusinessEntity>()
            .Fetch(x => x.Settings)
            .Fetch(x => x.RunSessions)
            .Fetch(x => x.CustomActions)
            .ToListAsync(); // No fetch of DescriptionWrapper ó it stays unloaded

        Parallel.ForEach(clients,
            new ParallelOptions { MaxDegreeOfParallelism = 8 },
            c =>
            {
                c.DeserializeAndDecrypt(_encryptionService);
                c.ValidateCodeHash(_codeHasher, _logger);
            });

        await Session.EvictAsync(clients);
        return clients;
    }

    public async Task<SettingClientBusinessEntity?> GetClient(string name, string? instance = null)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();

        var criteria = Session.CreateCriteria<SettingClientBusinessEntity>();

        criteria.CreateAlias("DescriptionWrapper", "desc", NHibernate.SqlCommand.JoinType.LeftOuterJoin);

        criteria.Add(Restrictions.Eq("Name", name));
        criteria.Add(Restrictions.Eq("Instance", instance));
        criteria.SetLockMode(LockMode.Upgrade);

        var client = await criteria.UniqueResultAsync<SettingClientBusinessEntity>();

        client?.DeserializeAndDecrypt(_encryptionService);
        client?.ValidateCodeHash(_codeHasher, _logger);

        return client;
    }

    public async Task<IList<SettingClientBusinessEntity>> GetAllInstancesOfClient(string name)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<SettingClientBusinessEntity>();
        criteria.CreateAlias("DescriptionWrapper", "desc", NHibernate.SqlCommand.JoinType.LeftOuterJoin);
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

    public async Task DeleteClient(SettingClientBusinessEntity client)
    {
        await Delete(client);
    }

    protected async Task<IList<SettingClientBusinessEntity>> GetAllClients(bool upgradeLock)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        if (upgradeLock)
        {
            return await Session.Query<SettingClientBusinessEntity>()
                .WithLock(LockMode.Upgrade)
                .Fetch(a => a.DescriptionWrapper)
                .ToListAsync();
        }

        return await Session.Query<SettingClientBusinessEntity>()
                .Fetch(a => a.DescriptionWrapper)
            .ToListAsync();
    }
}