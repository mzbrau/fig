using Fig.Common.Events;
using Fig.Contracts.CheckPoint;
using Fig.Contracts.ImportExport;
using Fig.Web.Converters;
using Fig.Web.Events;
using Fig.Web.Models.TimeMachine;
using Fig.Web.Services;

namespace Fig.Web.Facades;

public class TimeMachineFacade : ITimeMachineFacade
{
    private readonly IHttpService _httpService;
    private readonly ICheckPointConverter _checkPointConverter;
    private DateTime _currentStartTimeQuery = DateTime.MinValue;
    private DateTime _currentEndTimeQuery = DateTime.MinValue;

    public TimeMachineFacade(IHttpService httpService, ICheckPointConverter checkPointConverter, IEventDistributor eventDistributor)
    {
        _httpService = httpService;
        _checkPointConverter = checkPointConverter;
        eventDistributor.Subscribe(EventConstants.LogoutEvent, () =>
        {
            CheckPoints.Clear();
            StartTime = DateTime.Now - TimeSpan.FromDays(1);
            EndTime = DateTime.Now;
        });
    }

    public List<CheckPointModel> CheckPoints { get; } = new();
    
    public DateTime EarliestDate { get; private set; } = DateTime.MinValue;
    
    public DateTime StartTime { get; set; } = DateTime.Now - TimeSpan.FromDays(1);
    
    public DateTime EndTime { get; set; } = DateTime.Now;

    public async Task QueryCheckPoints(DateTime startTime, DateTime endTime)
    {
        if (_currentStartTimeQuery == startTime && _currentEndTimeQuery == endTime || startTime > endTime)
            return;

        var uri = $"timemachine" +
                  $"?startTime={Uri.EscapeDataString(startTime.ToUniversalTime().ToString("o"))}" +
                  $"&endTime={Uri.EscapeDataString(endTime.ToUniversalTime().ToString("o"))}";
        var result = await _httpService.Get<CheckPointCollectionDataContract>(uri);

        if (result == null)
            return;
        
        _currentEndTimeQuery = result.ResultEndTime;
        _currentStartTimeQuery = result.ResultStartTime;
        EarliestDate = result.EarliestCheckPoint;

        CheckPoints.Clear();
        foreach (var checkPoint in result.CheckPoints.Select(ev => _checkPointConverter.Convert(ev)))
        {
            CheckPoints.Add(checkPoint);
        }
        
        Console.WriteLine($"Loaded {CheckPoints.Count} check points");
    }

    public async Task<FigDataExportDataContract?> DownloadCheckPoint(CheckPointModel checkPoint)
    {
        var uri = $"/timemachine/data?dataId={Uri.EscapeDataString(checkPoint.DataId.ToString())}";
        return await _httpService.Get<FigDataExportDataContract>(uri);
    }

    public async Task ApplyCheckPoint(CheckPointModel checkPoint)
    {
        var uri = $"/timemachine/{Uri.EscapeDataString(checkPoint.Id.ToString())}";
        await _httpService.Put<HttpResponseMessage>(uri, null);
    }

    public async Task AddNoteToCheckPoint(CheckPointModel checkPoint, string note)
    {
        var update = new CheckPointUpdateDataContract(note);
        var uri = $"/timemachine/{Uri.EscapeDataString(checkPoint.Id.ToString())}/note";
        await _httpService.Put<HttpResponseMessage>(uri, update);
    }
}