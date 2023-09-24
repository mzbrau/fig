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
    [HttpPut]
    public IActionResult PerformMigration()
    {
        _encryptionMigrationService.PerformMigration();
        return Ok();
    }
}