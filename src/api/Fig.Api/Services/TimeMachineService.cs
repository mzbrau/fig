using System.Diagnostics;
using Fig.Api.Converters;
using Fig.Api.DataImport;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Observability;
using Fig.Api.Utils;
using Fig.Common.ExtensionMethods;
using Fig.Common.NetStandard.Json;
using Fig.Contracts.CheckPoint;
using Fig.Contracts.ImportExport;
using Fig.Datalayer.BusinessEntities;
using Newtonsoft.Json;

namespace Fig.Api.Services;

public class TimeMachineService : AuthenticatedService, ITimeMachineService
{
    private readonly IImportExportService _importExportService;
    private readonly ICheckPointRepository _checkPointRepository;
    private readonly ICheckPointDataRepository _checkPointDataRepository;
    private readonly ICheckPointConverter _checkPointConverter;
    private readonly IEventLogRepository _eventLogRepository;
    private readonly IEventLogFactory _eventLogFactory;
    private readonly IConfigurationRepository _configurationRepository;
    private readonly ILogger<TimeMachineService> _logger;
    private readonly object _lockObject = new();
    private DateTime? _earliestEvent;

    public TimeMachineService(
        IImportExportService importExportService,
        ICheckPointRepository checkPointRepository,
        ICheckPointDataRepository checkPointDataRepository,
        ICheckPointConverter checkPointConverter,
        IEventLogRepository eventLogRepository,
        IEventLogFactory eventLogFactory,
        IConfigurationRepository configurationRepository,
        ILogger<TimeMachineService> logger)
    {
        _importExportService = importExportService;
        _checkPointRepository = checkPointRepository;
        _checkPointDataRepository = checkPointDataRepository;
        _checkPointConverter = checkPointConverter;
        _eventLogRepository = eventLogRepository;
        _eventLogFactory = eventLogFactory;
        _configurationRepository = configurationRepository;
        _logger = logger;
        
        _importExportService.SetAuthenticatedUser(new ServiceUser());
    }

    public CheckPointCollectionDataContract GetCheckPoints(DateTime startDate, DateTime endDate)
    {
        var checkPoints =  _checkPointRepository.GetCheckPoints(startDate, endDate);
        var dataContracts = checkPoints.Select(a => _checkPointConverter.Convert(a));
        
        _earliestEvent ??= _checkPointRepository.GetEarliestEntry();
        return new CheckPointCollectionDataContract(_earliestEvent.Value, startDate, endDate, dataContracts);
    }

    public FigDataExportDataContract? GetCheckPointData(Guid dataId)
    {
        var data = _checkPointDataRepository.GetData(dataId);
        if (data?.ExportAsJson is not null)
        {
            if (data.ExportAsJson.TryParseJson(TypeNameHandling.Objects, out FigDataExportDataContract? export))
            {
                return export;
            }
        }

        return null;
    }

    public void CreateCheckPoint(CheckPointRecord record)
    {
        var config = _configurationRepository.GetConfiguration();
        if (!config.EnableTimeMachine)
        {
            _logger.LogInformation("Time machine is disabled, skipping checkpoint creation");
            return;
        }
        
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        lock (_lockObject)
        {
            _logger.LogInformation("Creating checkpoint with message {Message}", record.Message);
            var startTime = Stopwatch.GetTimestamp();
            var export = _importExportService.Export(false);

            if (export.Clients.Count == 0)
            {
                _logger.LogInformation("Skipping checkpoint creation as there are no clients");
                return;
            }
        
            var checkpoint = new CheckPointBusinessEntity
            {
                Timestamp = DateTime.UtcNow,
                NumberOfClients = export.Clients.Count,
                NumberOfSettings = export.Clients.Sum(a => a.Settings.Count),
                AfterEvent = record.Message,
                User = record.User
            };

            var checkpointData = new CheckPointDataBusinessEntity
            {
                ExportAsJson = JsonConvert.SerializeObject(export, JsonSettings.FigDefault),
                LastEncrypted = DateTime.UtcNow
            };
        
            var dataId = _checkPointDataRepository.Add(checkpointData);
            checkpoint.DataId = dataId;
            _logger.LogInformation("Saving checkpoint");
            _checkPointRepository.Add(checkpoint);
            _eventLogRepository.Add(_eventLogFactory.CheckpointCreated(record.Message));
            _logger.LogInformation("Checkpoint created successfully in {Duration}ms",
                Stopwatch.GetElapsedTime(startTime)
                    .TotalMilliseconds);
        }
    }

    public bool ApplyCheckPoint(Guid id)
    {
        var checkpoint = _checkPointRepository.GetCheckPoint(id);
        if (checkpoint is not null)
        {
            var data = _checkPointDataRepository.GetData(checkpoint.DataId);
            if (data is not null && data.ExportAsJson is not null)
            {
                if (!data.ExportAsJson.TryParseJson(TypeNameHandling.Objects,
                        out FigDataExportDataContract? export))
                {
                    _logger.LogWarning("Unable to parse data export");
                    return false;
                }
                
                export!.ImportType = ImportType.ClearAndImport;
                _logger.LogInformation("Applying checkpoint from {CheckPointDate} after event {Event}", checkpoint.Timestamp, checkpoint.AfterEvent);
                var result = _importExportService.Import(export, ImportMode.Api);
                _logger.LogInformation("Checkpoint apply deleted {DeletedClients} and imported {ImportedClients}",
                    result.DeletedClients.Count, result.ImportedClients.Count);
                _eventLogRepository.Add(_eventLogFactory.CheckPointApplied(AuthenticatedUser, checkpoint));
                return string.IsNullOrWhiteSpace(result.ErrorMessage);
            }
        }

        return false;
    }

    public bool UpdateCheckPoint(Guid checkPointId, CheckPointUpdateDataContract contract)
    {
        var checkPoint = _checkPointRepository.GetCheckPoint(checkPointId);
        if (checkPoint is not null)
        {
            checkPoint.Note = contract.Note;
            _checkPointRepository.UpdateCheckPoint(checkPoint);
            _eventLogRepository.Add(_eventLogFactory.NoteAddedToCheckPoint(AuthenticatedUser, checkPoint));
            return true;
        }

        return false;
    }
}