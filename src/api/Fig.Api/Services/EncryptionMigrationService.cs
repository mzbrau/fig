using System.Diagnostics;
using Fig.Api.Datalayer.Repositories;
using Fig.Common.ExtensionMethods;
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
    private readonly IEncryptionService _encryptionService;
    private readonly IOptionsMonitor<ApiSettings> _settings;
    private readonly ILogger<EncryptionMigrationService> _logger;

    public EncryptionMigrationService(IEventLogRepository eventLogRepository,
        ISettingClientRepository settingClientRepository,
        ISettingHistoryRepository settingHistoryRepository,
        IWebHookClientRepository webHookClientRepository,
        ICheckPointDataRepository checkPointDataRepository,
        IEncryptionService encryptionService,
        IOptionsMonitor<ApiSettings> settings,
        ILogger<EncryptionMigrationService> logger)
    {
        _eventLogRepository = eventLogRepository;
        _settingClientRepository = settingClientRepository;
        _settingHistoryRepository = settingHistoryRepository;
        _webHookClientRepository = webHookClientRepository;
        _checkPointDataRepository = checkPointDataRepository;
        _encryptionService = encryptionService;
        _settings = settings;
        _logger = logger;
    }
    
    public void PerformMigration()
    {
        var secretChangeDate = DateTime.UtcNow;
        if (string.IsNullOrWhiteSpace(_settings.CurrentValue.GetDecryptedPreviousSecret()))
        {
            // TODO: This was here to fix a bug in 0.9.0. It should be removed in a future version.
            PerformEventLogMigration(secretChangeDate);
            throw new ApplicationException("Logs have been migrated but unable to migrate other parts without a previous secret.");
        }

        var watch = Stopwatch.StartNew();
        _logger.LogInformation("Starting encryption migration...");
        
        PerformSettingClientMigration();
        PerformWebHookClientMigration();
        PerformEventLogMigration(secretChangeDate);
        PerformSettingHistoryMigration(secretChangeDate);
        PerformCheckPointMigration(secretChangeDate);
        
        _logger.LogInformation("Encryption migration complete in {ElapsedMs}ms", watch.ElapsedMilliseconds);
    }

    private void PerformEventLogMigration(DateTime secretChangeDate)
    {
        _logger.LogInformation("Starting event log migration...");
        int eventLogCount;
        do
        {
            var eventLogs = _eventLogRepository.GetLogsForEncryptionMigration(secretChangeDate).ToList();
            if (eventLogs.Any())
                _eventLogRepository.UpdateLogsAfterEncryptionMigration(eventLogs);

            eventLogCount = eventLogs.Count;
            
            // Don't hammer the database too much.
            Thread.Sleep(100);

        } while (eventLogCount > 0);
        
        _logger.LogInformation("Event log migration complete");
    }

    private void PerformSettingClientMigration()
    {
        _logger.LogInformation("Starting client migration...");
        var settingClients = _settingClientRepository.GetAllClients(AuthenticatedUser, true);

        foreach (var client in settingClients)
        {
            // Saving the client back is enough to encrypt it with the updated secret.
            _settingClientRepository.UpdateClient(client);
        }
        
        _logger.LogInformation("Client migration complete");
    }

    private void PerformSettingHistoryMigration(DateTime secretChangeDate)
    {
        _logger.LogInformation("Starting setting history migration...");
        int valueCount;
        do
        {
            var values = _settingHistoryRepository.GetValuesForEncryptionMigration(secretChangeDate).ToList();
            if (values.Any())
                _settingHistoryRepository.UpdateValuesAfterEncryptionMigration(values);

            valueCount = values.Count;
            
            // Don't hammer the database too much.
            Thread.Sleep(100);

        } while (valueCount > 0);
        
        _logger.LogInformation("Setting history migration complete");
    }

    private void PerformWebHookClientMigration()
    {
        _logger.LogInformation("Starting web hook client migration...");
        var webHookClients = _webHookClientRepository.GetClients(true);

        foreach (var client in webHookClients)
        {
            // Saving the client back is enough to encrypt it with the updated secret.
            _webHookClientRepository.UpdateClient(client);
        }
        
        _logger.LogInformation("Web Hook Client migration complete");
    }
    
    private void PerformCheckPointMigration(DateTime secretChangeDate)
    {
        _logger.LogInformation("Starting checkPoint migration...");
        int checkPointCount;
        do
        {
            var checkPoints = _checkPointDataRepository.GetCheckPointsForEncryptionMigration(secretChangeDate).ToList();
            foreach (var checkPoint in checkPoints)
            {
                MigrateCheckPointData(checkPoint);
            }

            if (checkPoints.Any())
                _checkPointDataRepository.UpdateCheckPointsAfterEncryptionMigration(checkPoints);

            checkPointCount = checkPoints.Count;
            
            // Don't hammer the database too much.
            Thread.Sleep(100);

        } while (checkPointCount > 0);
        
        _logger.LogInformation("CheckPoint migration complete");
    }

    private void MigrateCheckPointData(CheckPointDataBusinessEntity checkPoint)
    {
        if (checkPoint.ExportAsJson?.TryParseJson(TypeNameHandling.Objects, out FigDataExportDataContract? export) != true)
        {
            return;
        }

        foreach (var secretSetting in export?.Clients.SelectMany(a => a.Settings).Where(s => s.IsSecret) ?? [])
        {
            var decrypted = _encryptionService.Decrypt(secretSetting.Value?.GetValue()?.ToString(), true, false);
            if (decrypted is not null)
            {
                if (secretSetting.Value is StringSettingDataContract stringSetting)
                {
                    stringSetting.Value = _encryptionService.Encrypt(decrypted);
                }
            }
        }
    }
}