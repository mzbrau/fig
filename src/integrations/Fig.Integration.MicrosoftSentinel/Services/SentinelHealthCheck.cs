using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Fig.Integration.MicrosoftSentinel.Services;

public class SentinelHealthCheck : IHealthCheck
{
    private readonly ISentinelService _sentinelService;
    private readonly ILogger<SentinelHealthCheck> _logger;

    public SentinelHealthCheck(ISentinelService sentinelService, ILogger<SentinelHealthCheck> logger)
    {
        _sentinelService = sentinelService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var isHealthy = await _sentinelService.TestConnectionAsync(cancellationToken);
            
            if (isHealthy)
            {
                return HealthCheckResult.Healthy("Microsoft Sentinel connection is healthy");
            }

            return HealthCheckResult.Unhealthy("Microsoft Sentinel connection failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed for Microsoft Sentinel");
            return HealthCheckResult.Unhealthy("Microsoft Sentinel health check exception", ex);
        }
    }
}