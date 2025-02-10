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
    public async Task<IActionResult> GetConfiguration()
    {
        var config = await _configurationService.GetConfiguration();
        return Ok(config);
    }

    [Authorize(Role.Administrator)]
    [HttpPut]
    public async Task<IActionResult> UpdateConfiguration([FromBody] FigConfigurationDataContract config)
    {
        await _configurationService.UpdateConfiguration(config);
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