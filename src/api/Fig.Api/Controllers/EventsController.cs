using Fig.Api.Attributes;
using Fig.Api.Services;
using Fig.Contracts.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Fig.Api.Controllers;

[ApiController]
[Route("events")]
public class EventsController : ControllerBase
{
    private readonly IEventsService _eventService;
    private readonly ILogger<EventsController> _logger;

    public EventsController(ILogger<EventsController> logger, IEventsService eventService)
    {
        _logger = logger;
        _eventService = eventService;
    }

    [Authorize(Role.Administrator, Role.User, Role.ReadOnly)]
    [HttpGet]
    public async Task<IActionResult> GetEvents(
        [FromQuery] DateTime startTime,
        [FromQuery] DateTime endTime)
    {
        var result = await _eventService.GetEventLogs(startTime, endTime);
        return Ok(result);
    }

    [Authorize(Role.Administrator)]
    [HttpGet("Count")]
    public async Task<IActionResult> GetEventLogCount()
    {
        var count = await _eventService.GetEventLogCount();
        return Ok(count);
    }
}