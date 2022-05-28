using Fig.Api.Attributes;
using Fig.Api.Services;
using Fig.Contracts.Authentication;
using Fig.Contracts.Common;
using Microsoft.AspNetCore.Mvc;

namespace Fig.Api.Controllers;

[ApiController]
[Route("commonenumerations")]
public class CommonEnumerationsController : ControllerBase
{
    private readonly ILogger<CommonEnumerationsController> _logger;
    private readonly ICommonEnumerationsService _commonEnumerationsService;

    public CommonEnumerationsController(ILogger<CommonEnumerationsController> logger, ICommonEnumerationsService commonEnumerationsService)
    {
        _logger = logger;
        _commonEnumerationsService = commonEnumerationsService;
    }

    [Authorize(Role.Administrator, Role.User)]
    [HttpGet]
    public IActionResult Get()
    {
        var items = _commonEnumerationsService.Get();
        return Ok(items);
    }

    [Authorize(Role.Administrator, Role.User)]
    [HttpPost]
    public IActionResult Post([FromBody] CommonEnumerationDataContract item)
    {
        _commonEnumerationsService.Post(item);
        return Ok();
    }

    [Authorize(Role.Administrator, Role.User)]
    [HttpPut("{id}")]
    public IActionResult Put(Guid id, [FromBody] CommonEnumerationDataContract item)
    {
        _commonEnumerationsService.Put(id, item);
        return Ok();
    }

    [Authorize(Role.Administrator, Role.User)]
    [HttpDelete("{id}")]
    public IActionResult Delete(Guid id)
    {
        _commonEnumerationsService.Delete(id);
        return Ok();
    }
}