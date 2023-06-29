using Fig.Contracts.Status;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Converters;

public class ApiStatusConverter : IApiStatusConverter
{
    public List<ApiStatusDataContract> Convert(IList<ApiStatusBusinessEntity> statuses)
    {
        return statuses.Select(Convert).ToList();
    }

    private ApiStatusDataContract Convert(ApiStatusBusinessEntity status)
    {
        return new ApiStatusDataContract(status.RuntimeId,
            status.StartTimeUtc,
            status.LastSeen,
            status.IpAddress,
            status.Hostname,
            status.MemoryUsageBytes,
            status.RunningUser,
            status.TotalRequests,
            status.RequestsPerMinute,
            status.Version,
            status.ConfigurationErrorDetected,
            status.NumberOfPluginVerifiers,
            status.PluginVerifiers ?? string.Empty);
    }
}