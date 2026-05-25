using System.Diagnostics;
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
            // TODO: This was here to fix a bug in 0.9.0. It should be removed in a future version.
            await PerformEventLogMigration(secretChangeDate);
            throw new ApplicationException("Logs have been migrated but unable to migrate other parts without a previous secret.");
        }

        var watch = Stopwatch.StartNew();
        _logger.LogInformation("Starting encryption migration...");

        await ValidateActiveApiHosts();
        var snapshot = await _apiSecretRotationStateService.MarkMigrationStarted();
        if (snapshot.Status == ApiSecretRotationMigrationStatus.MigrationCompleted)
            return;

        try
        {
            var settingClientCount = await PerformSettingClientMigration();
            await _apiSecretRotationStateService.MarkMigrationStageCompleted("Setting clients", settingClientCount);

            var webHookClientCount = await PerformWebHookClientMigration();
            await _apiSecretRotationStateService.MarkMigrationStageCompleted("Web hook clients", webHookClientCount);

            var eventLogCount = await PerformEventLogMigration(secretChangeDate);
            await _apiSecretRotationStateService.MarkMigrationStageCompleted("Event logs", eventLogCount);

            var settingHistoryCount = await PerformSettingHistoryMigration(secretChangeDate);
            await _apiSecretRotationStateService.MarkMigrationStageCompleted("Setting history", settingHistoryCount);

            var checkPointCount = await PerformCheckPointMigration(secretChangeDate);
            await _apiSecretRotationStateService.MarkMigrationStageCompleted("Checkpoints", checkPointCount);

            var deferredChangeCount = await PerformDeferredChangeMigration();
            await _apiSecretRotationStateService.MarkMigrationStageCompleted("Deferred changes", deferredChangeCount);

            await _apiSecretRotationStateService.MarkMigrationCompleted();
            _logger.LogInformation("Encryption migration complete in {ElapsedMs}ms", watch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            await _apiSecretRotationStateService.MarkMigrationFailed(ex);
            throw;
        }
    }

    private async Task<int> PerformEventLogMigration(DateTime secretChangeDate)
    {
        _logger.LogInformation("Starting event log migration...");
        var totalCount = 0;
        int eventLogCount;
        do
        {
            var eventLogs = (await _eventLogRepository.GetLogsForEncryptionMigration(secretChangeDate)).ToList();
            if (eventLogs.Any())
                await _eventLogRepository.UpdateLogsAfterEncryptionMigration(eventLogs);

            eventLogCount = eventLogs.Count;
            totalCount += eventLogCount;
            
            // Don't hammer the database too much.
            Thread.Sleep(100);

        } while (eventLogCount > 0);
        
        _logger.LogInformation("Event log migration complete. Migrated {RecordCount} record(s)", totalCount);
        return totalCount;
    }

    private async Task<int> PerformSettingClientMigration()
    {
        _logger.LogInformation("Starting client migration...");
        var settingClients = await _settingClientRepository.GetAllClientsForEncryptionMigration(AuthenticatedUser);

        foreach (var client in settingClients)
        {
            // Saving the client back is enough to encrypt it with the updated secret.
            await _settingClientRepository.UpdateClient(client);
        }
        
        _logger.LogInformation("Client migration complete. Migrated {RecordCount} record(s)", settingClients.Count);
        return settingClients.Count;
    }

    private async Task<int> PerformSettingHistoryMigration(DateTime secretChangeDate)
    {
        _logger.LogInformation("Starting setting history migration...");
        var totalCount = 0;
        int valueCount;
        do
        {
            var values = (await _settingHistoryRepository.GetValuesForEncryptionMigration(secretChangeDate)).ToList();
            if (values.Any())
                await _settingHistoryRepository.UpdateValuesAfterEncryptionMigration(values);

            valueCount = values.Count;
            totalCount += valueCount;
            
            // Don't hammer the database too much.
            Thread.Sleep(100);

        } while (valueCount > 0);
        
        _logger.LogInformation("Setting history migration complete. Migrated {RecordCount} record(s)", totalCount);
        return totalCount;
    }

    private async Task<int> PerformWebHookClientMigration()
    {
        _logger.LogInformation("Starting web hook client migration...");
        var webHookClients = (await _webHookClientRepository.GetClients(true, true)).ToList();

        foreach (var client in webHookClients)
        {
            // Saving the client back is enough to encrypt it with the updated secret.
            await _webHookClientRepository.UpdateClient(client);
        }
        
        _logger.LogInformation("Web Hook Client migration complete. Migrated {RecordCount} record(s)", webHookClients.Count);
        return webHookClients.Count;
    }
    
    private async Task<int> PerformCheckPointMigration(DateTime secretChangeDate)
    {
        _logger.LogInformation("Starting checkPoint migration...");
        var totalCount = 0;
        int checkPointCount;
        do
        {
            var checkPoints = (await _checkPointDataRepository.GetCheckPointsForEncryptionMigration(secretChangeDate)).ToList();
            foreach (var checkPoint in checkPoints)
            {
                MigrateCheckPointData(checkPoint);
            }

            if (checkPoints.Any())
                await _checkPointDataRepository.UpdateCheckPointsAfterEncryptionMigration(checkPoints);

            checkPointCount = checkPoints.Count;
            totalCount += checkPointCount;
            
            // Don't hammer the database too much.
            Thread.Sleep(100);

        } while (checkPointCount > 0);
        
        _logger.LogInformation("CheckPoint migration complete. Migrated {RecordCount} record(s)", totalCount);
        return totalCount;
    }

    private void MigrateCheckPointData(CheckPointDataBusinessEntity checkPoint)
    {
        if (checkPoint.ExportAsJson?.TryParseJson(TypeNameHandling.Objects, out FigDataExportDataContract? export) != true)
        {
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
        _logger.LogInformation("Starting deferred change migration...");
        var deferredChanges = (await _deferredChangeRepository.GetAllChanges()).ToList();
        foreach (var change in deferredChanges)
        {
            // Saving the change back is enough to encrypt it with the updated secret.
            await _deferredChangeRepository.UpdateDeferredChange(change);
        }
        
        _logger.LogInformation("Deferred change migration complete. Migrated {RecordCount} record(s)", deferredChanges.Count);
        return deferredChanges.Count;
    }

    private async Task ValidateActiveApiHosts()
    {
        var activeApis = await _apiStatusRepository.GetAllActive();
        var mismatchedApis = activeApis
            .Where(api => api.ConfigurationErrorDetected)
            .Select(api => api.Hostname)
            .Where(hostname => !string.IsNullOrWhiteSpace(hostname))
            .Distinct()
            .ToList();

        if (mismatchedApis.Any())
        {
            throw new InvalidOperationException(
                $"Cannot run API secret migration while active API hosts report different server secrets: {string.Join(", ", mismatchedApis)}.");
        }
    }
}