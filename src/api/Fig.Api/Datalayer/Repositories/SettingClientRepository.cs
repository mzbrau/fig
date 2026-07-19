using System.Diagnostics;
using Fig.Api.ExtensionMethods;
using Fig.Api.Observability;
using Fig.Api.Services;
using Fig.Api.Validators;
using Fig.Contracts.Authentication;
using Fig.Datalayer.BusinessEntities;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using Newtonsoft.Json;
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

    public async Task<IList<SettingClientBusinessEntity>> GetAllClientsForEncryptionMigration(UserDataContract? requestingUser,
        Action<SettingClientMigrationLoadProgress>? progress = null)
    {
        return await GetAllClients(requestingUser, true, false, true, progress);
    }

    public async Task<SettingClientReadResult> GetAllClientsBestEffort(UserDataContract? requestingUser, bool validateCode = true)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity("GetAllClientsBestEffort");
        var totalWatch = Stopwatch.StartNew();
        var queryWatch = Stopwatch.StartNew();
        List<SettingClientBusinessEntity> persistedClients;
        try
        {
            using (Activity? queryActivity = ApiActivitySource.Instance.StartActivity("QueryClients"))
            {
                persistedClients = (await GetAll(false))
                    .Where(client => requestingUser?.HasAccess(client.Name) == true)
                    .ToList();
                queryActivity?.SetTag("fig.api.client_count", persistedClients.Count);
                queryActivity?.SetTag("fig.api.setting_count",
                    persistedClients.Sum(c => c.Settings?.Count ?? 0));
                queryActivity?.SetTag("fig.api.elapsed_ms", queryWatch.ElapsedMilliseconds);
                queryActivity?.SetTag("fig.api.query_elapsed_ms", queryWatch.ElapsedMilliseconds);
            }
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
        var tryFallbackFirst = await _apiSecretRotationStateService.ShouldTryFallbackSecretFirstAsync();
        _logger.LogDebug(
            "Best-effort setting client load with tryFallbackFirst={TryFallbackFirst}",
            tryFallbackFirst);

        var failures = new ConcurrentBag<SettingClientReadFailure>();
        var failedClientIds = new ConcurrentDictionary<Guid, byte>();

        var decryptWatch = Stopwatch.StartNew();
        using (Activity? decryptActivity = ApiActivitySource.Instance.StartActivity("DecryptAndValidate"))
        {
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

            decryptActivity?.SetTag("fig.api.client_count", clients.Count);
            decryptActivity?.SetTag("fig.api.setting_count", clients.Sum(c => c.Settings?.Count ?? 0));
            decryptActivity?.SetTag("fig.api.elapsed_ms", decryptWatch.ElapsedMilliseconds);
            decryptActivity?.SetTag("fig.api.decrypt_elapsed_ms", decryptWatch.ElapsedMilliseconds);
        }

        var successfulClients = clients
            .Where(client => !failedClientIds.ContainsKey(client.Id))
            .ToList();

        await EvictAll(persistedClients);
        LogSlowOperation(
            $"Best-effort setting client decrypt and validation (tryFallbackFirst={tryFallbackFirst}, mode={ValidatedDecryptionMode.FirstValid})",
            decryptWatch.ElapsedMilliseconds,
            clients.Count);
        LogSlowOperation("Best-effort setting client load total", totalWatch.ElapsedMilliseconds, clients.Count);

        activity?.SetTag("fig.api.client_count", successfulClients.Count);
        activity?.SetTag("fig.api.load_failure_count", failures.Count);
        activity?.SetTag("fig.api.total_elapsed_ms", totalWatch.ElapsedMilliseconds);

        return new SettingClientReadResult(successfulClients, failures.ToList());
    }

    private async Task<IList<SettingClientBusinessEntity>> GetAllClients(UserDataContract? requestingUser,
        bool upgradeLock,
        bool validateCode,
        bool tryFallbackFirst,
        Action<SettingClientMigrationLoadProgress>? migrationProgress = null)
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
        var keyOrder = tryFallbackFirst ? ApiSecretKeyOrder.PreviousThenCurrent : ApiSecretKeyOrder.CurrentThenPrevious;
        var decryptedClients = 0;
        Parallel.ForEach(clients,
            new ParallelOptions { MaxDegreeOfParallelism = GetMaxDecryptDegreeOfParallelism(8) },
            c =>
            {
                try
                {
                    c.DeserializeAndDecrypt(_encryptionService, tryFallbackFirst);
                    if (validateCode)
                        c.ValidateCodeHash(_codeHasher, _logger);

                    if (migrationProgress is not null)
                    {
                        var processedClients = Interlocked.Increment(ref decryptedClients);
                        migrationProgress(new SettingClientMigrationLoadProgress(
                            processedClients,
                            clients.Count,
                            c.Name,
                            c.Instance));
                        _logger.LogInformation(
                            "Prepared setting client {CurrentClient}/{TotalClients} for encryption migration: {ClientName} instance {Instance} ({ClientId}) with {SettingCount} setting(s)",
                            processedClients,
                            clients.Count,
                            c.Name.Sanitize(),
                            c.Instance,
                            c.Id,
                            c.Settings.Count);
                    }
                }
                catch (Exception ex) when (ex is JsonException or CryptographicException)
                {
                    _logger.LogError(ex,
                        "Failed strict setting client decrypt for client {ClientName} instance {Instance} ({ClientId}). KeyOrder={KeyOrder}; Mode={Mode}",
                        c.Name.Sanitize(),
                        c.Instance,
                        c.Id,
                        keyOrder,
                        ValidatedDecryptionMode.Strict);
                    throw new InvalidOperationException(
                        $"Failed strict setting client decrypt for client {c.Name} instance {c.Instance} ({c.Id}). KeyOrder={keyOrder}; Mode={ValidatedDecryptionMode.Strict}.",
                        ex);
                }
            });
        LogSlowOperation($"Setting client decrypt and validation (validateCode={validateCode}, keyOrder={keyOrder}, mode={ValidatedDecryptionMode.Strict})",
            decryptWatch.ElapsedMilliseconds,
            clients.Count);

        if (!upgradeLock)
        {
            InitializeLazyProperties(clients);
            await EvictAll(clients);
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

    public async Task<SettingClientBusinessEntity?> GetClientForDeletion(string name, string? instance = null)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<SettingClientBusinessEntity>();
        criteria.Add(Restrictions.Eq(nameof(SettingClientBusinessEntity.Name), name));
        criteria.Add(Restrictions.Eq(nameof(SettingClientBusinessEntity.Instance), instance));
        criteria.SetLockMode(LockMode.Upgrade);
        return await criteria.UniqueResultAsync<SettingClientBusinessEntity>();
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

    public async Task<bool> HasAnyInstancesOfClient(string name)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<SettingClientBusinessEntity>();
        criteria.Add(Restrictions.Eq(nameof(SettingClientBusinessEntity.Name), name));
        criteria.SetProjection(Projections.RowCountInt64());

        return await criteria.UniqueResultAsync<long>() > 0;
    }

    public async Task DeleteClient(SettingClientBusinessEntity client)
    {
        await Delete(client);
    }

    public async Task<IList<(string Name, string Description)>> GetClientDescriptions(UserDataContract? requestingUser)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity("GetClientDescriptions");
        var stopwatch = Stopwatch.StartNew();

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

        activity?.SetTag("fig.api.client_count", clientDescriptions.Count);
        activity?.SetTag("fig.api.elapsed_ms", stopwatch.ElapsedMilliseconds);

        return clientDescriptions;
    }

    private static SettingClientBusinessEntity CloneForBestEffortRead(SettingClientBusinessEntity client)
    {
        return new SettingClientBusinessEntity
        {
            Id = client.Id,
            Name = client.Name,
            // Omit Description: GetAllClients discards it (loaded later via /clients/descriptions).
            // Accessing the lazy StringClob here would hydrate large markdown for every client.
            Description = string.Empty,
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

    private static void InitializeLazyProperties(IEnumerable<SettingClientBusinessEntity> clients)
    {
        foreach (var client in clients)
        {
            _ = client.Description;
        }
    }

    private async Task EvictAll(IEnumerable<SettingClientBusinessEntity> clients)
    {
        foreach (var client in clients)
        {
            await Session.EvictAsync(client);
        }
    }
}