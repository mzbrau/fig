using Fig.Api.Attributes;
using Fig.Api.Services;
using Fig.Contracts.Authentication;
using Fig.Contracts.WebHook;
using Microsoft.AspNetCore.Mvc;

namespace Fig.Api.Controllers;

[ApiController]
[Route("webhooks")]
public class WebHooksController : ControllerBase
{
    private readonly IWebHookService _webHookService;

    public WebHooksController(IWebHookService webHookService)
    {
        _webHookService = webHookService;
    }
    
    [Authorize(Role.Administrator)]
    [HttpGet]
    public IActionResult GetWebHooks()
    {
        var webHooks = _webHookService.GetWebHooks();
        return Ok(webHooks);
    }

    [Authorize(Role.Administrator)]
    [HttpPost]
    public IActionResult AddWebHook([FromBody] WebHookDataContract webHook)
    {
        var result = _webHookService.AddWebHook(webHook);
        return Ok(result);
    }
    
    [Authorize(Role.Administrator)]
    [HttpPut("{webHookId}")]
    public IActionResult UpdateWebHook([FromRoute]Guid webHookId, [FromBody] WebHookDataContract webHook)
    {
        var result = _webHookService.UpdateWebHook(webHookId, webHook);
        return Ok(result);
    }

    [Authorize(Role.Administrator)]
    [HttpDelete("{webHookId}")]
    public IActionResult DeleteWebHook([FromRoute]Guid webHookId)
    {
        _webHookService.DeleteWebHook(webHookId);
        return Ok();
    }
}