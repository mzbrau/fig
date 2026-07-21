using Fig.Contracts.Diagnostics;

namespace Fig.Api.Services;

public interface IWebClientSaveTimingService
{
    void RecordClientSaveTiming(WebClientSaveTimingDataContract timing);
}
