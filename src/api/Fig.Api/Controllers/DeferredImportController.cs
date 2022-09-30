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
    public IActionResult GetDeferredImportClients()
    {
        var deferredImports = _importExportService.GetDeferredImportClients();
        return Ok(deferredImports);
    }
}