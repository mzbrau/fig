using Fig.Api.Services;
using Fig.Contracts.SettingConfiguration;
using Microsoft.AspNetCore.Mvc;

namespace Fig.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class SettingsConfigurationController : ControllerBase
{
    private readonly ILogger<SettingsController> _logger;
    private readonly ISettingsService _settingsService;

    public SettingsConfigurationController(ILogger<SettingsController> logger, ISettingsService settingsService)
    {
        _logger = logger;
        _settingsService = settingsService;
    }
    
    /// <summary>
    /// Called by the web client to display settings for configuration
    /// </summary>
    /// <returns></returns>
    [HttpGet(Name = "GetSettingsConfiguration")]
    public IActionResult Get()
    {
        _logger.LogInformation("Getting all settings for configuration");
        return Ok(_settingsService.GetSettingsForConfiguration());
    }
}