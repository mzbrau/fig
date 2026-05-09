using Fig.Common;
using Fig.Contracts.Capabilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Fig.Api.Datalayer.Repositories;

namespace Fig.Api.Controllers;

[ApiController, Route("capabilities")]
public class CapabilitiesController : ControllerBase
{
    private static readonly string[] BaseSupportedFeatures =
    [
        "deferredDescriptionRegistration",
        "requestCompression",
        "clientSettingUpdates"
    ];

    private const string CacheKeyPrefix = "fig_capabilities";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    private readonly IVersionHelper _versionHelper;
    private readonly IMemoryCache _memoryCache;
    private readonly IConfigurationRepository _configurationRepository;

    public CapabilitiesController(
        IVersionHelper versionHelper,
        IMemoryCache memoryCache,
        IConfigurationRepository configurationRepository)
    {
        _versionHelper = versionHelper;
        _memoryCache = memoryCache;
        _configurationRepository = configurationRepository;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetCapabilities()
    {
        var allowMigrateFromMigrations = (await _configurationRepository.GetConfiguration()).AllowMigrateFromMigrations;
        var result = _memoryCache.GetOrCreate(GetCacheKey(allowMigrateFromMigrations), entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return new FigCapabilitiesDataContract(
                _versionHelper.GetVersion(),
                GetSupportedFeatures(allowMigrateFromMigrations));
        });

        return Ok(result);
    }

    private static string GetCacheKey(bool allowMigrateFromMigrations) =>
        $"{CacheKeyPrefix}:{allowMigrateFromMigrations}";

    private static IReadOnlyList<string> GetSupportedFeatures(bool allowMigrateFromMigrations) =>
        allowMigrateFromMigrations
            ? [.. BaseSupportedFeatures, "migrateFromClientTransforms"]
            : BaseSupportedFeatures;
}
