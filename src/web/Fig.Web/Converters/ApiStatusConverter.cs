using Fig.Contracts.Status;
using Fig.Web.Models.Api;

namespace Fig.Web.Converters;

public class ApiStatusConverter : IApiStatusConverter
{
    public ApiStatusModel Convert(ApiStatusDataContract status)
    {
        return new ApiStatusModel
        {
            RuntimeId = status.RuntimeId,
            StartTimeUtc = status.StartTimeUtc,
            LastSeen = status.LastSeen,
            IpAddress = status.IpAddress,
            Hostname = status.Hostname,
            MemoryUsageBytes = status.MemoryUsageBytes,
            Version = status.Version
        };
    }
}