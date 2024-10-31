using Fig.Api.Converters;
using Fig.Api.Datalayer.Repositories;
using Fig.Contracts.CheckPoint;
using Fig.Contracts.ImportExport;
using Fig.Datalayer.BusinessEntities;
using Newtonsoft.Json;

namespace Fig.Api.Services;

public class TimeMachineService : ITimeMachineService
{
    private readonly IImportExportService _importExportService;
    private readonly ICheckPointRepository _checkPointRepository;
    private readonly ICheckPointDataRepository _checkPointDataRepository;
    private readonly ICheckPointConverter _checkPointConverter;

    public TimeMachineService(
        IImportExportService importExportService,
        ICheckPointRepository checkPointRepository,
        ICheckPointDataRepository checkPointDataRepository,
        ICheckPointConverter checkPointConverter)
    {
        _importExportService = importExportService;
        _checkPointRepository = checkPointRepository;
        _checkPointDataRepository = checkPointDataRepository;
        _checkPointConverter = checkPointConverter;
    }

    public CheckPointCollectionDataContract GetCheckPoints(DateTime startDate, DateTime endDate)
    {
        var checkPoints =  _checkPointRepository.GetCheckPoints(startDate, endDate);
        var dataContracts = checkPoints.Select(a => _checkPointConverter.Convert(a));
        
        // TODO Set earliest point correctly
        return new CheckPointCollectionDataContract(DateTime.MinValue, startDate, endDate, dataContracts);
    }

    public FigDataExportDataContract? GetCheckPointData(Guid dataId)
    {
        var data = _checkPointDataRepository.GetData(dataId);
        if (data?.ExportAsJson is not null)
        {
            var export = JsonConvert.DeserializeObject<FigDataExportDataContract>(data.ExportAsJson);
            return export;
        }

        return null;
    }

    public void CreateCheckPoint(string message)
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