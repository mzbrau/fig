// using Fig.Api.ExtensionMethods;
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
        catch (Exception e)
        {
            _logger.LogError(e, "Import failed");
            await _eventLogRepository.Add(_eventLogFactory.DataImportFailed(data?.ImportType ?? ImportType.AddNew, importMode, AuthenticatedUser, e.Message));
            return new ImportResultDataContract
            {
                ErrorMessage = e.Message
            };
        }
    }

    public async Task<FigDataExportDataContract> Export(bool createEventLog = true)
    {
        var clients = await _settingClientRepository.GetAllClients(AuthenticatedUser, false);

        if (createEventLog)
        {
            await _eventLogRepository.Add(_eventLogFactory.DataExported(AuthenticatedUser));
        }
        
        // TODO How to manage versions.
        var export = new FigDataExportDataContract(DateTime.UtcNow,
            ImportType.AddNew,
            1,
            clients.OrderBy(a => a.Name).Select(a => _clientExportConverter.Convert(a))
                .ToList())
        {
            ExportingServer = Environment.MachineName
        };

        return export;
    }    public async Task<FigValueOnlyDataExportDataContract> ValueOnlyExport(bool excludeEnvironmentSpecific = false)
    {
        var clients = await _settingClientRepository.GetAllClients(AuthenticatedUser, false);

        await _eventLogRepository.Add(_eventLogFactory.DataExported(AuthenticatedUser));
        
        var export = new FigValueOnlyDataExportDataContract(DateTime.UtcNow,
            ImportType.UpdateValues,
            1,
            null,
            clients.OrderBy(a => a.Name).Select(a => _clientExportConverter.ConvertValueOnly(a, excludeEnvironmentSpecific))
                .ToList())
        {
            ExportingServer = Environment.MachineName
        };
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

        await _eventLogRepository.Add(_eventLogFactory.DataImportStarted(data.ImportType, importMode, AuthenticatedUser));
        
        var importedClients = new List<string>();
        var deferredClients = new List<string>();
        
        data.Clients.ForEach(c => c.Settings.ForEach(Validate));
        data.ProcessExternallyManagedStatus();

        var errorMessageBuilder = new StringBuilder();
        
        foreach (var clientToUpdate in data.Clients)
        {
            var client = await _settingClientRepository.GetClient(clientToUpdate.Name, clientToUpdate.Instance);

            if (client != null && data.ImportType == ImportType.UpdateValuesInitOnly)
            {
                errorMessageBuilder.AppendLine(
                    $"Init only import requested for client {client.Name} but client already exists. Skipping import");
                _logger.LogWarning("Init only import requested for client {ClientName} but client already exists. Skipping import", client.Name.Sanitize());
            }
            else if (client != null)
            {
                await UpdateClient(client, clientToUpdate, errorMessageBuilder);
                importedClients.Add(client.Name);
            }
            else
            {
                await AddDeferredImport(clientToUpdate);
                deferredClients.Add(clientToUpdate.Name);
            }
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

    private void Validate(SettingValueExportDataContract setting)
    {
        if (!setting.IsEncrypted)
            return;

        try
        {
            _encryptionService.Decrypt(setting.Value?.ToString());
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
        var clients = ConvertAndValidate(data.Clients);
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
        var clients = ConvertAndValidate(data.Clients);
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
        var existingClients = (await _settingClientRepository.GetAllClients(AuthenticatedUser, false)).Select(a => a.GetIdentifier());
        var clientsToAdd = data.Clients.Where(a => !existingClients.Contains(a.GetIdentifier())).ToList();
        var clients = ConvertAndValidate(clientsToAdd);
        await AddClients(clients);

        return new ImportResultDataContract
        {
            ImportType = data.ImportType,
            ImportedClients = clientsToAdd.Select(a => a.Name).ToList()
        };
    }

    private async Task AddDeferredImport(SettingClientValueExportDataContract clientToUpdate)
    {
        var businessEntity = _deferredClientConverter.Convert(clientToUpdate, AuthenticatedUser);
        await _deferredClientImportRepository.AddClient(businessEntity);
    }

    private async Task UpdateClient(SettingClientBusinessEntity client,
        SettingClientValueExportDataContract clientToUpdate, StringBuilder errorMessageBuilder)
    {
        var timeOfUpdate = DateTime.UtcNow;
        var changes = _settingApplier.ApplySettings(client, clientToUpdate.Settings);
        var missingSettings = clientToUpdate.Settings.Where(a => client.Settings.All(b => b.Name != a.Name)).ToList();
        if (missingSettings.Any())
        {
            errorMessageBuilder.AppendLine(
                $"The following import settings did not exist on client {client.Name} and will not be imported: {string.Join(", ", missingSettings.Select(a => a.Name))}");
        }
        
        client.LastSettingValueUpdate = timeOfUpdate;
        await _settingClientRepository.UpdateClient(client);
        await _settingChangeRecorder.RecordSettingChanges(changes, null, timeOfUpdate, client, AuthenticatedUser?.Username);
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
        List<SettingClientExportDataContract> importClients)
    {
        List<SettingClientBusinessEntity> clients = new();
        foreach (var clientToAdd in importClients)
        {
            var client = _clientExportConverter.Convert(clientToAdd);
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
}