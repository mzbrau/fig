using System.Diagnostics;
using Fig.Api.Datalayer.Repositories;
using Microsoft.Extensions.Options;

namespace Fig.Api.Services;

public class EncryptionMigrationService : AuthenticatedService, IEncryptionMigrationService
{
    private readonly IEventLogRepository _eventLogRepository;
    private readonly ISettingClientRepository _settingClientRepository;
    private readonly ISettingHistoryRepository _settingHistoryRepository;
    private readonly IWebHookClientRepository _webHookClientRepository;
    private readonly IOptions<ApiSettings> _settings;
    private readonly ILogger<EncryptionMigrationService> _logger;

    public EncryptionMigrationService(IEventLogRepository eventLogRepository,
        ISettingClientRepository settingClientRepository,
        ISettingHistoryRepository settingHistoryRepository,
        IWebHookClientRepository webHookClientRepository,
        IOptions<ApiSettings> settings,
        ILogger<EncryptionMigrationService> logger)
    {
        _eventLogRepository = eventLogRepository;
        _settingClientRepository = settingClientRepository;
        _settingHistoryRepository = settingHistoryRepository;
        _webHookClientRepository = webHookClientRepository;
        _settings = settings;
        _logger = logger;
    }
    
    public void PerformMigration()
    {
        if (string.IsNullOrWhiteSpace(_settings.Value.GetDecryptedPreviousSecret()))
            throw new ApplicationException("Unable to migrate without a previous secret.");
        
        var watch = Stopwatch.StartNew();
        _logger.LogInformation("Starting encryption migration...");
        var secretChangeDate = DateTime.UtcNow;
        PerformSettingClientMigration();
        PerformWebHookClientMigration();
        PerformEventLogMigration(secretChangeDate);
        PerformSettingHistoryMigration(secretChangeDate);
        
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
}