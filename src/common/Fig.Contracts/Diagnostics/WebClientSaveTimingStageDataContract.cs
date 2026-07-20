using Newtonsoft.Json;

namespace Fig.Contracts.Diagnostics;

public class WebClientSaveTimingStageDataContract
{
    [JsonConstructor]
    public WebClientSaveTimingStageDataContract(string name, long durationMs)
    {
        Name = name;
        DurationMs = durationMs;
    }

    public string Name { get; }

    public long DurationMs { get; }
}
