using Fig.Api.Utils;
using Fig.Common;
using Fig.Contracts.Status;
using Microsoft.AspNetCore.Mvc;

namespace Fig.Api.Controllers;

[ApiController, Route("apiversion")]
public class ApiVersionController : ControllerBase
{
    private readonly IVersionHelper _versionHelper;

    public ApiVersionController(IVersionHelper versionHelper)
    {
        _versionHelper = versionHelper;
    }

    [HttpGet]
    public IActionResult GetVersion()
    {
        var version = _versionHelper.GetVersion();
        return Ok(new ApiVersionDataContract(version));
    }
}