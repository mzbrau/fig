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
    public async Task<IActionResult> GetWebHooks()
    {
        var webHooks = await _webHookService.GetWebHooks();
        return Ok(webHooks);
    }

    [Authorize(Role.Administrator)]
    [HttpPost]
    public async Task<IActionResult> AddWebHook([FromBody] WebHookDataContract webHook)
    {
        var result = await _webHookService.AddWebHook(webHook);
        return Ok(result);
    }
    
    [Authorize(Role.Administrator)]
    [HttpPut("{webHookId}")]
    public async Task<IActionResult> UpdateWebHook([FromRoute]Guid webHookId, [FromBody] WebHookDataContract webHook)
    {
        var result = await _webHookService.UpdateWebHook(webHookId, webHook);
        return Ok(result);
    }

    [Authorize(Role.Administrator)]
    [HttpDelete("{webHookId}")]
    public async Task<IActionResult> DeleteWebHook([FromRoute]Guid webHookId)
    {
        await _webHookService.DeleteWebHook(webHookId);
        return Ok();
    }
}