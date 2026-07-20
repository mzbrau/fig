using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Fig.Contracts.Diagnostics;

public class WebClientSaveTimingDataContract
{
    [JsonConstructor]
    public WebClientSaveTimingDataContract(
        DateTime startedAtUtc,
        long totalDurationMs,
        int clientCount,
        int dirtyClientCount,
        int settingChangeCount,
        int httpPutCount,
        bool isSaveAll,
        IReadOnlyList<WebClientSaveTimingStageDataContract> stages)
    {
        StartedAtUtc = startedAtUtc;
        TotalDurationMs = totalDurationMs;
        ClientCount = clientCount;
        DirtyClientCount = dirtyClientCount;
        SettingChangeCount = settingChangeCount;
        HttpPutCount = httpPutCount;
        IsSaveAll = isSaveAll;
        Stages = stages;
    }

    public DateTime StartedAtUtc { get; }

    public long TotalDurationMs { get; }

    /// <summary>
    /// Number of clients considered for the save batch (e.g. selected or all non-group).
    /// </summary>
    public int ClientCount { get; }

    /// <summary>
    /// Number of clients that had at least one dirty setting saved.
    /// </summary>
    public int DirtyClientCount { get; }

    /// <summary>
    /// Total number of setting values included in PUT payloads.
    /// </summary>
    public int SettingChangeCount { get; }

    /// <summary>
    /// Number of HTTP PUT requests issued during the save batch.
    /// </summary>
    public int HttpPutCount { get; }

    public bool IsSaveAll { get; }

    public IReadOnlyList<WebClientSaveTimingStageDataContract> Stages { get; }
}
