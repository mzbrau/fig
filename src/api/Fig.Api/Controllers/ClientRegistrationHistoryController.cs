using Fig.Api.Attributes;
using Fig.Api.Services;
using Fig.Contracts.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Fig.Api.Controllers;

[ApiController]
[Route("clientregistrationhistory")]
public class ClientRegistrationHistoryController : ControllerBase
{
    private readonly IClientRegistrationHistoryService _historyService;
    private readonly ILogger<ClientRegistrationHistoryController> _logger;

    public ClientRegistrationHistoryController(
        IClientRegistrationHistoryService historyService,
        ILogger<ClientRegistrationHistoryController> logger)
    {
        _historyService = historyService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all client registration history records.
    /// </summary>
    /// <returns>A collection of all registration history records.</returns>
    [Authorize(Role.Administrator)]
    [HttpGet]
    public async Task<IActionResult> GetAllHistory()
    {
        var history = await _historyService.GetAllHistory();
        return Ok(history);
    }

    /// <summary>
    /// Clears all client registration history records.
    /// Primarily used for integration testing.
    /// </summary>
    [Authorize(Role.Administrator)]
    [HttpDelete]
    public async Task<IActionResult> ClearHistory()
    {
        await _historyService.ClearHistory();
        return Ok();
    }
}
