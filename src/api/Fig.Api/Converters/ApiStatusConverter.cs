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
        return new ApiStatusDataContract
        {
            RuntimeId = status.RuntimeId,
            StartTimeUtc = status.StartTimeUtc,
            LastSeen = status.LastSeen,
            IpAddress = status.IpAddress,
            Hostname = status.Hostname,
            MemoryUsageBytes = status.MemoryUsageBytes,
            RunningUser = status.RunningUser,
            TotalRequests = status.TotalRequests,
            RequestsPerMinute = status.RequestsPerMinute,
            Version = status.Version
        };
    }
}