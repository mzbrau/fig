using Fig.Api.Attributes;
using Fig.Api.Services;
using Fig.Contracts.Authentication;
using Fig.Contracts.Status;
using Microsoft.AspNetCore.Mvc;

namespace Fig.Api.Controllers;

[ApiController]
[Route("statuses")]
public class StatusController : ControllerBase
{
    private readonly IStatusService _statusService;

    public StatusController(IStatusService statusService)
    {
        _statusService = statusService;
    }

    [AllowAnonymous]
    [HttpPut("{clientName}")]
    public async Task<IActionResult> GetStatus(string clientName,
        [FromHeader] string? clientSecret,
        [FromQuery] string? instance,
        [FromBody] StatusRequestDataContract statusRequest)
    {
        if (string.IsNullOrWhiteSpace(clientSecret))
            return Unauthorized();

        var response =
            await _statusService.SyncStatus(clientName, instance, clientSecret, statusRequest);
        return Ok(response);
    }

    [Authorize(Role.Administrator, Role.User)]
    [HttpPut("{clientRunSessionId}/liveReload")]
    public async Task<IActionResult> SetLiveReload(Guid clientRunSessionId,
        [FromQuery] bool liveReload)
    {
        await _statusService.SetLiveReload(clientRunSessionId, liveReload);
        return Ok();
    }
    
    [Authorize(Role.Administrator, Role.User)]
    [HttpPut("{clientRunSessionId}/restart")]
    public async Task<IActionResult> RequestRestart(Guid clientRunSessionId)
    {
        await _statusService.RequestRestart(clientRunSessionId);
        return Ok();
    }

    [Authorize(Role.Administrator, Role.User, Role.ReadOnly)]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var allStatuses = await _statusService.GetAll();
        return Ok(allStatuses);
    }
}