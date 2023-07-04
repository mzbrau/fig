using Fig.Api.Converters;
using Fig.Api.DataImport;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.ExtensionMethods;
using Fig.Api.SettingVerification;
using Fig.Api.Utils;
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
    private readonly ISettingVerifier _settingVerifier;
    private readonly ISettingHistoryRepository _settingHistoryRepository;
    private readonly IDeferredClientConverter _deferredClientConverter;
    private readonly IDeferredClientImportRepository _deferredClientImportRepository;
    private readonly IDeferredSettingApplier _deferredSettingApplier;
    private readonly ISettingChangeRecorder _settingChangeRecorder;

    public ImportExportService(ISettingClientRepository settingClientRepository,
        IClientExportConverter clientExportConverter,
        IEventLogRepository eventLogRepository,
        IEventLogFactory eventLogFactory,
        ISettingVerifier settingVerifier,
        ISettingHistoryRepository settingHistoryRepository,
        IDeferredClientConverter deferredClientConverter,
        IDeferredClientImportRepository deferredClientImportRepository,
        IDeferredSettingApplier deferredSettingApplier,
        ISettingChangeRecorder settingChangeRecorder)
    {
        _settingClientRepository = settingClientRepository;
        _clientExportConverter = clientExportConverter;
        _eventLogRepository = eventLogRepository;
        _eventLogFactory = eventLogFactory;
        _settingVerifier = settingVerifier;
        _settingHistoryRepository = settingHistoryRepository;
        _deferredClientConverter = deferredClientConverter;
        _deferredClientImportRepository = deferredClientImportRepository;
        _deferredSettingApplier = deferredSettingApplier;
        _settingChangeRecorder = settingChangeRecorder;
    }
    
    public async Task<ImportResultDataContract> Import(FigDataExportDataContract data, ImportMode importMode)
    {
        if (!data.Clients.Any())
            return new ImportResultDataContract() { ImportType = data.ImportType };

        _eventLogRepository.Add(_eventLogFactory.DataImportStarted(data.ImportType, importMode, AuthenticatedUser));

        int clientImportCount;
        var clientDeletedCount = 0;
        var addedClients = new List<string>();
        switch (data.ImportType)
        {
            case ImportType.ClearAndImport:
                clientDeletedCount = DeleteClients(_ => true);
                await AddClients(data.Clients);
                clientImportCount = data.Clients.Count;
                addedClients.AddRange(data.Clients.Select(a => a.Name));
                break;
            case ImportType.ReplaceExisting:
            {
                var importedClients = data.Clients.Select(a => a.GetIdentifier());
                clientDeletedCount = DeleteClients(a => importedClients.Contains(a.GetIdentifier()));
                await AddClients(data.Clients);
                clientImportCount = data.Clients.Count;
                addedClients.AddRange(data.Clients.Select(a => a.Name));
                break;
            }
            case ImportType.AddNew:
            {
                var existingClients = _settingClientRepository.GetAllClients().Select(a => a.GetIdentifier());
                var clientsToAdd = data.Clients.Where(a => !existingClients.Contains(a.GetIdentifier())).ToList();
                await AddClients(clientsToAdd);
                clientImportCount = clientsToAdd.Count;
                addedClients.AddRange(clientsToAdd.Select(a => a.Name));
                break;
            }
            default:
                throw new NotSupportedException($"Import type {data.ImportType} not supported for full imports");
        }

        if (clientImportCount > 0)
            _eventLogRepository.Add(_eventLogFactory.DataImported(data.ImportType, importMode, clientImportCount, AuthenticatedUser));

        return new ImportResultDataContract
        {
            ImportType = data.ImportType,
            ImportedClientCount = clientImportCount,
            DeletedClientCount = clientDeletedCount,
            ImportedClients = addedClients
        };
    }

    public FigDataExportDataContract Export(bool decryptSecrets)
    {
        var clients = _settingClientRepository.GetAllClients();

        _eventLogRepository.Add(_eventLogFactory.DataExported(AuthenticatedUser, decryptSecrets));
        
        // TODO How to manage versions.
        return new FigDataExportDataContract(DateTime.UtcNow,
            ImportType.AddNew,
            1,
            clients.Select(a => _clientExportConverter.Convert(a,
                    decryptSecrets))
                .ToList());
    }

    public FigValueOnlyDataExportDataContract ValueOnlyExport()
    {
        var clients = _settingClientRepository.GetAllClients();

        _eventLogRepository.Add(_eventLogFactory.DataExported(AuthenticatedUser, false));
        
        return new FigValueOnlyDataExportDataContract(DateTime.UtcNow,
            ImportType.UpdateValues,
            1,
            clients.Select(a => _clientExportConverter.ConvertValueOnly(a))
                .ToList());
    }

    public ImportResultDataContract ValueOnlyImport(FigValueOnlyDataExportDataContract data, ImportMode importMode)
    {
        if (data.ImportType != ImportType.UpdateValues)
            throw new NotSupportedException(
                $"Value only imports only support {nameof(ImportType.UpdateValues)} import type");
        
        if (!data.Clients.Any())
            return new ImportResultDataContract() { ImportType = data.ImportType };

        _eventLogRepository.Add(_eventLogFactory.DataImportStarted(data.ImportType, importMode, AuthenticatedUser));
        
        var importedClients = new List<string>();
        var deferredClients = new List<string>();

        foreach (var clientToUpdate in data.Clients)
        {
            var client = _settingClientRepository.GetClient(clientToUpdate.Name, clientToUpdate.Instance);

            if (client != null)
            {
                UpdateClient(client, clientToUpdate);
                importedClients.Add(client.Name);
            }
            else
            {
                AddDeferredImport(clientToUpdate);
                deferredClients.Add(clientToUpdate.Name);
            }
        }
        
        if (importedClients.Any())
            _eventLogRepository.Add(_eventLogFactory.DataImported(data.ImportType, importMode, importedClients.Count, AuthenticatedUser));
        
        if (deferredClients.Any())
            _eventLogRepository.Add(_eventLogFactory.DeferredImportRegistered(data.ImportType, importMode, deferredClients.Count, AuthenticatedUser));
        
        return new ImportResultDataContract
        {
            ImportType = data.ImportType,
            ImportedClientCount = importedClients.Count,
            DeferredImportClientCount = deferredClients.Count,
            ImportedClients = importedClients,
            DeferredImportClients = deferredClients
        };
    }

    public List<DeferredImportClientDataContract> GetDeferredImportClients()
    {
        var clients = _deferredClientImportRepository.GetAllClients();
        return clients.Select(a => new DeferredImportClientDataContract(a.Name, a.Instance, a.SettingCount, a.AuthenticatedUser)).ToList();
    }

    private void AddDeferredImport(SettingClientValueExportDataContract clientToUpdate)
    {
        var businessEntity = _deferredClientConverter.Convert(clientToUpdate, AuthenticatedUser);
        _deferredClientImportRepository.SaveClient(businessEntity);
    }

    private void UpdateClient(SettingClientBusinessEntity client, SettingClientValueExportDataContract clientToUpdate)
    {
        var timeOfUpdate = DateTime.UtcNow;
        var changes = _deferredSettingApplier.ApplySettings(client, clientToUpdate.Settings);
        client.LastSettingValueUpdate = timeOfUpdate;
        _settingClientRepository.UpdateClient(client);
        _settingChangeRecorder.RecordSettingChanges(changes, null, timeOfUpdate, client, AuthenticatedUser?.Username);
    }

    private void RecordInitialSettingValues(SettingClientBusinessEntity client)
    {
        foreach (var setting in client.Settings)
        {
            var value = setting.Value is DataGridSettingBusinessEntity
                ? ChangedSetting.GetDataGridValue(setting.Value)
                : setting.Value;
            _settingHistoryRepository.Add(new SettingValueBusinessEntity
            {
                ClientId = client.Id,
                ChangedAt = DateTime.UtcNow,
                SettingName = setting.Name,
                Value = value,
                ChangedBy = "REGISTRATION"
            });
        }
    }
    
    private async Task AddClients(List<SettingClientExportDataContract> clients)
    {
        foreach (var clientToAdd in clients)
        {
            var client = _clientExportConverter.Convert(clientToAdd);

            foreach (var verification in client.DynamicVerifications)
                await _settingVerifier.Compile(verification);

            client.Settings.ToList().ForEach(a => a.Validate());
            client.LastRegistration = DateTime.UtcNow;

            _settingClientRepository.RegisterClient(client);
            RecordInitialSettingValues(client);
            _eventLogRepository.Add(_eventLogFactory.Imported(client, AuthenticatedUser));
        }
    }

    private int DeleteClients(Func<SettingClientBusinessEntity, bool> selector)
    {
        var clients = _settingClientRepository.GetAllClients();

        var count = 0;
        foreach (var client in clients.Where(selector))
        {
            _settingClientRepository.DeleteClient(client);
            _eventLogRepository.Add(_eventLogFactory.ClientDeleted(client.Id, client.Name, client.Instance, AuthenticatedUser));
            count++;
        }

        return count;
    }
}