using System.Diagnostics;
using Fig.Api.Observability;
using Fig.Contracts.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Fig.Api.Services;

public class WebClientLoadTimingService : IWebClientLoadTimingService
{
    private readonly ILogger<WebClientLoadTimingService> _logger;

    public WebClientLoadTimingService(ILogger<WebClientLoadTimingService> logger)
    {
        _logger = logger;
    }

    public void RecordClientLoadTiming(WebClientLoadTimingDataContract timing)
    {
        if (timing.Stages.Count == 0 && timing.TotalDurationMs <= 0)
        {
            _logger.LogDebug("Ignoring empty web client load timing report");
            return;
        }

        var startedAt = timing.StartedAtUtc.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(timing.StartedAtUtc, DateTimeKind.Utc)
            : timing.StartedAtUtc.ToUniversalTime();
        var startedAtOffset = new DateTimeOffset(startedAt);

        var totalDuration = TimeSpan.FromMilliseconds(Math.Max(0, timing.TotalDurationMs));
        var endedAt = startedAt + totalDuration;

        // Force a true root span. parentContext: default still parents to Activity.Current
        // (the ASP.NET POST), which buries reconstructed Web timings under a short request.
        var previous = Activity.Current;
        Activity.Current = null;
        try
        {
            IEnumerable<ActivityLink>? links = previous is null
                ? null
                : [new ActivityLink(previous.Context)];

            using var parent = WebClientActivitySource.Instance.StartActivity(
                "Web.SettingsClientLoad",
                ActivityKind.Internal,
                parentContext: default,
                tags: null,
                links: links,
                startTime: startedAtOffset);

            if (parent is null)
            {
                _logger.LogDebug(
                    "Fig.Web activity source has no listeners; skipped recording web client load timing " +
                    "(clients={ClientCount}, settings={SettingCount}, totalMs={TotalMs})",
                    timing.ClientCount,
                    timing.SettingCount,
                    timing.TotalDurationMs);
                return;
            }

            SetWebOriginTags(parent, timing);

            var stageCursor = startedAt;
            foreach (var stage in timing.Stages)
            {
                var stageDuration = TimeSpan.FromMilliseconds(Math.Max(0, stage.DurationMs));
                var stageEnd = stageCursor + stageDuration;
                var stageName = string.IsNullOrWhiteSpace(stage.Name) ? "Unknown" : stage.Name;

                using (var child = WebClientActivitySource.Instance.StartActivity(
                           $"Web.{stageName}",
                           ActivityKind.Internal,
                           parent.Context,
                           tags: null,
                           links: null,
                           startTime: new DateTimeOffset(stageCursor)))
                {
                    if (child is not null)
                    {
                        child.SetTag("fig.telemetry.origin", "web");
                        child.SetTag("fig.reporting.client", "fig-web");
                        child.SetTag("fig.web.stage.name", stageName);
                        child.SetTag("fig.web.stage.duration_ms", stage.DurationMs);
                        child.SetEndTime(stageEnd);
                    }
                }

                stageCursor = stageEnd;
            }

            parent.SetEndTime(endedAt);

            _logger.LogInformation(
                "Recorded Fig.Web client-reported settings load timing: " +
                "clients={ClientCount}, settings={SettingCount}, totalMs={TotalMs}, stages={StageCount}",
                timing.ClientCount,
                timing.SettingCount,
                timing.TotalDurationMs,
                timing.Stages.Count);
        }
        finally
        {
            Activity.Current = previous;
        }
    }

    private static void SetWebOriginTags(Activity activity, WebClientLoadTimingDataContract timing)
    {
        activity.SetTag("fig.telemetry.origin", "web");
        activity.SetTag("fig.reporting.client", "fig-web");
        activity.SetTag("fig.web.client_count", timing.ClientCount);
        activity.SetTag("fig.web.setting_count", timing.SettingCount);
        activity.SetTag("fig.web.total_duration_ms", timing.TotalDurationMs);

        if (timing.DescriptionClientCount is not null)
            activity.SetTag("fig.web.description_client_count", timing.DescriptionClientCount.Value);

        if (timing.DescriptionResponseChars is not null)
            activity.SetTag("fig.web.description_response_chars", timing.DescriptionResponseChars.Value);

        if (timing.SettingGroupsHttpMs is not null)
            activity.SetTag("fig.web.settinggroups_http_ms", timing.SettingGroupsHttpMs.Value);

        if (timing.ConvertDescriptionHtmlMs is not null)
            activity.SetTag("fig.web.convert_description_html_ms", timing.ConvertDescriptionHtmlMs.Value);

        if (timing.HttpFetchRequestMs is not null)
            activity.SetTag("fig.web.httpfetch_request_ms", timing.HttpFetchRequestMs.Value);

        if (timing.HttpFetchDeserializeMs is not null)
            activity.SetTag("fig.web.httpfetch_deserialize_ms", timing.HttpFetchDeserializeMs.Value);

        if (timing.HttpFetchBodyReadMs is not null)
            activity.SetTag("fig.web.httpfetch_body_read_ms", timing.HttpFetchBodyReadMs.Value);

        if (timing.HttpFetchParseMs is not null)
            activity.SetTag("fig.web.httpfetch_parse_ms", timing.HttpFetchParseMs.Value);

        if (timing.ConvertModelBuildMs is not null)
            activity.SetTag("fig.web.convert_model_build_ms", timing.ConvertModelBuildMs.Value);

        if (timing.InitializeSettingsMs is not null)
            activity.SetTag("fig.web.initialize_settings_ms", timing.InitializeSettingsMs.Value);

        if (timing.InitializeScriptsMs is not null)
            activity.SetTag("fig.web.initialize_scripts_ms", timing.InitializeScriptsMs.Value);

        if (timing.InitializeOtherMs is not null)
            activity.SetTag("fig.web.initialize_other_ms", timing.InitializeOtherMs.Value);

        if (timing.DisplayScriptsExecuted is not null)
            activity.SetTag("fig.web.display_scripts_executed", timing.DisplayScriptsExecuted.Value);

        if (timing.DisplayScriptsSucceeded is not null)
            activity.SetTag("fig.web.display_scripts_succeeded", timing.DisplayScriptsSucceeded.Value);

        if (timing.DisplayScriptsFailed is not null)
            activity.SetTag("fig.web.display_scripts_failed", timing.DisplayScriptsFailed.Value);

        if (timing.DisplayScriptsSkipped is not null)
            activity.SetTag("fig.web.display_scripts_skipped", timing.DisplayScriptsSkipped.Value);

        if (timing.DisplayScriptFailures is { Count: > 0 })
            activity.SetTag("fig.web.display_script_failures", string.Join(" | ", timing.DisplayScriptFailures));

        foreach (var stage in timing.Stages)
        {
            if (string.IsNullOrWhiteSpace(stage.Name))
                continue;

            var tagName = $"fig.web.stage.{SanitizeTagName(stage.Name)}_ms";
            activity.SetTag(tagName, stage.DurationMs);
        }
    }

    private static string SanitizeTagName(string stageName)
    {
        return stageName.Trim().ToLowerInvariant().Replace(' ', '_');
    }
}
