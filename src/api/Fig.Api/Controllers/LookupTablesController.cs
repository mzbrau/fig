using Fig.Api.Attributes;
using Fig.Api.Services;
using Fig.Contracts.Authentication;
using Fig.Contracts.Common;
using Microsoft.AspNetCore.Mvc;

namespace Fig.Api.Controllers;

[ApiController]
[Route("lookuptables")]
public class LookupTablesController : ControllerBase
{
    private readonly ILogger<LookupTablesController> _logger;
    private readonly ILookupTablesService _lookupTablesService;

    public LookupTablesController(ILogger<LookupTablesController> logger, ILookupTablesService lookupTablesService)
    {
        _logger = logger;
        _lookupTablesService = lookupTablesService;
    }

    [Authorize(Role.Administrator, Role.User, Role.LookupService)]
    [HttpGet]
    public IActionResult Get()
    {
        var items = _lookupTablesService.Get();
        return Ok(items);
    }

    [Authorize(Role.Administrator, Role.User, Role.LookupService)]
    [HttpPost]
    public IActionResult Post([FromBody] LookupTableDataContract item)
    {
        _lookupTablesService.Post(item);
        return Ok();
    }

    [Authorize(Role.Administrator, Role.User, Role.LookupService)]
    [HttpPut("{id}")]
    public IActionResult Put(Guid id, [FromBody] LookupTableDataContract item)
    {
        _lookupTablesService.Put(id, item);
        return Ok();
    }

    [Authorize(Role.Administrator, Role.User, Role.LookupService)]
    [HttpDelete("{id}")]
    public IActionResult Delete(Guid id)
    {
        _lookupTablesService.Delete(id);
        return Ok();
    }
}