using System.Diagnostics;
using Fig.Api.ExtensionMethods;
using Fig.Api.Datalayer.Repositories;
using Fig.Common.ExtensionMethods;
using Fig.Common.NetStandard.Json;
using Fig.Contracts.ImportExport;
using Fig.Contracts.Settings;
using Fig.Datalayer.BusinessEntities;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Fig.Api.Services;

public class EncryptionMigrationService : AuthenticatedService, IEncryptionMigrationService
{
    private readonly IEventLogRepository _eventLogRepository;
    private readonly ISettingClientRepository _settingClientRepository;
    private readonly ISettingHistoryRepository _settingHistoryRepository;
    private readonly IWebHookClientRepository _webHookClientRepository;
    private readonly ICheckPointDataRepository _checkPointDataRepository;
    private readonly IDeferredChangeRepository _deferredChangeRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly IApiSecretRotationStateService _apiSecretRotationStateService;
    private readonly IApiStatusRepository _apiStatusRepository;
    private readonly IOptionsMonitor<ApiSettings> _settings;
    private readonly ILogger<EncryptionMigrationService> _logger;

    public EncryptionMigrationService(IEventLogRepository eventLogRepository,
        ISettingClientRepository settingClientRepository,
        ISettingHistoryRepository settingHistoryRepository,
        IWebHookClientRepository webHookClientRepository,
        ICheckPointDataRepository checkPointDataRepository,
        IDeferredChangeRepository deferredChangeRepository,
        IEncryptionService encryptionService,
        IApiSecretRotationStateService apiSecretRotationStateService,
        IApiStatusRepository apiStatusRepository,
        IOptionsMonitor<ApiSettings> settings,
        ILogger<EncryptionMigrationService> logger)
    {
        _eventLogRepository = eventLogRepository;
        _settingClientRepository = settingClientRepository;
        _settingHistoryRepository = settingHistoryRepository;
        _webHookClientRepository = webHookClientRepository;
        _checkPointDataRepository = checkPointDataRepository;
        _deferredChangeRepository = deferredChangeRepository;
        _encryptionService = encryptionService;
        _apiSecretRotationStateService = apiSecretRotationStateService;
        _apiStatusRepository = apiStatusRepository;
        _settings = settings;
        _logger = logger;
    }

    public async Task<Fig.Contracts.ApiSecret.ApiSecretRotationStatusDataContract> GetStatus()
    {
        return await _apiSecretRotationStateService.GetStatus();
    }
    
    public async Task PerformMigration()
    {
        var secretChangeDate = DateTime.UtcNow;
        if (string.IsNullOrWhiteSpace(_settings.CurrentValue.GetDecryptedPreviousSecret()))
        {
            _logger.LogWarning("API secret encryption migration was requested without PreviousSecret configured. Only legacy event log migration can run.");
            // TODO: This was here to fix a bug in 0.9.0. It should be removed in a future version.
            await PerformEventLogMigration(secretChangeDate, false);
            throw new ApplicationException("Logs have been migrated but unable to migrate other parts without a previous secret.");
        }

        var watch = Stopwatch.StartNew();
        _logger.LogInformation("Starting encryption migration...");

        await ValidateActiveApiHosts();
        var snapshot = await _apiSecretRotationStateService.MarkMigrationStarted();
        if (snapshot.Status == ApiSecretRotationMigrationStatus.MigrationCompleted)
        {
            _logger.LogInformation("Encryption migration already completed for this API secret pair.");
            return;
        }

        await _apiSecretRotationStateService.InitializeMigrationProgress(EncryptionMigrationStages.CreateProgress());

        try
        {
            await PerformSettingClientMigration();
            await PerformWebHookClientMigration();
            await PerformEventLogMigration(secretChangeDate);
            await PerformSettingHistoryMigration(secretChangeDate);
            await PerformCheckPointMigration(secretChangeDate);
            await PerformDeferredChangeMigration();

            await _apiSecretRotationStateService.MarkMigrationCompleted();
            _logger.LogInformation(
                "Encryption migration complete in {ElapsedMs}ms. Remove PreviousSecret after confirming all API hosts are aligned; users logged in before the API secret change will be logged out when PreviousSecret is removed.",
                watch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            await _apiSecretRotationStateService.MarkMigrationFailed(ex);
            throw;
        }
    }

    private async Task<int> PerformEventLogMigration(DateTime secretChangeDate, bool trackProgress = true)
    {
        var watch = Stopwatch.StartNew();
        _logger.LogInformation("Starting event log migration for records encrypted on or before {SecretChangeDateUtc}", secretChangeDate);
        if (trackProgress)
            await _apiSecretRotationStateService.MarkMigrationStageStarted(EncryptionMigrationStages.EventLogs, currentItem: "event log batch 1");
        var totalCount = 0;
        var batchNumber = 0;
        int eventLogCount;
        do
        {
            batchNumber++;
            if (trackProgress)
                await _apiSecretRotationStateService.MarkMigrationProgress(
                    EncryptionMigrationStages.EventLogs,
                    totalCount,
                    currentItem: $"event log batch {batchNumber}",
                    force: true);
            var eventLogs = (await _eventLogRepository.GetLogsForEncryptionMigration(secretChangeDate)).ToList();
            if (eventLogs.Any())
            {
                _logger.LogInformation(
                    "Migrating event log batch {BatchNumber} with {BatchCount} record(s). Total migrated before batch: {TotalMigrated}",
                    batchNumber,
                    eventLogs.Count,
                    totalCount);
                await _eventLogRepository.UpdateLogsAfterEncryptionMigration(eventLogs);
            }

            eventLogCount = eventLogs.Count;
            totalCount += eventLogCount;
            if (trackProgress)
                await _apiSecretRotationStateService.MarkMigrationProgress(
                    EncryptionMigrationStages.EventLogs,
                    totalCount,
                    currentItem: eventLogCount > 0 ? $"event log batch {batchNumber + 1}" : null,
                    force: true);

            // Don't hammer the database too much.
            await Task.Delay(100);

        } while (eventLogCount > 0);
        
        if (trackProgress)
            await _apiSecretRotationStateService.MarkMigrationStageCompleted(EncryptionMigrationStages.EventLogs, totalCount);
        _logger.LogInformation(
            "Event log migration complete in {ElapsedMs}ms. Migrated {RecordCount} record(s) across {BatchCount} batch(es)",
            watch.ElapsedMilliseconds,
            totalCount,
            Math.Max(batchNumber - 1, 0));
        return totalCount;
    }

    private async Task<int> PerformSettingClientMigration()
    {
        var watch = Stopwatch.StartNew();
        _logger.LogInformation("Starting setting client migration...");
        await _apiSecretRotationStateService.MarkMigrationStageStarted(EncryptionMigrationStages.SettingClients);
        var settingClients = await _settingClientRepository.GetAllClientsForEncryptionMigration(AuthenticatedUser);
        await _apiSecretRotationStateService.MarkMigrationProgress(
            EncryptionMigrationStages.SettingClients,
            0,
            settingClients.Count,
            force: true);

        for (var index = 0; index < settingClients.Count; index++)
        {
            var client = settingClients[index];
            await _apiSecretRotationStateService.MarkMigrationProgress(
                EncryptionMigrationStages.SettingClients,
                index,
                settingClients.Count,
                FormatClient(client.Name, client.Instance),
                true);
            _logger.LogInformation(
                "Migrating setting client {CurrentClient}/{TotalClients}: {ClientName} instance {Instance} ({ClientId}) with {SettingCount} setting(s)",
                index + 1,
                settingClients.Count,
                client.Name.Sanitize(),
                client.Instance,
                client.Id,
                client.Settings.Count);
            // Saving the client back is enough to encrypt it with the updated secret.
            await _settingClientRepository.UpdateClient(client);
        }
        
        await _apiSecretRotationStateService.MarkMigrationStageCompleted(
            EncryptionMigrationStages.SettingClients,
            settingClients.Count,
            settingClients.Count);
        _logger.LogInformation(
            "Setting client migration complete in {ElapsedMs}ms. Migrated {RecordCount} client(s)",
            watch.ElapsedMilliseconds,
            settingClients.Count);
        return settingClients.Count;
    }

    private async Task<int> PerformSettingHistoryMigration(DateTime secretChangeDate)
    {
        var watch = Stopwatch.StartNew();
        _logger.LogInformation("Starting setting history migration for values encrypted on or before {SecretChangeDateUtc}", secretChangeDate);
        await _apiSecretRotationStateService.MarkMigrationStageStarted(EncryptionMigrationStages.SettingHistory, currentItem: "setting history batch 1");
        var totalCount = 0;
        var batchNumber = 0;
        int valueCount;
        do
        {
            batchNumber++;
            await _apiSecretRotationStateService.MarkMigrationProgress(
                EncryptionMigrationStages.SettingHistory,
                totalCount,
                currentItem: $"setting history batch {batchNumber}",
                force: true);
            var values = (await _settingHistoryRepository.GetValuesForEncryptionMigration(secretChangeDate)).ToList();
            if (values.Any())
            {
                _logger.LogInformation(
                    "Migrating setting history batch {BatchNumber} with {BatchCount} record(s). Total migrated before batch: {TotalMigrated}",
                    batchNumber,
                    values.Count,
                    totalCount);
                await _settingHistoryRepository.UpdateValuesAfterEncryptionMigration(values);
            }

            valueCount = values.Count;
            totalCount += valueCount;
            await _apiSecretRotationStateService.MarkMigrationProgress(
                EncryptionMigrationStages.SettingHistory,
                totalCount,
                currentItem: valueCount > 0 ? $"setting history batch {batchNumber + 1}" : null,
                force: true);

            // Don't hammer the database too much.
            await Task.Delay(100);

        } while (valueCount > 0);
        
        await _apiSecretRotationStateService.MarkMigrationStageCompleted(EncryptionMigrationStages.SettingHistory, totalCount);
        _logger.LogInformation(
            "Setting history migration complete in {ElapsedMs}ms. Migrated {RecordCount} record(s) across {BatchCount} batch(es)",
            watch.ElapsedMilliseconds,
            totalCount,
            Math.Max(batchNumber - 1, 0));
        return totalCount;
    }

    private async Task<int> PerformWebHookClientMigration()
    {
        var watch = Stopwatch.StartNew();
        _logger.LogInformation("Starting web hook client migration...");
        await _apiSecretRotationStateService.MarkMigrationStageStarted(EncryptionMigrationStages.WebHookClients);
        var webHookClients = (await _webHookClientRepository.GetClients(true, true)).ToList();
        await _apiSecretRotationStateService.MarkMigrationProgress(
            EncryptionMigrationStages.WebHookClients,
            0,
            webHookClients.Count,
            force: true);

        for (var index = 0; index < webHookClients.Count; index++)
        {
            var client = webHookClients[index];
            await _apiSecretRotationStateService.MarkMigrationProgress(
                EncryptionMigrationStages.WebHookClients,
                index,
                webHookClients.Count,
                client.Name,
                true);
            _logger.LogInformation(
                "Migrating web hook client {CurrentClient}/{TotalClients}: {ClientName} ({ClientId})",
                index + 1,
                webHookClients.Count,
                client.Name.Sanitize(),
                client.Id);
            // Saving the client back is enough to encrypt it with the updated secret.
            await _webHookClientRepository.UpdateClient(client);
        }
        
        await _apiSecretRotationStateService.MarkMigrationStageCompleted(
            EncryptionMigrationStages.WebHookClients,
            webHookClients.Count,
            webHookClients.Count);
        _logger.LogInformation(
            "Web hook client migration complete in {ElapsedMs}ms. Migrated {RecordCount} client(s)",
            watch.ElapsedMilliseconds,
            webHookClients.Count);
        return webHookClients.Count;
    }
    
    private async Task<int> PerformCheckPointMigration(DateTime secretChangeDate)
    {
        var watch = Stopwatch.StartNew();
        _logger.LogInformation("Starting checkpoint migration for checkpoints encrypted on or before {SecretChangeDateUtc}", secretChangeDate);
        await _apiSecretRotationStateService.MarkMigrationStageStarted(EncryptionMigrationStages.Checkpoints, currentItem: "checkpoint batch 1");
        var totalCount = 0;
        var batchNumber = 0;
        int checkPointCount;
        do
        {
            batchNumber++;
            await _apiSecretRotationStateService.MarkMigrationProgress(
                EncryptionMigrationStages.Checkpoints,
                totalCount,
                currentItem: $"checkpoint batch {batchNumber}",
                force: true);
            var checkPoints = (await _checkPointDataRepository.GetCheckPointsForEncryptionMigration(secretChangeDate)).ToList();
            if (checkPoints.Any())
            {
                _logger.LogInformation(
                    "Migrating checkpoint batch {BatchNumber} with {BatchCount} record(s). Total migrated before batch: {TotalMigrated}",
                    batchNumber,
                    checkPoints.Count,
                    totalCount);
            }

            foreach (var checkPoint in checkPoints)
            {
                MigrateCheckPointData(checkPoint);
            }

            if (checkPoints.Any())
                await _checkPointDataRepository.UpdateCheckPointsAfterEncryptionMigration(checkPoints);

            checkPointCount = checkPoints.Count;
            totalCount += checkPointCount;
            await _apiSecretRotationStateService.MarkMigrationProgress(
                EncryptionMigrationStages.Checkpoints,
                totalCount,
                currentItem: checkPointCount > 0 ? $"checkpoint batch {batchNumber + 1}" : null,
                force: true);

            // Don't hammer the database too much.
            await Task.Delay(100);

        } while (checkPointCount > 0);
        
        await _apiSecretRotationStateService.MarkMigrationStageCompleted(EncryptionMigrationStages.Checkpoints, totalCount);
        _logger.LogInformation(
            "Checkpoint migration complete in {ElapsedMs}ms. Migrated {RecordCount} record(s) across {BatchCount} batch(es)",
            watch.ElapsedMilliseconds,
            totalCount,
            Math.Max(batchNumber - 1, 0));
        return totalCount;
    }

    private void MigrateCheckPointData(CheckPointDataBusinessEntity checkPoint)
    {
        if (checkPoint.ExportAsJson?.TryParseJson(TypeNameHandling.Objects, out FigDataExportDataContract? export) != true)
        {
            _logger.LogWarning("Checkpoint {CheckPointId} did not contain valid export JSON and was left unchanged during encryption migration", checkPoint.Id);
            return;
        }

        var modified = false;
        foreach (var setting in export?.Clients.SelectMany(a => a.Settings) ?? [])
        {
            // Migrate encrypted string secret values
            if (setting.IsSecret && setting.Value is StringSettingDataContract stringSetting)
            {
                var decrypted = _encryptionService.Decrypt(stringSetting.Value, true, false);
                if (decrypted is not null)
                {
                    stringSetting.Value = _encryptionService.Encrypt(decrypted);
                    modified = true;
                }
            }

            // Migrate DataGrid secret column values
            if (setting.DataGridDefinitionJson is not null)
            {
                var dataGridDefinition = JsonConvert.DeserializeObject<Fig.Contracts.SettingDefinitions.DataGridDefinitionDataContract>(
                    setting.DataGridDefinitionJson, JsonSettings.FigDefault);
                if (dataGridDefinition?.Columns.Any(a => a.IsSecret) == true)
                {
                    var dataGridValue = setting.Value?.GetValue() as List<Dictionary<string, object?>>;
                    foreach (var column in dataGridDefinition.Columns.Where(a => a.IsSecret))
                    {
                        foreach (var row in dataGridValue ?? [])
                        {
                            if (row.TryGetValue(column.Name, out var columnValue) && columnValue is not null)
                            {
                                var decrypted = _encryptionService.Decrypt(columnValue.ToString(), true, false);
                                if (decrypted is not null)
                                {
                                    row[column.Name] = _encryptionService.Encrypt(decrypted);
                                    modified = true;
                                }
                            }
                        }
                    }
                }
            }
        }

        if (modified)
        {
            checkPoint.ExportAsJson = JsonConvert.SerializeObject(export, JsonSettings.FigDefault);
        }
    }
    
    private async Task<int> PerformDeferredChangeMigration()
    {
        var watch = Stopwatch.StartNew();
        _logger.LogInformation("Starting deferred change migration...");
        await _apiSecretRotationStateService.MarkMigrationStageStarted(EncryptionMigrationStages.DeferredChanges);
        var deferredChanges = (await _deferredChangeRepository.GetAllChanges()).ToList();
        await _apiSecretRotationStateService.MarkMigrationProgress(
            EncryptionMigrationStages.DeferredChanges,
            0,
            deferredChanges.Count,
            force: true);

        for (var index = 0; index < deferredChanges.Count; index++)
        {
            var change = deferredChanges[index];
            await _apiSecretRotationStateService.MarkMigrationProgress(
                EncryptionMigrationStages.DeferredChanges,
                index,
                deferredChanges.Count,
                FormatClient(change.ClientName, change.Instance),
                true);
            _logger.LogInformation(
                "Migrating deferred change {CurrentChange}/{TotalChanges}: {DeferredChangeId} for client {ClientName} instance {Instance}",
                index + 1,
                deferredChanges.Count,
                change.Id,
                change.ClientName.Sanitize(),
                change.Instance);
            // Saving the change back is enough to encrypt it with the updated secret.
            await _deferredChangeRepository.UpdateDeferredChange(change);
        }
        
        await _apiSecretRotationStateService.MarkMigrationStageCompleted(
            EncryptionMigrationStages.DeferredChanges,
            deferredChanges.Count,
            deferredChanges.Count);
        _logger.LogInformation(
            "Deferred change migration complete in {ElapsedMs}ms. Migrated {RecordCount} record(s)",
            watch.ElapsedMilliseconds,
            deferredChanges.Count);
        return deferredChanges.Count;
    }

    private async Task ValidateActiveApiHosts()
    {
        var activeApis = await _apiStatusRepository.GetAllActive();
        _logger.LogInformation("Validating {ActiveApiCount} active API host(s) before encryption migration", activeApis.Count);
        var mismatchedApis = activeApis
            .Where(api => api.ConfigurationErrorDetected)
            .Select(api => api.Hostname)
            .Where(hostname => !string.IsNullOrWhiteSpace(hostname))
            .Distinct()
            .ToList();

        if (mismatchedApis.Any())
        {
            _logger.LogWarning(
                "Cannot run API secret migration because active API host(s) report different server secrets: {Hosts}",
                string.Join(", ", mismatchedApis));
            throw new InvalidOperationException(
                $"Cannot run API secret migration while active API hosts report different server secrets: {string.Join(", ", mismatchedApis)}.");
        }

        _logger.LogInformation("Active API host validation completed successfully");
    }

    private static string FormatClient(string name, string? instance)
    {
        return string.IsNullOrWhiteSpace(instance)
            ? name
            : $"{name} ({instance})";
    }
}