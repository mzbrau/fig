using System.Web;
using Fig.Api.Attributes;
using Fig.Api.Exceptions;
using Fig.Api.Services;
using Fig.Api.Validators;
using Fig.Contracts.Authentication;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Microsoft.AspNetCore.Mvc;

namespace Fig.Api.Controllers;

[ApiController]
[Route("clients")]
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
    /// </summary>
    /// <returns>A collection of all registered clients and their setting definitions</returns>
    [Authorize(Role.Administrator, Role.User)]
    [HttpGet]
    public IActionResult GetAllClients()
    {
        var clients = _settingsService.GetAllClients();
        return Ok(clients);
    }

    /// <summary>
    ///     Called by the client on startup when retrieving settings
    /// </summary>
    /// <returns>Settings</returns>
    [AllowAnonymous]
    [HttpGet("{clientName}/settings")]
    public IActionResult GetSettingsByName(string clientName,
        [FromHeader] string? clientSecret,
        [FromQuery] string? instance)
    {
        if (string.IsNullOrWhiteSpace(clientSecret))
            return Unauthorized();

        var settings = _settingsService.GetSettings(Uri.EscapeDataString(clientName), clientSecret, instance);
        return Ok(settings);
    }

    [Authorize(Role.Administrator, Role.User)]
    [HttpGet("{clientName}/settings/{settingName}/history")]
    public IActionResult GetSettingHistory(string clientName, string settingName, [FromQuery] string? instance)
    {
        var history = _settingsService.GetSettingHistory(clientName, settingName, instance);
        return Ok(history);
    }

    /// <summary>
    ///     Called by the client when it registers its settings.
    /// </summary>
    /// <param name="clientSecret"></param>
    /// <param name="settingsClientDefinition">The settings to be registered.</param>
    /// <returns>An id for callback.</returns>
    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> RegisterClient([FromHeader] string clientSecret,
        [FromBody] SettingsClientDefinitionDataContract settingsClientDefinition)
    {
        if (!_clientSecretValidator.IsValid(clientSecret))
            throw new InvalidClientSecretException(
                "Client secret is invalid. It must be a string representation of a GUID");

        await _settingsService.RegisterSettings(clientSecret, settingsClientDefinition);
        return Ok();
    }

    /// <summary>
    ///     Update Settings via web client
    /// </summary>
    [Authorize(Role.Administrator, Role.User)]
    [HttpPut("{clientName}/settings")]
    public IActionResult UpdateSettingValues(string clientName,
        [FromQuery] string? instance,
        [FromBody] IEnumerable<SettingDataContract> updatedSettings)
    {
        _settingsService.UpdateSettingValues(Uri.EscapeDataString(clientName), instance != null ? Uri.EscapeDataString(instance) : null,
            updatedSettings);
        return Ok();
    }

    [Authorize(Role.Administrator)]
    [HttpDelete("{clientName}")]
    public IActionResult DeleteClient(string clientName,
        [FromQuery] string? instance)
    {
        _settingsService.DeleteClient(Uri.EscapeDataString(clientName), instance != null ? Uri.EscapeDataString(instance) : null);
        return Ok();
    }

    [Authorize(Role.Administrator, Role.User)]
    [HttpPut("{clientName}/verifications/{verificationName}")]
    public async Task<IActionResult> RunVerification(string clientName, string verificationName,
        [FromQuery] string? instance)
    {
        var result = await _settingsService.RunVerification(clientName, verificationName, instance);
        return Ok(result);
    }

    [Authorize(Role.Administrator, Role.User)]
    [HttpGet("{clientName}/verifications/{verificationName}/history")]
    public IActionResult GetVerificationHistory(string clientName, string verificationName,
        [FromQuery] string? instance)
    {
        var history = _settingsService.GetVerificationHistory(clientName, verificationName, instance);
        return Ok(history);
    }
}