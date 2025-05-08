using Fig.Api.Attributes;
using Fig.Api.Services;
using Fig.Contracts.Authentication;
using Fig.Contracts.CheckPoint;
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
    [HttpGet]
    public async Task<IActionResult> GetCheckPoints(
        [FromQuery] DateTime startTime,
        [FromQuery] DateTime endTime)
    {
        var result = await _timeMachineService.GetCheckPoints(startTime, endTime);
        return Ok(result);
    }
    
    [Authorize(Role.Administrator)]
    [HttpGet("data")]
    public async Task<IActionResult> GetCheckpointData(
        [FromQuery] Guid dataId)
    {
        var result = await _timeMachineService.GetCheckPointData(dataId);
        if (result is null)
        {
            return NotFound();
        }
        
        return Ok(result);
    }
    
    [Authorize(Role.Administrator)]
    [HttpPut("{checkPointId}")]
    public async Task<IActionResult> ApplyCheckPoint(
        [FromRoute] Guid checkPointId)
    {
        var succeeded = await _timeMachineService.ApplyCheckPoint(checkPointId);
        if (!succeeded)
        {
            return BadRequest();
        }
        
        return Ok();
    }
    
    [Authorize(Role.Administrator)]
    [HttpPut("{checkPointId}/note")]
    public async Task<IActionResult> UpdateCheckPoint(
        [FromRoute] Guid checkPointId,
        [FromBody] CheckPointUpdateDataContract dataContract)
    {
        var succeeded = await _timeMachineService.UpdateCheckPoint(checkPointId, dataContract);
        if (!succeeded)
        {
            return BadRequest();
        }
        
        return Ok();
    }

    // Really just for integration tests
    [Authorize(Role.Administrator)]
    [HttpDelete]
    public async Task<IActionResult> DeleteAllCheckPointTriggers()
    {
        await _timeMachineService.DeleteAllCheckPointTriggers();
        return Ok();
    }
}