using Fig.Api.Attributes;
using Fig.Api.Services;
using Fig.Contracts.Authentication;
using Fig.Contracts.ReleaseHighlights;
using Microsoft.AspNetCore.Mvc;

namespace Fig.Api.Controllers;

[ApiController]
[Route("releasehighlights")]
public class ReleaseHighlightsController : ControllerBase
{
    private readonly IReleaseHighlightsService _releaseHighlightsService;

    public ReleaseHighlightsController(IReleaseHighlightsService releaseHighlightsService)
    {
        _releaseHighlightsService = releaseHighlightsService;
    }

    [Authorize(Role.Administrator)]
    [HttpGet]
    public async Task<IActionResult> GetProgress()
    {
        return Ok(await _releaseHighlightsService.GetProgress());
    }

    [Authorize(Role.Administrator)]
    [HttpPost("viewed")]
    public async Task<IActionResult> RecordViewed([FromBody] ReleaseHighlightViewedDataContract viewedHighlight)
    {
        if (viewedHighlight == null)
            return BadRequest("Request body is required.");
        return Ok(await _releaseHighlightsService.RecordViewed(viewedHighlight));
    }
}
