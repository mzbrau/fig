using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Fig.Contracts.Diagnostics;

public class WebClientLoadTimingDataContract
{
    [JsonConstructor]
    public WebClientLoadTimingDataContract(
        DateTime startedAtUtc,
        long totalDurationMs,
        int clientCount,
        int settingCount,
        IReadOnlyList<WebClientLoadTimingStageDataContract> stages)
    {
        StartedAtUtc = startedAtUtc;
        TotalDurationMs = totalDurationMs;
        ClientCount = clientCount;
        SettingCount = settingCount;
        Stages = stages;
    }

    public DateTime StartedAtUtc { get; }

    public long TotalDurationMs { get; }

    public int ClientCount { get; }

    public int SettingCount { get; }

    public IReadOnlyList<WebClientLoadTimingStageDataContract> Stages { get; }
}
