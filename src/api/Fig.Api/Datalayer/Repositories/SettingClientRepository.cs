using System.Diagnostics;
using Fig.Api.ExtensionMethods;
using Fig.Api.Observability;
using Fig.Api.Services;
using Fig.Api.Validators;
using Fig.Contracts.Authentication;
using Fig.Datalayer.BusinessEntities;
using System.Collections.Concurrent;
using NHibernate;
using NHibernate.Criterion;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories;

public class SettingClientRepository : RepositoryBase<SettingClientBusinessEntity>, ISettingClientRepository
{
    private const long SlowOperationWarningMs = 1000;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<SettingClientRepository> _logger;
    private readonly ICodeHasher _codeHasher;
    private readonly IApiSecretRotationStateService _apiSecretRotationStateService;

    public SettingClientRepository(ISession session,
        IEncryptionService encryptionService,
        ILogger<SettingClientRepository> logger,
        ICodeHasher codeHasher,
        IApiSecretRotationStateService apiSecretRotationStateService)
        : base(session)
    {
        _encryptionService = encryptionService;
        _logger = logger;
        _codeHasher = codeHasher;
        _apiSecretRotationStateService = apiSecretRotationStateService;
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

    public async Task<IList<SettingClientBusinessEntity>> GetAllClients(UserDataContract? requestingUser, bool upgradeLock = false, bool validateCode = true)
    {
        return await GetAllClients(requestingUser, upgradeLock, validateCode, false);
    }

    public async Task<IList<SettingClientBusinessEntity>> GetAllClientsForEncryptionMigration(UserDataContract? requestingUser)
    {
        return await GetAllClients(requestingUser, true, false, true);
    }

    public async Task<SettingClientReadResult> GetAllClientsBestEffort(UserDataContract? requestingUser, bool validateCode = true)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var totalWatch = Stopwatch.StartNew();
        var queryWatch = Stopwatch.StartNew();
        List<SettingClientBusinessEntity> persistedClients;
        try
        {
            persistedClients = (await GetAll(false))
                .Where(client => requestingUser?.HasAccess(client.Name) == true)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to query setting clients for best-effort load after {ElapsedMs} ms. LockContentionDetected={LockContentionDetected}",
                queryWatch.ElapsedMilliseconds,
                ex.IsLockContention());
            throw;
        }

        LogSlowOperation("Best-effort setting client query", queryWatch.ElapsedMilliseconds, persistedClients.Count);
        var clients = persistedClients.Select(CloneForBestEffortRead).ToList();
        var rotationSnapshot = await _apiSecretRotationStateService.GetSnapshot();
        var tryFallbackFirst = rotationSnapshot.KeyOrder == ApiSecretKeyOrder.PreviousThenCurrent;
        _logger.LogDebug(
            "Best-effort setting client load using API secret key order {KeyOrder} with migration status {MigrationStatus}",
            rotationSnapshot.KeyOrder,
            rotationSnapshot.Status);

        var failures = new ConcurrentBag<SettingClientReadFailure>();
        var failedClientIds = new ConcurrentDictionary<Guid, byte>();

        var decryptWatch = Stopwatch.StartNew();
        Parallel.ForEach(clients,
            new ParallelOptions { MaxDegreeOfParallelism = GetMaxDecryptDegreeOfParallelism(8) },
            c =>
            {
                try
                {
                    c.DeserializeAndDecryptBestEffort(_encryptionService,
                        (setting, ex) =>
                        {
                            failures.Add(new SettingClientReadFailure(
                                c.Name,
                                c.Instance,
                                setting.Name,
                                "Setting value could not be decrypted and was omitted from this response.",
                                ex));
                            _logger.LogError(ex,
                                "Failed to decrypt setting {SettingName} for client {ClientName} instance {Instance}. Setting was omitted from this response.",
                                setting.Name,
                                c.Name.Sanitize(),
                                c.Instance);
                        },
                        tryFallbackFirst,
                        ValidatedDecryptionMode.FirstValid);

                    if (validateCode)
                        c.ValidateCodeHash(_codeHasher, _logger);
                }
                catch (Exception ex)
                {
                    failures.Add(new SettingClientReadFailure(
                        c.Name,
                        c.Instance,
                        null,
                        "Client could not be loaded and was omitted from this response.",
                        ex));
                    failedClientIds.TryAdd(c.Id, 0);
                    _logger.LogError(ex,
                        "Failed to load client {ClientName} instance {Instance}. Client was omitted from this response.",
                        c.Name.Sanitize(),
                        c.Instance);
                }
            });

        var successfulClients = clients
            .Where(client => !failedClientIds.ContainsKey(client.Id))
            .ToList();

        await Session.EvictAsync(persistedClients);
        LogSlowOperation(
            $"Best-effort setting client decrypt and validation (keyOrder={rotationSnapshot.KeyOrder}, mode={ValidatedDecryptionMode.FirstValid})",
            decryptWatch.ElapsedMilliseconds,
            clients.Count);
        LogSlowOperation("Best-effort setting client load total", totalWatch.ElapsedMilliseconds, clients.Count);
        return new SettingClientReadResult(successfulClients, failures.ToList());
    }

    private async Task<IList<SettingClientBusinessEntity>> GetAllClients(UserDataContract? requestingUser,
        bool upgradeLock,
        bool validateCode,
        bool tryFallbackFirst)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var totalWatch = Stopwatch.StartNew();
        var queryWatch = Stopwatch.StartNew();
        List<SettingClientBusinessEntity> clients;
        try
        {
            clients = (await GetAll(upgradeLock))
                .Where(client => requestingUser?.HasAccess(client.Name) == true)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to query setting clients after {ElapsedMs} ms. UpgradeLock={UpgradeLock}. LockContentionDetected={LockContentionDetected}",
                queryWatch.ElapsedMilliseconds,
                upgradeLock,
                ex.IsLockContention());
            throw;
        }

        LogSlowOperation($"Setting client query (upgradeLock={upgradeLock})", queryWatch.ElapsedMilliseconds, clients.Count);

        var decryptWatch = Stopwatch.StartNew();
        Parallel.ForEach(clients,
            new ParallelOptions { MaxDegreeOfParallelism = GetMaxDecryptDegreeOfParallelism(8) },
            c =>
            {
                c.DeserializeAndDecrypt(_encryptionService, tryFallbackFirst);
                if (validateCode)
                    c.ValidateCodeHash(_codeHasher, _logger);
            });
        LogSlowOperation($"Setting client decrypt and validation (validateCode={validateCode}, tryFallbackFirst={tryFallbackFirst})",
            decryptWatch.ElapsedMilliseconds,
            clients.Count);

        if (!upgradeLock)
        {
            await Session.EvictAsync(clients);
        }

        LogSlowOperation("Setting client load total", totalWatch.ElapsedMilliseconds, clients.Count);
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

    public async Task<IList<SettingClientBusinessEntity>> GetAllInstancesOfClient(string name, bool upgradeLock = true)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<SettingClientBusinessEntity>();
        criteria.Add(Restrictions.Eq("Name", name));
        if (upgradeLock)
            criteria.SetLockMode(LockMode.Upgrade);
        var queryWatch = Stopwatch.StartNew();
        List<SettingClientBusinessEntity> clients;
        try
        {
            clients = (await criteria.ListAsync<SettingClientBusinessEntity>()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to query instances for client {ClientName} after {ElapsedMs} ms. UpgradeLock={UpgradeLock}. LockContentionDetected={LockContentionDetected}",
                name.Sanitize(),
                queryWatch.ElapsedMilliseconds,
                upgradeLock,
                ex.IsLockContention());
            throw;
        }
        LogSlowOperation($"Setting client instance query for {name.Sanitize()} (upgradeLock={upgradeLock})",
            queryWatch.ElapsedMilliseconds,
            clients.Count);

        var decryptWatch = Stopwatch.StartNew();
        Parallel.ForEach(clients,
            new ParallelOptions { MaxDegreeOfParallelism = GetMaxDecryptDegreeOfParallelism(4) },
            c =>
            {
                c.DeserializeAndDecrypt(_encryptionService);
            });
        LogSlowOperation($"Setting client instance decrypt for {name.Sanitize()}",
            decryptWatch.ElapsedMilliseconds,
            clients.Count);

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

    private static SettingClientBusinessEntity CloneForBestEffortRead(SettingClientBusinessEntity client)
    {
        return new SettingClientBusinessEntity
        {
            Id = client.Id,
            Name = client.Name,
            Instance = client.Instance,
            ClientSecret = client.ClientSecret,
            PreviousClientSecret = client.PreviousClientSecret,
            PreviousClientSecretExpiryUtc = client.PreviousClientSecretExpiryUtc,
            LastRegistration = client.LastRegistration,
            LastSettingValueUpdate = client.LastSettingValueUpdate,
            Settings = client.Settings.Select(CloneSettingForBestEffortRead).ToList(),
            CustomActions = client.CustomActions.Select(CloneCustomActionForBestEffortRead).ToList()
        };
    }

    private static SettingBusinessEntity CloneSettingForBestEffortRead(SettingBusinessEntity setting)
    {
        return new SettingBusinessEntity
        {
            Id = setting.Id,
            Name = setting.Name,
            Description = setting.Description,
            IsSecret = setting.IsSecret,
            ValueType = setting.ValueType,
            ValueAsJson = setting.ValueAsJson,
            DefaultValue = setting.DefaultValue,
            JsonSchema = setting.JsonSchema,
            ValidationRegex = setting.ValidationRegex,
            ValidationExplanation = setting.ValidationExplanation,
            ValidValues = setting.ValidValues,
            Group = setting.Group,
            DisplayOrder = setting.DisplayOrder,
            Advanced = setting.Advanced,
            LookupTableKey = setting.LookupTableKey,
            EditorLineCount = setting.EditorLineCount,
            DataGridDefinitionJson = setting.DataGridDefinitionJson,
            EnablesSettings = setting.EnablesSettings,
            SupportsLiveUpdate = setting.SupportsLiveUpdate,
            LastChanged = setting.LastChanged,
            CategoryName = setting.CategoryName,
            CategoryColor = setting.CategoryColor,
            DisplayScript = setting.DisplayScript,
            DisplayScriptHash = setting.DisplayScriptHash,
            DisplayScriptHashRequired = setting.DisplayScriptHashRequired,
            IsExternallyManaged = setting.IsExternallyManaged,
            Classification = setting.Classification,
            EnvironmentSpecific = setting.EnvironmentSpecific,
            LookupKeySettingName = setting.LookupKeySettingName,
            Indent = setting.Indent,
            DependsOnProperty = setting.DependsOnProperty,
            DependsOnValidValues = setting.DependsOnValidValues,
            Heading = setting.Heading,
            InitOnlyExport = setting.InitOnlyExport,
            MigrateFrom = setting.MigrateFrom,
            MigrateFromMigrationMethod = setting.MigrateFromMigrationMethod
        };
    }

    private static CustomActionBusinessEntity CloneCustomActionForBestEffortRead(CustomActionBusinessEntity customAction)
    {
        return new CustomActionBusinessEntity
        {
            Id = customAction.Id,
            Name = customAction.Name,
            ButtonName = customAction.ButtonName,
            Description = customAction.Description,
            SettingsUsed = customAction.SettingsUsed,
            Classification = customAction.Classification,
            ClientName = customAction.ClientName,
            ClientReference = customAction.ClientReference
        };
    }

    private void LogSlowOperation(string operation, long elapsedMs, int clientCount)
    {
        if (elapsedMs < SlowOperationWarningMs)
            return;

        _logger.LogWarning(
            "Slow {Operation} completed in {ElapsedMs} ms for {ClientCount} client(s)",
            operation,
            elapsedMs,
            clientCount);
    }

    private static int GetMaxDecryptDegreeOfParallelism(int upperBound)
    {
        return Math.Max(1, Math.Min(upperBound, Math.Max(1, Environment.ProcessorCount / 2)));
    }
}