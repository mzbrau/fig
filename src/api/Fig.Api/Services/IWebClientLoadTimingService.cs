using Fig.Contracts.Diagnostics;

namespace Fig.Api.Services;

public interface IWebClientLoadTimingService
{
    void RecordClientLoadTiming(WebClientLoadTimingDataContract timing);
}
