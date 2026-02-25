using Fig.Api.Datalayer.Repositories;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Fig.Api.Health
{
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly IApiStatusRepository _apiStatusRepository;

        public DatabaseHealthCheck(IApiStatusRepository apiStatusRepository)
        {
            _apiStatusRepository = apiStatusRepository;
        }
        
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new())
        {
            try
            {
                var apis = await _apiStatusRepository.GetAllActive();
                return HealthCheckResult.Healthy();
            }
            catch (Exception)
            {
                return HealthCheckResult.Unhealthy("Database health check failed.");
            }
        }
    }
}
