using Fig.Api.Attributes;
using Fig.Api.DataImport;
using Fig.Api.Services;
using Fig.Contracts.Authentication;
using Fig.Contracts.ImportExport;
using Microsoft.AspNetCore.Mvc;

namespace Fig.Api.Controllers;

[ApiController]
[Route("valueonlydata")]
public class ValueOnlyDataController : ControllerBase
{
    private readonly IImportExportService _importExportService;

    public ValueOnlyDataController(IImportExportService importExportService)
    {
        _importExportService = importExportService;
    }
    
    [Authorize(Role.Administrator)]
    [HttpGet]
    public IActionResult GetValueOnlyExport()
    {
        var export = _importExportService.ValueOnlyExport();
        return Ok(export);
    }
    
    [Authorize(Role.Administrator)]
    [HttpPut]
    public IActionResult SubmitValueOnlyImport([FromBody] FigValueOnlyDataExportDataContract data)
    {
        var result = _importExportService.ValueOnlyImport(data, ImportMode.Api);
        return Ok(result);
    }
}