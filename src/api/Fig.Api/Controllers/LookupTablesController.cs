using Fig.Api.Attributes;
using Fig.Api.Services;
using Fig.Contracts.Authentication;
using Fig.Contracts.LookupTable;
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

    [Authorize(Role.Administrator, Role.User, Role.LookupService, Role.ReadOnly)]
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var items = await _lookupTablesService.Get();
        return Ok(items);
    }

    [Authorize(Role.Administrator, Role.User, Role.LookupService)]
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] LookupTableDataContract item)
    {
        await _lookupTablesService.Post(item);
        return Ok();
    }

    [Authorize(Role.Administrator, Role.User, Role.LookupService)]
    [HttpPut("{id}")]
    public async Task<IActionResult> Put(Guid id, [FromBody] LookupTableDataContract item)
    {
        await _lookupTablesService.Put(id, item);
        return Ok();
    }

    [Authorize(Role.Administrator, Role.User, Role.LookupService)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _lookupTablesService.Delete(id);
        return Ok();
    }
}