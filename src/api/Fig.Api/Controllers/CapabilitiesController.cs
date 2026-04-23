using Fig.Common;
using Fig.Contracts.Capabilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Fig.Api.Controllers;

[ApiController, Route("capabilities")]
public class CapabilitiesController : ControllerBase
{
    private static readonly string[] SupportedFeatures =
    [
        "deferredDescriptionRegistration",
        "requestCompression"
    ];

    private const string CacheKey = "fig_capabilities";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly IVersionHelper _versionHelper;
    private readonly IMemoryCache _memoryCache;

    public CapabilitiesController(IVersionHelper versionHelper, IMemoryCache memoryCache)
    {
        _versionHelper = versionHelper;
        _memoryCache = memoryCache;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult GetCapabilities()
    {
        var result = _memoryCache.GetOrCreate(CacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return new FigCapabilitiesDataContract(_versionHelper.GetVersion(), SupportedFeatures);
        });

        return Ok(result);
    }
}
