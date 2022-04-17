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
            _statusService.SyncStatus(HttpUtility.UrlDecode(clientName), instance, clientSecret, statusRequest);
        return Ok(response);
    }

    [Authorize(Role.Administrator)]
    [HttpPut("{clientName}/configuration")]
    public IActionResult UpdateConfiguration(string clientName,
        [FromQuery] string? instance,
        [FromBody] ClientConfigurationDataContract updatedConfiguration)
    {
        _statusService.UpdateConfiguration(HttpUtility.UrlDecode(clientName), HttpUtility.UrlDecode(instance),
            updatedConfiguration);
        return Ok();
    }

    [Authorize(Role.Administrator)]
    [HttpGet]
    public IActionResult GetAll()
    {
        var allStatuses = _statusService.GetAll();
        return Ok(allStatuses);
    }
}