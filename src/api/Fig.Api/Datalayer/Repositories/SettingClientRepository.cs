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
    
    public async Task<IList<SettingClientBusinessEntity>> GetAllClientsWithoutDescription(UserDataContract? requestingUser)
    {
        if (requestingUser is null)
            return [];
        
        using Activity? activity = ApiActivitySource.Instance.StartActivity();

        // Project all properties except Description
        var query = Session.Query<SettingClientBusinessEntity>()
            .Select(client => new SettingClientBusinessEntity
            {
                Id = client.Id,
                Name = client.Name,
                Instance = client.Instance,
                ClientSecret = client.ClientSecret,
                PreviousClientSecret = client.PreviousClientSecret,
                PreviousClientSecretExpiryUtc = client.PreviousClientSecretExpiryUtc,
                LastRegistration = client.LastRegistration,
                LastSettingValueUpdate = client.LastSettingValueUpdate,
                Settings = client.Settings,
                RunSessions = client.RunSessions,
                CustomActions = client.CustomActions
                // Exclude Description
            });

        var clients = await query.ToListAsync();
        
        clients = clients.Where(client => requestingUser.HasAccess(client.Name)).ToList();
        
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
        criteria.Add(Restrictions.Eq(nameof(SettingClientBusinessEntity.Name), name));
        criteria.Add(Restrictions.Eq(nameof(SettingClientBusinessEntity.Instance), instance));
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
}