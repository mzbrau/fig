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

    [Authorize(Role.Administrator, Role.User)]
    [HttpGet]
    public IActionResult GetEvents(
        [FromQuery] DateTime startTime,
        [FromQuery] DateTime endTime)
    {
        var result = _eventService.GetEventLogs(startTime, endTime);
        return Ok(result);
    }
}