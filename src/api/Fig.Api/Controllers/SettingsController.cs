using Fig.Api.Services;
using Fig.Contracts.Settings;
using Microsoft.AspNetCore.Mvc;

namespace Fig.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class SettingsController : ControllerBase
{
    private readonly ILogger<SettingsController> _logger;
    private readonly ISettingsService _settingsService;

    public SettingsController(ILogger<SettingsController> logger, ISettingsService settingsService)
    {
        _logger = logger;
        _settingsService = settingsService;
    }
    
    /// <summary>
    /// Called by the client on startup when retrieving settings
    /// </summary>
    /// <returns>Settings</returns>
    [HttpGet(Name = "GetSettings")]
    public IActionResult Get(
        [FromBody] SettingRequestDataContract request)
    {
        _logger.LogInformation(
            "Getting settings for service {serviceName} with parameters " +
            "hostname:{hostname}, username:{username}, instance:{instance}",
            request.ClientName, request.Qualifiers.Hostname, request.Qualifiers.Username,
            request.Qualifiers.Instance);
        return Ok(_settingsService.GetSettings(request));
    }
    
    /// <summary>
    /// Called by the web client when updating settings.
    /// </summary>
    /// <returns></returns>
    [HttpPut(Name = "PutSettings")]
    public IActionResult Put([FromBody] SettingsClientDataContract settingsClient)
    {
        _logger.LogInformation(
            "Updating settings for service {serviceName} with parameters " +
            "hostname:{hostname}, username:{username}, instance:{instance}",
            settingsClient.Name, settingsClient.Qualifiers.Hostname,
            settingsClient.Qualifiers.Username, settingsClient.Qualifiers.Instance);

        _settingsService.UpdateSettingValues(settingsClient);
        
        return Ok();
    }
}