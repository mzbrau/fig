using Fig.Api.Attributes;
using Fig.Api.Services;
using Fig.Contracts.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Fig.Api.Controllers;

[ApiController]
[Route("timemachine")]
public class TimeMachineController : ControllerBase
{
    private readonly ITimeMachineService _timeMachineService;

    public TimeMachineController(ITimeMachineService timeMachineService)
    {
        _timeMachineService = timeMachineService;
    }
    
    [Authorize(Role.Administrator)]
    public IActionResult GetEvents(
        [FromQuery] DateTime startTime,
        [FromQuery] DateTime endTime)
    {
        var result = _timeMachineService.GetCheckPoints(startTime, endTime);
        return Ok(result);
    }
    
    [Authorize(Role.Administrator)]
    [HttpGet("Data")]
    public IActionResult GetCheckpointData(
        [FromQuery] Guid dataId)
    {
        var result = _timeMachineService.GetCheckPointData(dataId);
        if (result is null)
        {
            return NotFound();
        }
        
        return Ok(result);
    }
}