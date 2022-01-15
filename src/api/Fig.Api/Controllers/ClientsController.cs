using System.Web;
using Fig.Api.Exceptions;
using Fig.Api.Services;
using Fig.Api.Validators;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Microsoft.AspNetCore.Mvc;

namespace Fig.Api.Controllers;

[ApiController]
[Route("api/clients")]
public class ClientsController : ControllerBase
{
    private readonly IClientSecretValidator _clientSecretValidator;
    private readonly ILogger<ClientsController> _logger;
    private readonly ISettingsService _settingsService;

    public ClientsController(ILogger<ClientsController> logger, ISettingsService settingsService,
        IClientSecretValidator clientSecretValidator)
    {
        _logger = logger;
        _settingsService = settingsService;
        _clientSecretValidator = clientSecretValidator;
    }

    /// <summary>
    ///     Called by the web client to display settings for configuration.
    ///     TODO: Security.
    /// </summary>
    /// <returns>A collection of all registered clients and their setting definitions</returns>
    [HttpGet]
    public IActionResult GetAllClients()
    {
        try
        {
            var clients = _settingsService.GetAllClients();
            return Ok(clients);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error when getting all clients {ex}", ex);
            return StatusCode(500);
        }
    }

    /// <summary>
    ///     Called by the client on startup when retrieving settings
    /// </summary>
    /// <returns>Settings</returns>
    [HttpGet("{clientName}/settings")]
    public IActionResult GetSettingsByName(string clientName,
        [FromHeader] string? clientSecret,
        [FromQuery] string? instance)
    {
        if (string.IsNullOrWhiteSpace(clientSecret)) return Unauthorized();

        IEnumerable<SettingDataContract> settings;
        try
        {
            settings = _settingsService.GetSettings(HttpUtility.UrlDecode(clientName), clientSecret, instance);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error when getting settings by id {ex}", ex);
            return BadRequest();
        }

        return Ok(settings);
    }

    /// <summary>
    ///     Called by the client when it registers its settings.
    /// </summary>
    /// <param name="clientSecret"></param>
    /// <param name="settingsClientDefinition">The settings to be registered.</param>
    /// <returns>An id for callback.</returns>
    [HttpPost]
    public async Task<IActionResult> RegisterClient([FromHeader] string clientSecret,
        [FromBody] SettingsClientDefinitionDataContract settingsClientDefinition)
    {
        if (!_clientSecretValidator.IsValid(clientSecret))
            return BadRequest("Client secret is invalid. It must be a string representation of a GUID");

        try
        {
            await _settingsService.RegisterSettings(clientSecret, settingsClientDefinition);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError($"Error when registering client {ex}", ex);
            return Unauthorized(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error when registering client {ex}", ex);
            return BadRequest();
        }

        return Ok();
    }

    /// <summary>
    ///     Update Settings via web client
    /// </summary>
    [HttpPut("{clientName}/settings")]
    public IActionResult UpdateSettingValues(string clientName,
        [FromQuery] string? instance,
        [FromBody] IEnumerable<SettingDataContract> updatedSettings)
    {
        try
        {
            _settingsService.UpdateSettingValues(HttpUtility.UrlDecode(clientName), HttpUtility.UrlDecode(instance),
                updatedSettings);
        }
        catch (InvalidClientException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error when updating setting values {ex}", ex);
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
            _settingsService.DeleteClient(HttpUtility.UrlDecode(clientName), HttpUtility.UrlDecode(instance));
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error when deleting client {ex}", ex);
            return BadRequest();
        }

        return Ok();
    }
}