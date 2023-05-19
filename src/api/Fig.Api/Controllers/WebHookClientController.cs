using Fig.Api.Attributes;
using Fig.Api.Services;
using Fig.Common.NetStandard.WebHook;
using Fig.Contracts.Authentication;
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
    public IActionResult GetClients()
    {
        var clients = _webHookService.GetClients();
        return Ok(clients);
    }

    [Authorize(Role.Administrator)]
    [HttpPost]
    public IActionResult AddClient([FromBody] WebHookClientDataContract data)
    {
        var client = _webHookService.AddClient(data);
        return Ok(client);
    }
    
    [Authorize(Role.Administrator)]
    [HttpPut("{clientId}")]
    public IActionResult UpdateClient([FromRoute]Guid clientId, [FromBody] WebHookClientDataContract data)
    {
        var client = _webHookService.UpdateClient(clientId, data);
        return Ok(client);
    }

    [Authorize(Role.Administrator)]
    [HttpDelete("{clientId}")]
    public IActionResult DeleteClient([FromRoute]Guid clientId)
    {
        _webHookService.DeleteClient(clientId);
        return Ok();
    }
}