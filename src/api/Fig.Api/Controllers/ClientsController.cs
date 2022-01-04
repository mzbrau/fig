using Fig.Api.Services;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Microsoft.AspNetCore.Mvc;

namespace Fig.Api.Controllers;

[ApiController]
[Route("api/clients")]
public class ClientsController : ControllerBase
{
    private readonly ILogger<ClientsController> _logger;
    private readonly ISettingsService _settingsService;

    public ClientsController(ILogger<ClientsController> logger, ISettingsService settingsService)
    {
        _logger = logger;
        _settingsService = settingsService;
    }
    
    /// <summary>
    /// Called by the web client to display settings for configuration.
    /// TODO: Security.
    /// </summary>
    /// <returns>A collection of all registered clients and their setting definitions</returns>
    [HttpGet]
    public IActionResult GetAllClients()
    {
        return Ok(_settingsService.GetAllClients());
    }
    
    /// <summary>
    /// Called by the client on startup when retrieving settings
    /// </summary>
    /// <returns>Settings</returns>
    [HttpGet("{clientName}/settings")]
    public IActionResult GetSettingsById(string id,
        [FromHeader] string? clientSecret,
        [FromQuery] string? instance)
    {
        if (string.IsNullOrWhiteSpace(clientSecret))
        {
            return Unauthorized();
        }

        IEnumerable<SettingDataContract> settings;
        try
        {
            settings = _settingsService.GetSettings(id, clientSecret, instance);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception)
        {
            return BadRequest();
        }
        
        return Ok(settings);
    }

    /// <summary>
    /// Called by the client when it registers its settings.
    /// </summary>
    /// <param name="clientSecret"></param>
    /// <param name="settingsClientDefinition">The settings to be registered.</param>
    /// <returns>An id for callback.</returns>
    [HttpPost]
    public IActionResult RegisterClient([FromHeader] string clientSecret, 
        [FromBody] SettingsClientDefinitionDataContract settingsClientDefinition)
    {
        string clientId;
        try
        {
            clientId = _settingsService.RegisterSettings(clientSecret, settingsClientDefinition);
        }
        catch (Exception)
        {
            return BadRequest();
        }
        
        return Ok(clientId);
    }
    
    /// <summary>
    /// Update Settings via web client
    /// </summary>
    [HttpPut("{clientName}/settings")]
    public IActionResult UpdateSettingValues(string clientName,
        [FromQuery] string? instance,
        [FromBody] IEnumerable<SettingDataContract> updatedSettings)
    {
        try
        {
            _settingsService.UpdateSettingValues(clientName, instance, updatedSettings);
        }
        catch (Exception)
        {
            return BadRequest();
        }
        
        return Ok();
    }
    
    [HttpDelete("{clientName}")]
    public IActionResult DeleteClient(string clientName, 
        [FromQuery] string? instance)
    {
        try
        {
            _settingsService.DeleteClient(clientName, instance);
        }
        catch (Exception)
        {
            return BadRequest();
        }
        
        return Ok();
    }
}