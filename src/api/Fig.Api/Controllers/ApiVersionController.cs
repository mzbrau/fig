using Fig.Api.Services;
using Fig.Common;
using Fig.Contracts.Status;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fig.Api.Controllers;

[ApiController, Route("apiversion")]
public class ApiVersionController : ControllerBase
{
    private readonly IVersionHelper _versionHelper;
    private readonly ISettingsService _settingsService;

    public ApiVersionController(IVersionHelper versionHelper, ISettingsService settingsService)
    {
        _versionHelper = versionHelper;
        _settingsService = settingsService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetVersion()
    {
        var lastUpdate = await _settingsService.GetLastSettingUpdate();
        var version = _versionHelper.GetVersion();
        return Ok(new ApiVersionDataContract(version, Environment.MachineName, lastUpdate));
    }
}