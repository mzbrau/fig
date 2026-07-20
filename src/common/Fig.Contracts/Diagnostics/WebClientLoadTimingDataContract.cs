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
        long? initializeSettingsMs = null,
        int? displayScriptsExecuted = null,
        int? displayScriptsSucceeded = null,
        int? displayScriptsFailed = null,
        int? displayScriptsSkipped = null,
        long? initializeScriptsMs = null,
        long? initializeOtherMs = null,
        IReadOnlyList<string>? displayScriptFailures = null)
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
        DisplayScriptsExecuted = displayScriptsExecuted;
        DisplayScriptsSucceeded = displayScriptsSucceeded;
        DisplayScriptsFailed = displayScriptsFailed;
        DisplayScriptsSkipped = displayScriptsSkipped;
        InitializeScriptsMs = initializeScriptsMs;
        InitializeOtherMs = initializeOtherMs;
        DisplayScriptFailures = displayScriptFailures;
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
    /// Total time spent in client.InitializeAsync() after first paint (scripts + validation + enabled walks).
    /// </summary>
    public long? InitializeSettingsMs { get; }

    /// <summary>
    /// Subset of <see cref="InitializeSettingsMs"/> spent running display scripts (shared-engine batch).
    /// </summary>
    public long? InitializeScriptsMs { get; }

    /// <summary>
    /// Subset of <see cref="InitializeSettingsMs"/> spent on non-script work (enabled status, validation).
    /// </summary>
    public long? InitializeOtherMs { get; }

    /// <summary>
    /// Total display-script completions reported during InitializeModels (includes succeeded/failed/skipped).
    /// </summary>
    public int? DisplayScriptsExecuted { get; }

    public int? DisplayScriptsSucceeded { get; }

    public int? DisplayScriptsFailed { get; }

    /// <summary>
    /// Scripts not run (empty script or infinite-loop guard). Should be 0 for healthy initial loads.
    /// </summary>
    public int? DisplayScriptsSkipped { get; }

    /// <summary>
    /// Per-failure summaries ("ClientName/SettingName: error") when display scripts fail during load.
    /// </summary>
    public IReadOnlyList<string>? DisplayScriptFailures { get; }
}
