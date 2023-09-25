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
        
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new())
        {
            try
            {
                var apis = _apiStatusRepository.GetAllActive();
                return Task.FromResult(HealthCheckResult.Healthy());
            }
            catch (Exception e)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(exception: e));
            }
        }
    }
}
