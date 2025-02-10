using Fig.Api.Attributes;
using Fig.Api.Services;
using Fig.Contracts.Authentication;
using Fig.Contracts.WebHook;
using Microsoft.AspNetCore.Mvc;

namespace Fig.Api.Controllers;

[ApiController]
[Route("webhookclient")]
public class WebHookClientController : ControllerBase
{
    private readonly IWebHookService _webHookService;

    public WebHookClientController(IWebHookService webHookService)
    {
        _webHookService = webHookService;
    }
    
    [Authorize(Role.Administrator)]
    [HttpGet]
    public async Task<IActionResult> GetClients()
    {
        var clients = await _webHookService.GetClients();
        return Ok(clients);
    }

    [Authorize(Role.Administrator)]
    [HttpPost]
    public async Task<IActionResult> AddClient([FromBody] WebHookClientDataContract data)
    {
        var client = await _webHookService.AddClient(data);
        return Ok(client);
    }
    
    [Authorize(Role.Administrator)]
    [HttpPut("{clientId}")]
    public async Task<IActionResult> UpdateClient([FromRoute]Guid clientId, [FromBody] WebHookClientDataContract data)
    {
        var client = await _webHookService.UpdateClient(clientId, data);
        return Ok(client);
    }

    [Authorize(Role.Administrator)]
    [HttpDelete("{clientId}")]
    public async Task<IActionResult> DeleteClient([FromRoute]Guid clientId)
    {
        await _webHookService.DeleteClient(clientId);
        return Ok();
    }

    [Authorize(Role.Administrator)]
    [HttpPut("{clientId}/test")]
    public async Task<IActionResult> TestClient([FromRoute] Guid clientId)
    {
        var result = await _webHookService.TestClient(clientId);
        return Ok(result);
    }
}