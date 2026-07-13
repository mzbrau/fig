// using Fig.Api.ExtensionMethods;
using System.Security.Cryptography;
using System.Text;
using Fig.Api.Converters;
using Fig.Api.DataImport;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Exceptions;
using Fig.Api.ExtensionMethods;
using Fig.Api.Utils;
using Fig.Contracts.ExtensionMethods;
using Fig.Contracts.ImportExport;
using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.BusinessEntities.SettingValues;
using Newtonsoft.Json.Linq;

namespace Fig.Api.Services;

public class ImportExportService : AuthenticatedService, IImportExportService
{
    private readonly ISettingClientRepository _settingClientRepository;
    private readonly IClientExportConverter _clientExportConverter;
    private readonly IEventLogRepository _eventLogRepository;
    private readonly IEventLogFactory _eventLogFactory;
    private readonly ISettingHistoryRepository _settingHistoryRepository;
    private readonly IDeferredClientConverter _deferredClientConverter;
    private readonly IDeferredClientImportRepository _deferredClientImportRepository;
    private readonly ISettingApplier _settingApplier;
    private readonly ISettingChangeRecorder _settingChangeRecorder;
    private readonly IEncryptionService _encryptionService;
    private readonly IClientOverrideService _clientOverrideService;
    private readonly ILogger<ImportExportService> _logger;

    public ImportExportService(ISettingClientRepository settingClientRepository,
        IClientExportConverter clientExportConverter,
        IEventLogRepository eventLogRepository,
        IEventLogFactory eventLogFactory,
        ISettingHistoryRepository settingHistoryRepository,
        IDeferredClientConverter deferredClientConverter,
        IDeferredClientImportRepository deferredClientImportRepository,
        ISettingApplier settingApplier,
        ISettingChangeRecorder settingChangeRecorder,
        IEncryptionService encryptionService,
        IClientOverrideService clientOverrideService,
        ILogger<ImportExportService> logger)
    {
        _settingClientRepository = settingClientRepository;
        _clientExportConverter = clientExportConverter;
        _eventLogRepository = eventLogRepository;
        _eventLogFactory = eventLogFactory;
        _settingHistoryRepository = settingHistoryRepository;
        _deferredClientConverter = deferredClientConverter;
        _deferredClientImportRepository = deferredClientImportRepository;
        _settingApplier = settingApplier;
        _settingChangeRecorder = settingChangeRecorder;
        _encryptionService = encryptionService;
        _clientOverrideService = clientOverrideService;
        _logger = logger;
    }
    
    public async Task<ImportResultDataContract> Import(FigDataExportDataContract? data, ImportMode importMode)
    {
        foreach (var client in data?.Clients.Select(a => a.Name) ?? new List<string>())
            ThrowIfNoAccess(client);
        
        try
        {
            return await PerformImport(data, importMode);
        }
        catch (InvalidPasswordException e)
        {
            var errorMessage = GetFriendlyErrorMessage(e);
            _logger.LogError(e, "Import failed due to decryption error");
            await _eventLogRepository.Add(_eventLogFactory.DataImportFailed(data?.ImportType ?? ImportType.AddNew, importMode, AuthenticatedUser, errorMessage));
            return new ImportResultDataContract
            {
                ErrorMessage = errorMessage,
                RequiresDecryptionKey = string.IsNullOrWhiteSpace(data?.DecryptionKey)
            };
        }
        catch (Exception e)
        {
            var errorMessage = GetFriendlyErrorMessage(e);
            _logger.LogError(e, "Import failed");
            await _eventLogRepository.Add(_eventLogFactory.DataImportFailed(data?.ImportType ?? ImportType.AddNew, importMode, AuthenticatedUser, errorMessage));
            return new ImportResultDataContract
            {
                ErrorMessage = errorMessage
            };
        }
    }

    public async Task<FigDataExportDataContract> Export(bool createEventLog = true, bool includeLastChanged = false)
    {
        var clients = await _settingClientRepository.GetAllClients(AuthenticatedUser);

        if (createEventLog)
        {
            await _eventLogRepository.Add(_eventLogFactory.DataExported(AuthenticatedUser));
        }
        
        // TODO How to manage versions.
        var export = new FigDataExportDataContract(DateTime.UtcNow,
            ImportType.AddNew,
            1,
            clients.Where(c => c.Settings.Any()).OrderBy(a => a.Name).Select(a => _clientExportConverter.Convert(a))
                .ToList())
        {
            ExportingServer = Environment.MachineName
        };

        if (includeLastChanged)
        {
            await PopulateLastChangedDetails(export, clients);
        }

        return export;
    }    
    
    public async Task<FigValueOnlyDataExportDataContract> ValueOnlyExport(bool excludeEnvironmentSpecific = false, bool includeLastChanged = false)
    {
        var clients = await _settingClientRepository.GetAllClients(AuthenticatedUser);

        await _eventLogRepository.Add(_eventLogFactory.DataExported(AuthenticatedUser));
        
        var export = new FigValueOnlyDataExportDataContract(DateTime.UtcNow,
            ImportType.UpdateValues,
            1,
            null,
            clients.Where(c => c.Settings.Any()).OrderBy(a => a.Name).Select(a => _clientExportConverter.ConvertValueOnly(a, excludeEnvironmentSpecific))
                .ToList())
        {
            ExportingServer = Environment.MachineName
        };

        if (includeLastChanged)
        {
            await PopulateLastChangedDetailsValueOnly(export, clients);
        }

        return export;
    }

    public async Task<ImportResultDataContract> ValueOnlyImport(FigValueOnlyDataExportDataContract? data, ImportMode importMode)
    {
        foreach (var client in data?.Clients.Select(a => a.Name) ?? new List<string>())
            ThrowIfNoAccess(client);
        
        if (data?.ImportType != ImportType.UpdateValues && data?.ImportType != ImportType.UpdateValuesInitOnly)
            throw new NotSupportedException(
                $"Value only imports only support {nameof(ImportType.UpdateValues)} import type");
        
        if (!data.Clients.Any())
            return new ImportResultDataContract { ImportType = data.ImportType, ErrorMessage = "No clients to import"};

        try
        {
            data.Clients.ForEach(c => c.Settings.ForEach(s => Validate(s, data.DecryptionKey)));
        }
        catch (InvalidImportException e)
        {
            _logger.LogError(e, "Value only import failed due to decryption error");
            await _eventLogRepository.Add(_eventLogFactory.DataImportFailed(data.ImportType, importMode, AuthenticatedUser, e.Message));
            return new ImportResultDataContract
            {
                ImportType = data.ImportType,
                ErrorMessage = e.Message,
                RequiresDecryptionKey = true
            };
        }

        await _eventLogRepository.Add(_eventLogFactory.DataImportStarted(data.ImportType, importMode, AuthenticatedUser));
        
        var importedClients = new List<string>();
        var deferredClients = new List<string>();
        
        data.ProcessExternallyManagedStatus();

        var errorMessageBuilder = new StringBuilder();
        
        try
        {
            foreach (var clientToUpdate in data.Clients)
            {
                var clientIdentifier = GetClientIdentifier(clientToUpdate.Name, clientToUpdate.Instance);
                var client = await _settingClientRepository.GetClient(clientToUpdate.Name, clientToUpdate.Instance);

                if (client != null && data.ImportType == ImportType.UpdateValuesInitOnly)
                {
                    errorMessageBuilder.AppendLine(
                        $"Init only import requested for client {clientIdentifier} but client already exists. Skipping import");
                    _logger.LogWarning("Init only import requested for client {ClientName} but client already exists. Skipping import", client.Name.Sanitize());
                }
                else if (client != null)
                {
                    await UpdateClient(client, clientToUpdate, errorMessageBuilder, data.DecryptionKey);
                    importedClients.Add(clientIdentifier);
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(clientToUpdate.Instance))
                    {
                        var baseClient = await _settingClientRepository.GetClient(clientToUpdate.Name, null);
                        if (baseClient != null)
                        {
                            var instanceClient = await _clientOverrideService.CreateClientOverride(
                                clientToUpdate.Name,
                                clientToUpdate.Instance,
                                AuthenticatedUser);

                            await UpdateClient(instanceClient, clientToUpdate, errorMessageBuilder, data.DecryptionKey);
                            importedClients.Add(clientIdentifier);
                            continue;
                        }
                    }

                    await AddDeferredImport(clientToUpdate, data.DecryptionKey);
                    deferredClients.Add(clientIdentifier);
                }
            }
        }
        catch (InvalidImportException e)
        {
            _logger.LogError(e, "Value only import failed during client update due to decryption error");
            await _eventLogRepository.Add(_eventLogFactory.DataImportFailed(data.ImportType, importMode, AuthenticatedUser, e.Message));
            return new ImportResultDataContract
            {
                ImportType = data.ImportType,
                ErrorMessage = e.Message,
                RequiresDecryptionKey = string.IsNullOrWhiteSpace(data.DecryptionKey)
            };
        }
        catch (CryptographicException e)
        {
            const string message = "Unable to decrypt existing client data. The server encryption key may have changed since this client was registered.";
            _logger.LogError(e, "Value only import failed during client retrieval due to decryption error");
            await _eventLogRepository.Add(_eventLogFactory.DataImportFailed(data.ImportType, importMode, AuthenticatedUser, message));
            return new ImportResultDataContract
            {
                ImportType = data.ImportType,
                ErrorMessage = message,
                RequiresDecryptionKey = true
            };
        }
        
        if (importedClients.Any())
            await _eventLogRepository.Add(_eventLogFactory.DataImported(data.ImportType, importMode, importedClients.Count, AuthenticatedUser));
        
        if (deferredClients.Any())
            await _eventLogRepository.Add(_eventLogFactory.DeferredImportRegistered(data.ImportType, importMode, deferredClients.Count, AuthenticatedUser));
        
        return new ImportResultDataContract
        {
            ImportType = data.ImportType,
            ImportedClients = importedClients,
            DeferredImportClients = deferredClients,
            ErrorMessage = errorMessageBuilder.Length > 0 ? errorMessageBuilder.ToString() : null
        };
    }

    private static string GetClientIdentifier(string name, string? instance)
    {
        if (string.IsNullOrWhiteSpace(instance))
            return name;

        return $"{name} (instance: {instance})";
    }

    private void Validate(SettingValueExportDataContract setting, string? customDecryptionKey = null)
    {
        if (!setting.IsEncrypted)
            return;

        // DataGrid values are complex types (JArray) — column-level decryption
        // is validated during import when column definitions are available.
        if (setting.Value is JArray)
            return;

        try
        {
            _encryptionService.DecryptForImport(setting.Value?.ToString(), customDecryptionKey);
        }
        catch (Exception)
        {
            throw new InvalidImportException($"Unable to decrypt setting {setting.Name}. " +
                                             $"It might have been encrypted with a different encryption key.");
        }
    }

    public async Task<List<DeferredImportClientDataContract>> GetDeferredImportClients()
    {
        var clients = await _deferredClientImportRepository.GetAllClients(AuthenticatedUser);
        return clients.Select(a => new DeferredImportClientDataContract(a.Name, a.Instance, a.SettingCount, a.AuthenticatedUser)).ToList();
    }

    public async Task DeleteAllDeferredImports()
    {
        await _deferredClientImportRepository.DeleteAll();
    }

    private async Task<ImportResultDataContract> PerformImport(FigDataExportDataContract? data, ImportMode importMode)
    {
        if (data?.Clients.Any() != true)
            return new ImportResultDataContract() { ImportType = data?.ImportType ?? ImportType.AddNew, ErrorMessage = "No Clients to Import" };

        await _eventLogRepository.Add(_eventLogFactory.DataImportStarted(data.ImportType, importMode, AuthenticatedUser));

        ImportResultDataContract result;
        switch (data.ImportType)
        {
            case ImportType.ClearAndImport:
                result = await ClearAndImport(data);
                break;
            case ImportType.ReplaceExisting:
            {
                result = await ReplaceExisting(data);
                break;
            }
            case ImportType.AddNew:
            {
                result = await AddNew(data);
                break;
            }
            default:
                throw new NotSupportedException($"Import type {data.ImportType} not supported for full imports");
        }

        if (result.ImportedClients.Count > 0)
            await _eventLogRepository.Add(_eventLogFactory.DataImported(data.ImportType, importMode, result.ImportedClients.Count, AuthenticatedUser));

        return result;
    }

    private async Task<ImportResultDataContract> ClearAndImport(FigDataExportDataContract data)
    {
        var clients = ConvertAndValidate(data.Clients, data.DecryptionKey);
        var deletedClients = await DeleteClients(_ => true);
        await AddClients(clients);

        return new ImportResultDataContract
        {
            ImportType = data.ImportType,
            ImportedClients = data.Clients.Select(a => a.Name).ToList(),
            DeletedClients = deletedClients,
        };
    }

    private async Task<ImportResultDataContract> ReplaceExisting(FigDataExportDataContract data)
    {
        var clients = ConvertAndValidate(data.Clients, data.DecryptionKey);
        var importedClients = data.Clients.Select(a => a.GetIdentifier());
        var deletedClients = await DeleteClients(a => importedClients.Contains(a.GetIdentifier()));
        await AddClients(clients);

        return new ImportResultDataContract
        {
            ImportType = data.ImportType,
            ImportedClients = data.Clients.Select(a => a.Name).ToList(),
            DeletedClients = deletedClients,
        };
    }

    private async Task<ImportResultDataContract> AddNew(FigDataExportDataContract data)
    {
        var existingClients = (await _settingClientRepository.GetAllClients(AuthenticatedUser)).Select(a => a.GetIdentifier());
        var clientsToAdd = data.Clients.Where(a => !existingClients.Contains(a.GetIdentifier())).ToList();
        var clients = ConvertAndValidate(clientsToAdd, data.DecryptionKey);
        await AddClients(clients);

        return new ImportResultDataContract
        {
            ImportType = data.ImportType,
            ImportedClients = clientsToAdd.Select(a => a.Name).ToList()
        };
    }

    private async Task AddDeferredImport(SettingClientValueExportDataContract clientToUpdate, string? customDecryptionKey = null)
    {
        if (!string.IsNullOrWhiteSpace(customDecryptionKey))
        {
            ReEncryptSettingsForDeferredImport(clientToUpdate, customDecryptionKey);
        }
        
        var businessEntity = _deferredClientConverter.Convert(clientToUpdate, AuthenticatedUser);
        await _deferredClientImportRepository.AddClient(businessEntity);
    }

    private void ReEncryptSettingsForDeferredImport(SettingClientValueExportDataContract client, string customDecryptionKey)
    {
        foreach (var setting in client.Settings.Where(s => s.IsEncrypted))
        {
            if (setting.Value is JArray jArray)
            {
                ReEncryptDataGridForDeferredImport(jArray, customDecryptionKey, setting.Name);
                continue;
            }
            
            var encryptedText = System.Convert.ToString(setting.Value, System.Globalization.CultureInfo.InvariantCulture);
            if (encryptedText is null)
                continue;

            try
            {
                var decrypted = _encryptionService.DecryptWithCustomKey(encryptedText, customDecryptionKey);
                setting.Value = _encryptionService.Encrypt(decrypted);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to re-encrypt setting '{SettingName}' for deferred import, storing as-is", setting.Name);
            }
        }
    }

    private void ReEncryptDataGridForDeferredImport(JArray jArray, string customDecryptionKey, string settingName)
    {
        foreach (var row in jArray.OfType<JObject>())
        {
            foreach (var property in row.Properties().ToList())
            {
                if (property.Value.Type != JTokenType.String)
                    continue;

                var cellValue = property.Value.ToString();
                try
                {
                    var decrypted = _encryptionService.DecryptWithCustomKey(cellValue, customDecryptionKey);
                    property.Value = _encryptionService.Encrypt(decrypted);
                }
                catch (Exception)
                {
                    // Not an encrypted value, leave as-is
                }
            }
        }
    }

    private async Task UpdateClient(SettingClientBusinessEntity client,
        SettingClientValueExportDataContract clientToUpdate, StringBuilder errorMessageBuilder, string? customDecryptionKey = null)
    {
        var timeOfUpdate = DateTime.UtcNow;
        var result = _settingApplier.ApplySettings(client, clientToUpdate.Settings, customDecryptionKey);
        foreach (var warning in result.Warnings)
        {
            errorMessageBuilder.AppendLine(warning);
        }

        var missingSettings = clientToUpdate.Settings
            .Where(a => !result.HandledImportSettingNames.Contains(a.Name) && client.Settings.All(b => b.Name != a.Name))
            .ToList();
        if (missingSettings.Any())
        {
            errorMessageBuilder.AppendLine(
                $"The following import settings did not exist on client {client.Name} and will not be imported: {string.Join(", ", missingSettings.Select(a => a.Name))}");
        }
        
        client.LastSettingValueUpdate = timeOfUpdate;
        await _settingClientRepository.UpdateClient(client);
        await _settingChangeRecorder.RecordSettingChanges(result.Changes, null, timeOfUpdate, client, AuthenticatedUser?.Username);
    }

    private async Task RecordInitialSettingValues(SettingClientBusinessEntity client)
    {
        foreach (var setting in client.Settings)
        {
            var value = setting.Value is DataGridSettingBusinessEntity dataGridVal
                ? ChangedSetting.GetDataGridValue(dataGridVal, setting.GetDataGridDefinition())
                : setting.Value;
            await _settingHistoryRepository.Add(new SettingValueBusinessEntity
            {
                ClientId = client.Id,
                ChangedAt = DateTime.UtcNow,
                SettingName = setting.Name,
                Value = value,
                ChangedBy = "REGISTRATION"
            });
        }
    }

    private List<SettingClientBusinessEntity> ConvertAndValidate(
        List<SettingClientExportDataContract> importClients, string? customDecryptionKey = null)
    {
        List<SettingClientBusinessEntity> clients = new();
        foreach (var clientToAdd in importClients)
        {
            var client = _clientExportConverter.Convert(clientToAdd, customDecryptionKey);
            client.Settings.ToList().ForEach(a => a.Validate());
            clients.Add(client);
        }

        return clients;
    }
    
    private async Task AddClients(List<SettingClientBusinessEntity> clients)
    {
        foreach (var client in clients)
        {
            client.LastRegistration = DateTime.UtcNow;

            await _settingClientRepository.RegisterClient(client);
            await RecordInitialSettingValues(client);
            await _eventLogRepository.Add(_eventLogFactory.Imported(client, AuthenticatedUser));
        }
    }

    private async Task<List<string>> DeleteClients(Func<SettingClientBusinessEntity, bool> selector)
    {
        var clients = await _settingClientRepository.GetAllClients(AuthenticatedUser, true);

        var names = new List<string>();
        foreach (var client in clients.Where(selector))
        {
            await _settingClientRepository.DeleteClient(client);
            await _eventLogRepository.Add(_eventLogFactory.ClientDeleted(client.Id, client.Name, client.Instance, AuthenticatedUser));
            names.Add(client.Name);
        }

        return names;
    }

    private static string GetFriendlyErrorMessage(Exception exception)
    {
        return exception switch
        {
            NullReferenceException when exception.StackTrace?.Contains("NHibernate") == true && 
                                       exception.StackTrace.Contains("lookup_tables") => 
                "Import failed due to a lookup table data issue. Please check that all lookup tables referenced in the import data exist and are properly configured.",
            
            NullReferenceException => 
                "Import failed due to missing required data. Please check that all referenced settings and clients are properly defined.",
            
            InvalidImportException => exception.Message,
            
            _ => exception.Message
        };
    }

    private async Task PopulateLastChangedDetailsValueOnly(FigValueOnlyDataExportDataContract export,
        IList<SettingClientBusinessEntity> clients)
    {
        await PopulateLastChangedDetailsCore(
            export.Clients,
            clients,
            exportClient => exportClient.Name,
            exportClient => exportClient.Instance,
            exportClient => exportClient.Settings,
            setting => setting.Name,
            (setting, details) => setting.LastChangedDetails = details);
    }

    private async Task PopulateLastChangedDetails(FigDataExportDataContract export,
        IList<SettingClientBusinessEntity> clients)
    {
        await PopulateLastChangedDetailsCore(
            export.Clients,
            clients,
            exportClient => exportClient.Name,
            exportClient => exportClient.Instance,
            exportClient => exportClient.Settings,
            setting => setting.Name,
            (setting, details) => setting.LastChangedDetails = details);
    }

    private async Task PopulateLastChangedDetailsCore<TClient, TSetting>(
        IEnumerable<TClient> exportClients,
        IList<SettingClientBusinessEntity> clients,
        Func<TClient, string> getClientName,
        Func<TClient, string?> getClientInstance,
        Func<TClient, IEnumerable<TSetting>> getSettings,
        Func<TSetting, string> getSettingName,
        Action<TSetting, SettingLastChangedDataContract> setLastChangedDetails)
    {
        var lastChangedEntries = await _settingHistoryRepository.GetLastChangedForAllClients();
        var lastChangedLookupByClient = lastChangedEntries
            .GroupBy(e => e.ClientId)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(e => e.SettingName)
                    .ToDictionary(gg => gg.Key, gg => gg.First()));

        foreach (var exportClient in exportClients)
        {
            var clientName = getClientName(exportClient);
            var clientInstance = getClientInstance(exportClient);
            var client = clients.FirstOrDefault(c =>
                c.Name == clientName && c.Instance == clientInstance);

            if (client == null)
                continue;

            if (!lastChangedLookupByClient.TryGetValue(client.Id, out var lastChangedLookup))
                continue;

            foreach (var setting in getSettings(exportClient).Where(s => lastChangedLookup.ContainsKey(getSettingName(s))))
            {
                var historyEntry = lastChangedLookup[getSettingName(setting)];
                setLastChangedDetails(
                    setting,
                    new SettingLastChangedDataContract(
                        historyEntry.ChangedBy,
                        historyEntry.ChangedAt,
                        historyEntry.ChangeMessage));
            }
        }
    }
}
