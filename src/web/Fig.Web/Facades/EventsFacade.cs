using Fig.Common.Events;
using Fig.Contracts.EventHistory;
using Fig.Web.Converters;
using Fig.Web.Events;
using Fig.Web.Models.Events;
using Fig.Web.Services;

namespace Fig.Web.Facades;

public class EventsFacade : IEventsFacade
{
    private readonly IHttpService _httpService;
    private readonly IEventLogConverter _eventLogConverter;
    private DateTime _currentStartTimeQuery = DateTime.MinValue;
    private DateTime _currentEndTimeQuery = DateTime.MinValue;

    public EventsFacade(IHttpService httpService, IEventLogConverter eventLogConverter, IEventDistributor eventDistributor)
    {
        _httpService = httpService;
        _eventLogConverter = eventLogConverter;
        eventDistributor.Subscribe(EventConstants.LogoutEvent, () =>
        {
            EventLogs.Clear();
            StartTime = DateTime.Now - TimeSpan.FromDays(1);
            EndTime = DateTime.Now;
        });
    }
    
    public List<EventLogModel> EventLogs { get; } = new();

    public DateTime EarliestDate { get; private set; } = DateTime.MinValue;
    
    public DateTime StartTime { get; set; } = DateTime.Now - TimeSpan.FromDays(1);
    
    public DateTime EndTime { get; set; } = DateTime.Now;

    public async Task QueryEvents(DateTime startTime, DateTime endTime)
    {
        if (_currentStartTimeQuery == startTime && _currentEndTimeQuery == endTime || startTime > endTime)
            return;

        var uri = $"events" +
                  $"?startTime={Uri.EscapeDataString(startTime.ToUniversalTime().ToString("o"))}" +
                  $"&endTime={Uri.EscapeDataString(endTime.ToUniversalTime().ToString("o"))}";
        var result = await _httpService.Get<EventLogCollectionDataContract>(uri);

        if (result == null)
            return;
        
        _currentEndTimeQuery = result.ResultEndTime;
        _currentStartTimeQuery = result.ResultStartTime;
        EarliestDate = result.EarliestEvent;

        EventLogs.Clear();
        foreach (var log in result.Events.Select(ev => _eventLogConverter.Convert(ev)))
        {
            EventLogs.Add(log);
        }
        
        Console.WriteLine($"Loaded {EventLogs.Count} events");
    }

    public async Task<List<EventLogModel>> GetClientTimeline(string clientName, string? instance)
    {
        var uri = $"events/client/{Uri.EscapeDataString(clientName)}/timeline";
        if (!string.IsNullOrEmpty(instance))
        {
            uri += $"?instance={Uri.EscapeDataString(instance)}";
        }
        
        var result = await _httpService.Get<EventLogCollectionDataContract>(uri);
        
        if (result == null)
            return new List<EventLogModel>();
        
        return result.Events.Select(ev => _eventLogConverter.Convert(ev)).ToList();
    }
}