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
        long? convertDescriptionHtmlMs = null,
        long? httpFetchRequestMs = null,
        long? httpFetchDeserializeMs = null,
        long? httpFetchBodyReadMs = null,
        long? httpFetchParseMs = null,
        long? convertModelBuildMs = null,
        long? initializeSettingsMs = null)
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
        HttpFetchRequestMs = httpFetchRequestMs;
        HttpFetchDeserializeMs = httpFetchDeserializeMs;
        HttpFetchBodyReadMs = httpFetchBodyReadMs;
        HttpFetchParseMs = httpFetchParseMs;
        ConvertModelBuildMs = convertModelBuildMs;
        InitializeSettingsMs = initializeSettingsMs;
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

    /// <summary>
    /// Time until /clients response headers are available (server + network TTFB).
    /// </summary>
    public long? HttpFetchRequestMs { get; }

    /// <summary>
    /// Time spent reading and deserializing the /clients response body (body read + parse).
    /// </summary>
    public long? HttpFetchDeserializeMs { get; }

    /// <summary>
    /// Time spent copying the /clients response body into memory (JS-interop / transfer).
    /// </summary>
    public long? HttpFetchBodyReadMs { get; }

    /// <summary>
    /// Time spent deserializing the buffered /clients body with Newtonsoft.
    /// </summary>
    public long? HttpFetchParseMs { get; }

    /// <summary>
    /// Pure model construction time inside ConvertToModels (excludes await Task.Yield paint overhead).
    /// </summary>
    public long? ConvertModelBuildMs { get; }

    /// <summary>
    /// Time spent in client.InitializeAsync() during InitializeModels (excludes ordering / searchable list).
    /// </summary>
    public long? InitializeSettingsMs { get; }
}
