using Fig.Api.Attributes;
using Fig.Api.Services;
using Fig.Contracts.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Fig.Api.Controllers;

[ApiController]
[Route("deferredimport")]
public class DeferredImportController : ControllerBase
{
    private readonly IImportExportService _importExportService;

    public DeferredImportController(IImportExportService importExportService)
    {
        _importExportService = importExportService;
    }
    
    [Authorize(Role.Administrator)]
    [HttpGet]
    public async Task<IActionResult> GetDeferredImportClients()
    {
        var deferredImports = await _importExportService.GetDeferredImportClients();
        return Ok(deferredImports);
    }
}