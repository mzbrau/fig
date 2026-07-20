using Fig.Api.Attributes;
using Fig.Api.Services;
using Fig.Contracts.Authentication;
using Fig.Contracts.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Fig.Api.Controllers;

[ApiController]
[Route("diagnostics")]
public class DiagnosticsController : ControllerBase
{
    private readonly IWebClientLoadTimingService _webClientLoadTimingService;

    public DiagnosticsController(IWebClientLoadTimingService webClientLoadTimingService)
    {
        _webClientLoadTimingService = webClientLoadTimingService;
    }

    /// <summary>
    /// Accepts client-side (Fig.Web) load timings and re-emits them as Fig.Web OpenTelemetry spans.
    /// </summary>
    [Authorize(Role.Administrator, Role.User, Role.ReadOnly)]
    [HttpPost("web-client-load")]
    [SkipTransaction]
    public IActionResult RecordWebClientLoadTiming([FromBody] WebClientLoadTimingDataContract timing)
    {
        if (timing is null)
            return BadRequest();

        _webClientLoadTimingService.RecordClientLoadTiming(timing);
        return NoContent();
    }
}
