using Fig.Api.Attributes;
using Fig.Api.Exceptions;
using Fig.Api.Services;
using Fig.Api.Validators;
using Fig.Common.NetStandard.Validation;
using Fig.Contracts.Authentication;
using Fig.Contracts.SettingClients;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Microsoft.AspNetCore.Mvc;

namespace Fig.Api.Controllers;

[ApiController]
[Route("clients")]
public class ClientsController : ControllerBase
{
    private readonly IClientSecretValidator _clientSecretValidator;
    private readonly IClientNameValidator _clientNameValidator;
    private readonly ISettingsService _settingsService;

    public ClientsController(ISettingsService settingsService,
        IClientSecretValidator clientSecretValidator,
        IClientNameValidator clientNameValidator)
    {
        _settingsService = settingsService;
        _clientSecretValidator = clientSecretValidator;
        _clientNameValidator = clientNameValidator;
    }

    /// <summary>
    ///     Called by the web client to display settings for configuration.
    /// </summary>
    /// <returns>A collection of all registered clients and their setting definitions</returns>
    [Authorize(Role.Administrator, Role.User, Role.ReadOnly)]
    [HttpGet]
    public async Task<IActionResult> GetAllClients()
    {
        var clients = await _settingsService.GetAllClients();
        return Ok(clients);
    }

    /// <summary>
    ///     Called by the web client to get just the names and descriptions of all clients.
    /// </summary>
    /// <returns>A collection of client names and descriptions</returns>
    [Authorize(Role.Administrator, Role.User, Role.ReadOnly)]
    [HttpGet("descriptions")]
    public async Task<IActionResult> GetClientDescriptions()
    {
        var clientDescriptions = await _settingsService.GetClientDescriptions();
        return Ok(clientDescriptions);
    }

    /// <summary>
    ///     Called by the client on startup when retrieving settings
    /// </summary>
    /// <returns>Settings</returns>
    [AllowAnonymous]
    [HttpGet("{clientName}/settings")]
    public async Task<IActionResult> GetSettingsByName(string clientName,
        [FromHeader] string? clientSecret,
        [FromQuery] string? instance,
        [FromQuery] Guid runSessionId)
    {
        if (string.IsNullOrWhiteSpace(clientSecret))
            return Unauthorized();

        var settings = await _settingsService.GetSettings(clientName, clientSecret, instance, runSessionId);
        return Ok(settings);
    }

    [Authorize(Role.Administrator, Role.User, Role.ReadOnly)]
    [HttpGet("{clientName}/settings/{settingName}/history")]
    public async Task<IActionResult> GetSettingHistory(string clientName, string settingName, [FromQuery] string? instance)
    {
        var history = await _settingsService.GetSettingHistory(clientName, settingName, instance);
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
            throw new InvalidClientSecretException(clientSecret);
        
        _clientNameValidator.Validate(settingsClientDefinition.Name);

        await _settingsService.RegisterSettings(clientSecret, settingsClientDefinition);
        return Ok();
    }

    /// <summary>
    ///     Update Settings via web client
    /// </summary>
    [Authorize(Role.Administrator, Role.User)]
    [HttpPut("{clientName}/settings")]
    public async Task<IActionResult> UpdateSettingValues(string clientName,
        [FromQuery] string? instance,
        [FromBody] SettingValueUpdatesDataContract updatedSettings)
    {
        await _settingsService.UpdateSettingValues(clientName, instance,
            updatedSettings);
        return Ok();
    }

    [Authorize(Role.Administrator)]
    [HttpDelete("{clientName}")]
    public async Task<IActionResult> DeleteClient(string clientName,
        [FromQuery] string? instance)
    {
        await _settingsService.DeleteClient(clientName, instance);
        return Ok();
    }

    [Authorize(Role.Administrator)]
    [HttpPut("{clientName}/secret")]
    public async Task<IActionResult> ChangeSecret(string clientName, [FromBody] ClientSecretChangeRequestDataContract changeRequest)
    {
        if (!_clientSecretValidator.IsValid(changeRequest.NewSecret))
            throw new InvalidClientSecretException(changeRequest.NewSecret);
        
        var result = await _settingsService.ChangeClientSecret(clientName, changeRequest);
        return Ok(result);
    }
}