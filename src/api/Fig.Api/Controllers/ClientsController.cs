using Fig.Api.Attributes;
using Fig.Api.Exceptions;
using Fig.Api.Services;
using Fig.Api.Validators;
using Fig.Common.NetStandard.Json;
using Fig.Contracts.Constants;
using Fig.Common.NetStandard.Validation;
using Fig.Contracts.Authentication;
using Fig.Contracts.Diagnostics;
using Fig.Contracts.Json;
using Fig.Contracts.SettingClients;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;
namespace Fig.Api.Controllers;

[ApiController]
[Route("clients")]
public class ClientsController : ControllerBase
{
    private const int MaxClientLoadFailureHeaderBytes = 4096;
    private const int MaxClientLoadFailureItems = 20;
    private const int MaxClientLoadFailureMessageChars = 200;
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
    ///     Serialized with <see cref="FigWebLoadJsonSettings"/> (compact polymorphic values, no $type)
    ///     to cut Blazor WASM parse cost. Fig.Client does not use this endpoint.
    /// </summary>
    /// <returns>A collection of all registered clients and their setting definitions</returns>
    [Authorize(Role.Administrator, Role.User, Role.ReadOnly)]
    [HttpGet]
    [SkipTransaction]
    public async Task<IActionResult> GetAllClients()
    {
        var result = await _settingsService.GetAllClients();
        AddLoadFailureHeader(result.Failures);

        // A/B: X-Fig-Load-Perf compactClientsJson=0 → FigHttp ($type) instead of FigWebLoad.
        var loadPerf = LoadPerfFlags.Parse(
            Request.Headers.TryGetValue(FigHttpHeaders.LoadPerf, out var headerValues)
                ? headerValues.FirstOrDefault()
                : null);
        var settings = loadPerf.CompactClientsJson
            ? FigWebLoadJsonSettings.Instance
            : JsonSettings.FigHttp;
        Activity.Current?.SetTag("fig.web.load_perf_flags", loadPerf.ToHeaderValue());
        Activity.Current?.SetTag("fig.api.clients_compact_json", loadPerf.CompactClientsJson);

        var json = JsonConvert.SerializeObject(result.Clients, settings);
        return Content(json, "application/json");
    }

    private void AddLoadFailureHeader(IList<ClientLoadFailureDataContract> failures)
    {
        if (!failures.Any())
            return;

        Response.Headers[FigHttpHeaders.ClientLoadFailures] = BuildLoadFailureHeader(failures);
    }

    private static string BuildLoadFailureHeader(IList<ClientLoadFailureDataContract> failures)
    {
        var payloadFailures = new List<ClientLoadFailureDataContract>();
        var encoded = EncodeLoadFailureSummary(new ClientLoadFailureSummaryDataContract(failures.Count, payloadFailures, true));

        foreach (var failure in failures.Take(MaxClientLoadFailureItems))
        {
            var candidateFailures = payloadFailures
                .Append(TrimFailureMessage(failure))
                .ToList();
            var truncated = candidateFailures.Count < failures.Count;
            var candidate = EncodeLoadFailureSummary(new ClientLoadFailureSummaryDataContract(failures.Count, candidateFailures, truncated));
            if (Encoding.ASCII.GetByteCount(candidate) > MaxClientLoadFailureHeaderBytes)
                break;

            payloadFailures = candidateFailures;
            encoded = candidate;
        }

        return encoded;
    }

    private static string EncodeLoadFailureSummary(ClientLoadFailureSummaryDataContract summary)
    {
        var json = JsonConvert.SerializeObject(summary, JsonSettings.FigDefault);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    private static ClientLoadFailureDataContract TrimFailureMessage(ClientLoadFailureDataContract failure)
    {
        var message = failure.Message ?? string.Empty;
        if (message.Length > MaxClientLoadFailureMessageChars)
            message = message[..MaxClientLoadFailureMessageChars];

        return new ClientLoadFailureDataContract(
            failure.ClientName,
            failure.Instance,
            failure.SettingName,
            message);
    }

    /// <summary>
    ///     Called by the web client to get just the names and descriptions of all clients.
    /// </summary>
    /// <returns>A collection of client names and descriptions</returns>
    [Authorize(Role.Administrator, Role.User, Role.ReadOnly)]
    [HttpGet("descriptions")]
    [SkipTransaction]
    public async Task<IActionResult> GetClientDescriptions()
    {
        var clientDescriptions = await _settingsService.GetClientDescriptions();
        return Ok(clientDescriptions);
    }

    /// <summary>
    ///     Called by the client on startup when retrieving settings
    /// </summary>
    /// <returns>Settings</returns>
    [LogFigClientCall]
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

    [Authorize(Role.Administrator, Role.User, Role.ReadOnly)]
    [HttpGet("settings/lastchanged")]
    public async Task<IActionResult> GetLastChangedForAllClientsSettings()
    {
        var lastChanged = await _settingsService.GetLastChangedForAllClientsAndSettings();
        return Ok(lastChanged);
    }

    /// <summary>
    ///     Called by the client when it registers its settings.
    /// </summary>
    /// <param name="clientSecret"></param>
    /// <param name="settingsClientDefinition">The settings to be registered.</param>
    /// <returns>An id for callback.</returns>
    [LogFigClientCall]
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

    [LogFigClientCall]
    [AllowAnonymous]
    [HttpPost("migrations/preview")]
    public async Task<IActionResult> PreviewMigrateFromMigrations([FromHeader] string clientSecret,
        [FromBody] SettingsClientDefinitionDataContract settingsClientDefinition)
    {
        if (!_clientSecretValidator.IsValid(clientSecret))
            throw new InvalidClientSecretException(clientSecret);

        _clientNameValidator.Validate(settingsClientDefinition.Name);

        var requests = await _settingsService.GetMigrateFromMigrationRequests(clientSecret, settingsClientDefinition);
        return Ok(requests);
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

    /// <summary>
    ///     Update settings from the registered application using its client secret.
    /// </summary>
    [LogFigClientCall]
    [AllowAnonymous]
    [HttpPut("{clientName}/settings/self")]
    public async Task<IActionResult> UpdateSettingValuesFromClient(string clientName,
        [FromHeader] string? clientSecret,
        [FromQuery] string? instance,
        [FromBody] SettingValueUpdatesDataContract updatedSettings)
    {
        if (string.IsNullOrWhiteSpace(clientSecret))
            return Unauthorized();

        if (!_clientSecretValidator.IsValid(clientSecret))
            throw new InvalidClientSecretException(clientSecret);

        await _settingsService.UpdateSettingValuesFromClient(clientName, instance, clientSecret, updatedSettings);
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

    /// <summary>
    ///     Called by the client after registration to update the description without
    ///     repeating the full payload (deferred description registration).
    /// </summary>
    [LogFigClientCall]
    [AllowAnonymous]
    [HttpPut("{clientName}/description")]
    public async Task<IActionResult> UpdateClientDescription(string clientName,
        [FromHeader] string clientSecret,
        [FromQuery] string? instance,
        [FromBody] ClientDescriptionUpdateDataContract update)
    {
        if (!_clientSecretValidator.IsValid(clientSecret))
            throw new InvalidClientSecretException(clientSecret);

        await _settingsService.UpdateClientDescription(clientName, instance, clientSecret, update.Description);
        return Ok();
    }
}
