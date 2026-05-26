using Fig.Api.Attributes;
using Fig.Api.Services;
using Fig.Contracts.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Fig.Api.Controllers;

[ApiController]
[Route("encryptionmigration")]
public class EncryptionMigrationController : ControllerBase
{
    private readonly IEncryptionMigrationService _encryptionMigrationService;

    public EncryptionMigrationController(IEncryptionMigrationService encryptionMigrationService)
    {
        _encryptionMigrationService = encryptionMigrationService;
    }
    
    [Authorize(Role.Administrator)]
    [HttpGet("status")]
    [SkipTransaction]
    public async Task<IActionResult> GetStatus()
    {
        return Ok(await _encryptionMigrationService.GetStatus());
    }
    
    [Authorize(Role.Administrator)]
    [HttpPut]
    [SkipTransaction]
    public async Task<IActionResult> PerformMigration()
    {
        await _encryptionMigrationService.PerformMigration();
        return Ok();
    }
}