using Fig.Api.Attributes;
using Fig.Api.DataImport;
using Fig.Api.Services;
using Fig.Common.NetStandard.Json;
using Fig.Contracts.Authentication;
using Fig.Contracts.ImportExport;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

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
    public async Task<IActionResult> GetValueOnlyExport([FromQuery] bool excludeEnvironmentSpecific = false, [FromQuery] bool includeLastChanged = false)
    {
        var export = await _importExportService.ValueOnlyExport(excludeEnvironmentSpecific, includeLastChanged);
        var json = JsonConvert.SerializeObject(export, JsonSettings.FigMinimalUserFacing);
        return Content(json, "application/json");
    }
    
    [Authorize(Role.Administrator)]
    [HttpPut]
    public async Task<IActionResult> SubmitValueOnlyImport([FromBody] FigValueOnlyDataExportDataContract? data)
    {
        var result = await _importExportService.ValueOnlyImport(data, ImportMode.Api);
        return Ok(result);
    }
}