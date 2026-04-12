using Fig.Api.Attributes;
using Fig.Api.Services;
using Fig.Contracts.Authentication;
using Fig.Contracts.ImportExport;
using Fig.Contracts.SettingGroups;
using Microsoft.AspNetCore.Mvc;

namespace Fig.Api.Controllers;

[ApiController]
[Route("settinggroupdata")]
public class SettingGroupDataController : ControllerBase
{
    private readonly ILogger<SettingGroupDataController> _logger;
    private readonly IGroupImportExportService _groupImportExportService;

    public SettingGroupDataController(ILogger<SettingGroupDataController> logger, 
        IGroupImportExportService groupImportExportService)
    {
        _logger = logger;
        _groupImportExportService = groupImportExportService;
    }

    [Authorize(Role.Administrator)]
    [HttpGet]
    public async Task<IActionResult> ExportGroups()
    {
        var data = await _groupImportExportService.ExportGroups();
        return Ok(data);
    }

    [Authorize(Role.Administrator)]
    [HttpPut]
    public async Task<IActionResult> ImportGroups([FromBody] SettingGroupExportDataContract data, 
        [FromQuery] ImportType importType = ImportType.AddNew)
    {
        if (importType is not (ImportType.ClearAndImport or ImportType.AddNew or ImportType.ReplaceExisting))
            return BadRequest($"Import type '{importType}' is not supported for setting groups.");

        var result = await _groupImportExportService.ImportGroups(data, importType);
        return Ok(result);
    }
}
