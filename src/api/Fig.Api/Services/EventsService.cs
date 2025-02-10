using System.Diagnostics;
using Fig.Api.Converters;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Observability;
using Fig.Contracts.Authentication;
using Fig.Contracts.EventHistory;

namespace Fig.Api.Services;

public class EventsService : AuthenticatedService, IEventsService
{
    private readonly IEventLogRepository _eventLogRepository;
    private readonly IEventsConverter _eventsConverter;
    private readonly ILogger<EventsService> _logger;
    private DateTime? _earliestEvent;

    public EventsService(IEventLogRepository eventLogRepository, IEventsConverter eventsConverter, ILogger<EventsService> logger)
    {
        _eventLogRepository = eventLogRepository;
        _eventsConverter = eventsConverter;
        _logger = logger;
    }

    public async Task<EventLogCollectionDataContract> GetEventLogs(DateTime startTime, DateTime endTime)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        if (startTime > endTime)
            throw new ArgumentException("Start time cannot be after the end time");

        _earliestEvent ??= await _eventLogRepository.GetEarliestEntry();
        var onlyUnrestricted = AuthenticatedUser != null && AuthenticatedUser.Role != Role.Administrator;
        var events = await _eventLogRepository.GetAllLogs(startTime, endTime, onlyUnrestricted, AuthenticatedUser);

        var eventsDataContract = events.Select(log => _eventsConverter.Convert(log));
        return new EventLogCollectionDataContract(_earliestEvent.Value, startTime, endTime, eventsDataContract);
    }

    public async Task<EventLogCountDataContract> GetEventLogCount()
    {
        var count = await _eventLogRepository.GetEventLogCount();
        return new EventLogCountDataContract(count);
    }
}