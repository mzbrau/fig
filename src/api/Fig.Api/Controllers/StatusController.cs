using System.Web;
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
    public IActionResult GetStatus(string clientName,
        [FromHeader] string? clientSecret,
        [FromQuery] string? instance,
        [FromBody] StatusRequestDataContract statusRequest)
    {
        if (string.IsNullOrWhiteSpace(clientSecret))
            return Unauthorized();

        var response =
            _statusService.SyncStatus(Uri.EscapeDataString(clientName), instance, clientSecret, statusRequest);
        return Ok(response);
    }

    [Authorize(Role.Administrator)]
    [HttpPut("{clientName}/configuration")]
    public IActionResult UpdateConfiguration(string clientName,
        [FromQuery] string? instance,
        [FromBody] ClientConfigurationDataContract updatedConfiguration)
    {
        var updatedConfig = _statusService.UpdateConfiguration(Uri.EscapeDataString(clientName),
            instance != null ? Uri.EscapeDataString(instance) : null,
            updatedConfiguration);
        return Ok(updatedConfig);
    }

    [Authorize(Role.Administrator)]
    [HttpGet]
    public IActionResult GetAll()
    {
        var allStatuses = _statusService.GetAll();
        return Ok(allStatuses);
    }
}