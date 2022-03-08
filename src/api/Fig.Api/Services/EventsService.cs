using Fig.Api.Converters;
using Fig.Api.Datalayer.Repositories;
using Fig.Contracts.EventHistory;

namespace Fig.Api.Services;

public class EventsService : IEventsService
{
    private readonly IEventLogRepository _eventLogRepository;
    private readonly IEventsConverter _eventsConverter;
    private DateTime? _earliestEvent;

    public EventsService(IEventLogRepository eventLogRepository, IEventsConverter eventsConverter)
    {
        _eventLogRepository = eventLogRepository;
        _eventsConverter = eventsConverter;
    }
    
    public EventLogCollectionDataContract GetEventLogs(DateTime startTime, DateTime endTime)
    {
        if (startTime > endTime)
            throw new ArgumentException("Start time cannot be after the end time");
        
        _earliestEvent ??= _eventLogRepository.GetEarliestEntry();
        var events = _eventLogRepository.GetAllLogs(startTime, endTime);

        return new EventLogCollectionDataContract()
        {
            EarliestEvent = _earliestEvent.Value,
            ResultStartTime = startTime,
            ResultEndTime = endTime,
            Events = events.Select(log => _eventsConverter.Convert(log))
        };
    }
}