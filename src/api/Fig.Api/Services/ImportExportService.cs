using Fig.Api.Converters;
using Fig.Api.DataImport;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.ExtensionMethods;
using Fig.Api.SettingVerification;
using Fig.Api.Utils;
using Fig.Contracts;
using Fig.Contracts.ExtensionMethods;
using Fig.Contracts.ImportExport;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Services;

public class ImportExportService : AuthenticatedService, IImportExportService
{
    private readonly ISettingClientRepository _settingClientRepository;
    private readonly IClientExportConverter _clientExportConverter;
    private readonly IEventLogRepository _eventLogRepository;
    private readonly IEventLogFactory _eventLogFactory;
    private readonly ISettingVerifier _settingVerifier;
    private readonly ISettingHistoryRepository _settingHistoryRepository;

    public ImportExportService(ISettingClientRepository settingClientRepository,
        IClientExportConverter clientExportConverter,
        IEventLogRepository eventLogRepository,
        IEventLogFactory eventLogFactory,
        ISettingVerifier settingVerifier,
        ISettingHistoryRepository settingHistoryRepository)
    {
        _settingClientRepository = settingClientRepository;
        _clientExportConverter = clientExportConverter;
        _eventLogRepository = eventLogRepository;
        _eventLogFactory = eventLogFactory;
        _settingVerifier = settingVerifier;
        _settingHistoryRepository = settingHistoryRepository;
    }
    
    public async Task Import(FigDataExportDataContract data, ImportMode importMode)
    {
        if (!data.Clients.Any())
            return;

        _eventLogRepository.Add(_eventLogFactory.DataImportStarted(data.ImportType, importMode, AuthenticatedUser));

        var clientImportCount = 0;
        if (data.ImportType == ImportType.ClearAndImport)
        {
            DeleteClients(_ => true);
            await AddClients(data.Clients);
            clientImportCount = data.Clients.Count;
        }
        else if (data.ImportType == ImportType.ReplaceExisting)
        {
            var importedClients = data.Clients.Select(a => a.GetIdentifier());
            DeleteClients(a => importedClients.Contains(a.GetIdentifier()));
            await AddClients(data.Clients);
            clientImportCount = data.Clients.Count;
        }
        else if (data.ImportType == ImportType.AddNew)
        {
            var existingClients = _settingClientRepository.GetAllClients().Select(a => a.GetIdentifier());
            var clientsToAdd = data.Clients.Where(a => !existingClients.Contains(a.GetIdentifier())).ToList();
            await AddClients(clientsToAdd);
            clientImportCount = clientsToAdd.Count;
        }

        if (clientImportCount > 0)
            _eventLogRepository.Add(_eventLogFactory.DataImported(data.ImportType, importMode, clientImportCount, AuthenticatedUser));
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

    private void DeleteClients(Func<SettingClientBusinessEntity, bool> selector)
    {
        var clients = _settingClientRepository.GetAllClients();

        foreach (var client in clients.Where(selector))
        {
            _settingClientRepository.DeleteClient(client);
            _eventLogRepository.Add(_eventLogFactory.ClientDeleted(client.Id, client.Name, client.Instance, AuthenticatedUser));
        }
    }

    public FigDataExportDataContract Export()
    {
        var clients = _settingClientRepository.GetAllClients();

        _eventLogRepository.Add(_eventLogFactory.DataExported(AuthenticatedUser));
        
        return new FigDataExportDataContract
        {
            ExportedAt = DateTime.UtcNow,
            ImportType = ImportType.AddNew,
            Version = 1, // TODO How to manage versions.
            Clients = clients.Select(a => _clientExportConverter.Convert(a)).ToList()
        };
    }

    private void RecordInitialSettingValues(SettingClientBusinessEntity client)
    {
        foreach (var setting in client.Settings)
        {
            var value = setting.ValueType.Is(FigPropertyType.DataGrid)
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
}