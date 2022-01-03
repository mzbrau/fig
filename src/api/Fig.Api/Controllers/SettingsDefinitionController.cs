using Fig.Api.Services;
using Fig.Contracts.SettingDefinitions;
using Microsoft.AspNetCore.Mvc;

namespace Fig.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class SettingsDefinitionController : ControllerBase
{
    private readonly ILogger<SettingsDefinitionController> _logger;
    private readonly ISettingsService _settingsService;

    public SettingsDefinitionController(ILogger<SettingsDefinitionController> logger, ISettingsService settingsService)
    {
        _logger = logger;
        _settingsService = settingsService;
    }
    
    /// <summary>
    /// Called by the client when it registers its settings.
    /// </summary>
    /// <param name="settingsClientDefinition">The settings to be registered.</param>
    /// <returns>A result.</returns>
    [HttpPost(Name = "PostSettingDefinition")]
    public IActionResult Post([FromBody] SettingsClientDefinitionDataContract settingsClientDefinition)
    {
        _logger.LogInformation(
            "Registering settings for service {serviceName}",
            settingsClientDefinition.Name);
        _settingsService.RegisterSettings(settingsClientDefinition);
        return Ok();
    }
}