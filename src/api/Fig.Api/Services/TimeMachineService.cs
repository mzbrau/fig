using Fig.Api.Constants;
using Fig.Api.Datalayer.Repositories;
using Fig.Common.Events;
using Fig.Datalayer;
using Fig.Datalayer.BusinessEntities;
using Newtonsoft.Json;

namespace Fig.Api.Services;

public class TimeMachineService : ITimerMachineService
{
    private readonly IEventDistributor _eventDistributor;
    private readonly IImportExportService _importExportService;
    private readonly ICheckPointRepository _checkPointRepository;
    private readonly ICheckPointDataRepository _checkPointDataRepository;

    public TimeMachineService(IEventDistributor eventDistributor,
        IImportExportService importExportService,
        ICheckPointRepository checkPointRepository,
        ICheckPointDataRepository checkPointDataRepository)
    {
        _eventDistributor = eventDistributor;
        _importExportService = importExportService;
        _checkPointRepository = checkPointRepository;
        _checkPointDataRepository = checkPointDataRepository;

        _eventDistributor.Subscribe<string>(EventConstants.CheckPointRequired, CreateCheckPoint);
    }

    public void GetCheckPoints(DateTime startDate, DateTime endDate)
    {
        var checkPoints =  _checkPointRepository.GetCheckPoints(startDate, endDate);
        
        // TODO: Convert and return (need to write the converter)
    }

    private void CreateCheckPoint(string message)
    {
        var export = _importExportService.Export();
        var checkpoint = new CheckPointBusinessEntity
        {
            Timestamp = DateTime.UtcNow,
            NumberOfClients = export.Clients.Count,
            NumberOfSettings = export.Clients.Sum(a => a.Settings.Count),
            AfterEvent = message
        };

        var checkpointData = new CheckPointDataBusinessEntity
        {
            ExportAsJson = JsonConvert.SerializeObject(export)
        };
        
        var dataId = _checkPointDataRepository.Add(checkpointData);
        checkpoint.DataId = dataId;
        _checkPointRepository.Add(checkpoint);
    }
}