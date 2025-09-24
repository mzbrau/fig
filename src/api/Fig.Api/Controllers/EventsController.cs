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
    private readonly IConfigurationService _configurationService;

    public EventsController(ILogger<EventsController> logger, IEventsService eventService, IConfigurationService configurationService)
    {
        _logger = logger;
        _eventService = eventService;
        _configurationService = configurationService;
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

    [Authorize(Role.Administrator, Role.User, Role.ReadOnly)]
    [HttpGet("client/{clientName}/timeline")]
    public async Task<IActionResult> GetClientTimeline(
        [FromRoute] string clientName,
        [FromQuery] string? instance = null)
    {
        var configuration = await _configurationService.GetConfiguration();
        var endTime = DateTime.UtcNow;
        var startTime = endTime.AddDays(-configuration.TimelineDurationDays);
        var result = await _eventService.GetClientSettingChanges(startTime, endTime, clientName, instance);
        return Ok(result);
    }
}