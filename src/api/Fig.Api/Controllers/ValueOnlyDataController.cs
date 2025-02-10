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
    public async Task<IActionResult> GetValueOnlyExport()
    {
        var export = await _importExportService.ValueOnlyExport();
        return Ok(export);
    }
    
    [Authorize(Role.Administrator)]
    [HttpPut]
    public async Task<IActionResult> SubmitValueOnlyImport([FromBody] FigValueOnlyDataExportDataContract? data)
    {
        var result = await _importExportService.ValueOnlyImport(data, ImportMode.Api);
        return Ok(result);
    }
}