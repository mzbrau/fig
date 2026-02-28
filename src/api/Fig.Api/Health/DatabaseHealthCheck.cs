using Fig.Api.Datalayer.Repositories;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Fig.Api.Health
{
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly IApiStatusRepository _apiStatusRepository;
        private readonly ILogger<DatabaseHealthCheck> _logger;

        public DatabaseHealthCheck(IApiStatusRepository apiStatusRepository, ILogger<DatabaseHealthCheck> logger)
        {
            _apiStatusRepository = apiStatusRepository;
            _logger = logger;
        }
        
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new())
        {
            try
            {
                var apis = await _apiStatusRepository.GetAllActive();
                return HealthCheckResult.Healthy();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Database health check failed");
                return HealthCheckResult.Unhealthy("Database health check failed.", ex);
            }
        }
    }
}
