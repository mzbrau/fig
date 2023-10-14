using Fig.Api.Attributes;
using Fig.Api.Services;
using Fig.Contracts.Authentication;
using Fig.Contracts.Configuration;
using Microsoft.AspNetCore.Mvc;

namespace Fig.Api.Controllers;

[ApiController]
[Route("configuration")]
public class ConfigurationController : ControllerBase
{
    private readonly IConfigurationService _configurationService;

    public ConfigurationController(IConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }

    [Authorize(Role.Administrator)]
    [HttpGet]
    public IActionResult GetConfiguration()
    {
        var config = _configurationService.GetConfiguration();
        return Ok(config);
    }

    [Authorize(Role.Administrator)]
    [HttpPut]
    public IActionResult UpdateConfiguration([FromBody] FigConfigurationDataContract config)
    {
        _configurationService.UpdateConfiguration(config);
        return Ok();
    }

    [Authorize(Role.Administrator)]
    [HttpPut("KeyVault")]
    public async Task<IActionResult> TestAzureKeyVault()
    {
        var result = await _configurationService.TestAzureKeyVault();
        return Ok(result);
    }
}