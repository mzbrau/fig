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
        IReadOnlyList<WebClientLoadTimingStageDataContract> stages,
        int? descriptionClientCount = null,
        long? descriptionResponseChars = null,
        long? settingGroupsHttpMs = null,
        long? convertDescriptionHtmlMs = null)
    {
        StartedAtUtc = startedAtUtc;
        TotalDurationMs = totalDurationMs;
        ClientCount = clientCount;
        SettingCount = settingCount;
        Stages = stages;
        DescriptionClientCount = descriptionClientCount;
        DescriptionResponseChars = descriptionResponseChars;
        SettingGroupsHttpMs = settingGroupsHttpMs;
        ConvertDescriptionHtmlMs = convertDescriptionHtmlMs;
    }

    public DateTime StartedAtUtc { get; }

    public long TotalDurationMs { get; }

    public int ClientCount { get; }

    public int SettingCount { get; }

    public IReadOnlyList<WebClientLoadTimingStageDataContract> Stages { get; }

    public int? DescriptionClientCount { get; }

    public long? DescriptionResponseChars { get; }

    /// <summary>
    /// Full HTTP duration for GET settinggroups (may overlap HttpFetchClients / ConvertToModels).
    /// </summary>
    public long? SettingGroupsHttpMs { get; }

    /// <summary>
    /// Time spent converting setting description markdown to HTML during ConvertToModels (0 when deferred).
    /// </summary>
    public long? ConvertDescriptionHtmlMs { get; }
}
