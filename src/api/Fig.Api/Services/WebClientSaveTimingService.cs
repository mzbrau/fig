using System.Diagnostics;
using Fig.Api.Observability;
using Fig.Contracts.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Fig.Api.Services;

public class WebClientSaveTimingService : IWebClientSaveTimingService
{
    private readonly ILogger<WebClientSaveTimingService> _logger;

    public WebClientSaveTimingService(ILogger<WebClientSaveTimingService> logger)
    {
        _logger = logger;
    }

    public void RecordClientSaveTiming(WebClientSaveTimingDataContract timing)
    {
        if (timing.Stages.Count == 0 && timing.TotalDurationMs <= 0)
        {
            _logger.LogDebug("Ignoring empty web client save timing report");
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
                "Web.SettingsClientSave",
                ActivityKind.Internal,
                parentContext: default,
                tags: null,
                links: links,
                startTime: startedAtOffset);

            if (parent is null)
            {
                _logger.LogDebug(
                    "Fig.Web activity source has no listeners; skipped recording web client save timing " +
                    "(clients={ClientCount}, dirtyClients={DirtyClientCount}, changes={ChangeCount}, totalMs={TotalMs})",
                    timing.ClientCount,
                    timing.DirtyClientCount,
                    timing.SettingChangeCount,
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
                "Recorded Fig.Web client-reported settings save timing: " +
                "clients={ClientCount}, dirtyClients={DirtyClientCount}, changes={ChangeCount}, " +
                "puts={PutCount}, saveAll={IsSaveAll}, totalMs={TotalMs}, stages={StageCount}",
                timing.ClientCount,
                timing.DirtyClientCount,
                timing.SettingChangeCount,
                timing.HttpPutCount,
                timing.IsSaveAll,
                timing.TotalDurationMs,
                timing.Stages.Count);
        }
        finally
        {
            Activity.Current = previous;
        }
    }

    private static void SetWebOriginTags(Activity activity, WebClientSaveTimingDataContract timing)
    {
        activity.SetTag("fig.telemetry.origin", "web");
        activity.SetTag("fig.reporting.client", "fig-web");
        activity.SetTag("fig.web.client_count", timing.ClientCount);
        activity.SetTag("fig.web.dirty_client_count", timing.DirtyClientCount);
        activity.SetTag("fig.web.setting_change_count", timing.SettingChangeCount);
        activity.SetTag("fig.web.http_put_count", timing.HttpPutCount);
        activity.SetTag("fig.web.is_save_all", timing.IsSaveAll);
        activity.SetTag("fig.web.total_duration_ms", timing.TotalDurationMs);

        if (timing.HttpPutMaxMs is not null)
            activity.SetTag("fig.web.http_put_max_ms", timing.HttpPutMaxMs.Value);

        if (timing.HttpPutAvgMs is not null)
            activity.SetTag("fig.web.http_put_avg_ms", timing.HttpPutAvgMs.Value);

        if (timing.SideEffectsMs is not null)
            activity.SetTag("fig.web.side_effects_ms", timing.SideEffectsMs.Value);

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
