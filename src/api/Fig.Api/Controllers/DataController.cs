using Fig.Api.Attributes;
using Fig.Api.DataImport;
using Fig.Api.Services;
using Fig.Contracts.Authentication;
using Fig.Contracts.ImportExport;
using Microsoft.AspNetCore.Mvc;

namespace Fig.Api.Controllers;

[ApiController]
[Route("data")]
public class DataController : ControllerBase
{
    private readonly IEncryptionService _encryptionService;
    private readonly IImportExportService _importExportService;

    public DataController(IImportExportService importExportService, IEncryptionService encryptionService)
    {
        _importExportService = importExportService;
        _encryptionService = encryptionService;
    }

    [Authorize(Role.Administrator)]
    [HttpGet]
    public IActionResult GetExport([FromQuery] bool decryptSecrets)
    {
        var export = _importExportService.Export(decryptSecrets);
        return Ok(export);
    }

    [Authorize(Role.Administrator)]
    [HttpPut]
    public async Task<IActionResult> SubmitImport([FromBody] FigDataExportDataContract data)
    {
        var result = await _importExportService.Import(data, ImportMode.Api);
        return Ok(result);
    }

    [Authorize(Role.Administrator)]
    [HttpGet("certificates")]
    public IActionResult GetAllCertificates()
    {
        var certificates = _encryptionService.GetAllCertificatesInStore();
        return Ok(certificates);
    }

    [Authorize(Role.Administrator)]
    [HttpGet("certificates/{thumbprint}")]
    public IActionResult GetCertificate(string thumbprint)
    {
        var certificate = _encryptionService.GetCertificate(thumbprint);
        if (certificate == null)
            return NotFound();

        return Ok(certificate);
    }

    [Authorize(Role.Administrator)]
    [HttpPut("certificates")]
    public IActionResult GenerateNewCertificate()
    {
        var certificate = _encryptionService.CreateCertificate();
        return Ok(certificate);
    }
}