using Fig.Api.Attributes;
using Fig.Api.Services;
using Fig.Contracts.Authentication;
using Fig.Contracts.Scheduling;
using Microsoft.AspNetCore.Mvc;

namespace Fig.Api.Controllers;

[ApiController]
[Route("scheduling")]
public class SchedulingController : ControllerBase
{
    private readonly ILogger<SchedulingController> _logger;
    private readonly ISchedulingService _schedulingService;

    public SchedulingController(ILogger<SchedulingController> logger, ISchedulingService schedulingService)
    {
        _logger = logger;
        _schedulingService = schedulingService;
    }
    
    [Authorize(Role.Administrator)]
    [HttpGet]
    public async Task<IActionResult> GetAllDeferredChanges()
    {
        var items = await _schedulingService.GetAllDeferredChanges();
        return Ok(items);
    }

    [Authorize(Role.Administrator)]
    [HttpPut("{deferredChangeId}")]
    public async Task<IActionResult> RescheduleChange([FromRoute] Guid deferredChangeId,
        [FromBody] RescheduleDeferredChangeDataContract change)
    {
        await _schedulingService.RescheduleChange(deferredChangeId, change);
        return Ok();
    }
    
    [Authorize(Role.Administrator)]
    [HttpDelete("{deferredChangeId}")]
    public async Task<IActionResult> DeleteDeferredChange([FromRoute] Guid deferredChangeId)
    {
        await _schedulingService.DeleteDeferredChange(deferredChangeId);
        return Ok();
    }
}