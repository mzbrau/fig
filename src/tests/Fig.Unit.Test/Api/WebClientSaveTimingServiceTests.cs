using System.Diagnostics;
using Fig.Api.Observability;
using Fig.Api.Services;
using Fig.Contracts.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class WebClientSaveTimingServiceTests
{
    private readonly List<Activity> _stoppedActivities = new();
    private ActivityListener? _listener;

    [SetUp]
    public void SetUp()
    {
        _stoppedActivities.Clear();
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == WebClientActivitySource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => _stoppedActivities.Add(activity)
        };
        ActivitySource.AddActivityListener(_listener);
    }

    [TearDown]
    public void TearDown()
    {
        _listener?.Dispose();
        _listener = null;
    }

    [Test]
    public void RecordClientSaveTiming_EmitsFigWebSpansWithOriginAndCounts()
    {
        var service = new WebClientSaveTimingService(NullLogger<WebClientSaveTimingService>.Instance);
        var startedAt = DateTime.UtcNow.AddSeconds(-4);
        var timing = new WebClientSaveTimingDataContract(
            startedAt,
            totalDurationMs: 3800,
            clientCount: 8,
            dirtyClientCount: 3,
            settingChangeCount: 12,
            httpPutCount: 3,
            isSaveAll: true,
            stages:
            [
                new WebClientSaveTimingStageDataContract(WebClientSaveTimingStageNames.CollectChanges, 50),
                new WebClientSaveTimingStageDataContract(WebClientSaveTimingStageNames.HttpPutSettings, 3000),
                new WebClientSaveTimingStageDataContract(WebClientSaveTimingStageNames.RefreshStatuses, 500),
                new WebClientSaveTimingStageDataContract(WebClientSaveTimingStageNames.RefreshScheduling, 250),
                new WebClientSaveTimingStageDataContract(WebClientSaveTimingStageNames.Other, 0)
            ],
            httpPutMaxMs: 1200,
            httpPutAvgMs: 1000,
            sideEffectsMs: 2);

        service.RecordClientSaveTiming(timing);

        var parent = _stoppedActivities.Single(a => a.OperationName == "Web.SettingsClientSave");
        Assert.That(parent.Source.Name, Is.EqualTo(WebClientActivitySource.Name));
        Assert.That(parent.GetTagItem("fig.telemetry.origin"), Is.EqualTo("web"));
        Assert.That(parent.GetTagItem("fig.reporting.client"), Is.EqualTo("fig-web"));
        Assert.That(parent.GetTagItem("fig.web.client_count"), Is.EqualTo(8));
        Assert.That(parent.GetTagItem("fig.web.dirty_client_count"), Is.EqualTo(3));
        Assert.That(parent.GetTagItem("fig.web.setting_change_count"), Is.EqualTo(12));
        Assert.That(parent.GetTagItem("fig.web.http_put_count"), Is.EqualTo(3));
        Assert.That(parent.GetTagItem("fig.web.is_save_all"), Is.EqualTo(true));
        Assert.That(parent.GetTagItem("fig.web.total_duration_ms"), Is.EqualTo(3800L));
        Assert.That(parent.GetTagItem("fig.web.http_put_max_ms"), Is.EqualTo(1200L));
        Assert.That(parent.GetTagItem("fig.web.http_put_avg_ms"), Is.EqualTo(1000L));
        Assert.That(parent.GetTagItem("fig.web.side_effects_ms"), Is.EqualTo(2L));
        Assert.That(parent.GetTagItem("fig.web.stage.collectchanges_ms"), Is.EqualTo(50L));
        Assert.That(parent.GetTagItem("fig.web.stage.httpputsettings_ms"), Is.EqualTo(3000L));
        Assert.That(parent.GetTagItem("fig.web.stage.refreshstatuses_ms"), Is.EqualTo(500L));
        Assert.That(parent.GetTagItem("fig.web.stage.refreshscheduling_ms"), Is.EqualTo(250L));
        Assert.That(parent.GetTagItem("fig.web.stage.other_ms"), Is.EqualTo(0L));

        var stageNames = _stoppedActivities
            .Where(a => a.OperationName.StartsWith("Web.") && a.OperationName != "Web.SettingsClientSave")
            .Select(a => a.OperationName)
            .ToList();

        Assert.That(stageNames, Is.EquivalentTo(new[]
        {
            "Web.CollectChanges",
            "Web.HttpPutSettings",
            "Web.RefreshStatuses",
            "Web.RefreshScheduling",
            "Web.Other"
        }));

        foreach (var stage in _stoppedActivities.Where(a => a.ParentId == parent.Id && a.OperationName != "Web.Other"))
        {
            Assert.That(stage.Source.Name, Is.EqualTo(WebClientActivitySource.Name));
            Assert.That(stage.GetTagItem("fig.telemetry.origin"), Is.EqualTo("web"));
            Assert.That(stage.Duration.TotalMilliseconds, Is.GreaterThan(0));
        }

        Assert.That(parent.Duration, Is.EqualTo(TimeSpan.FromMilliseconds(3800)).Within(TimeSpan.FromMilliseconds(50)));
    }

    [Test]
    public void RecordClientSaveTiming_WithEmptyReport_DoesNotEmitSpans()
    {
        var service = new WebClientSaveTimingService(NullLogger<WebClientSaveTimingService>.Instance);
        var timing = new WebClientSaveTimingDataContract(
            DateTime.UtcNow,
            totalDurationMs: 0,
            clientCount: 0,
            dirtyClientCount: 0,
            settingChangeCount: 0,
            httpPutCount: 0,
            isSaveAll: false,
            stages: []);

        service.RecordClientSaveTiming(timing);

        Assert.That(_stoppedActivities, Is.Empty);
    }

    [Test]
    public void RecordClientSaveTiming_WithAmbientActivity_CreatesRootSpanNotNestedUnderAmbient()
    {
        using var ambient = new Activity("HttpPOST").Start();
        Assert.That(Activity.Current, Is.SameAs(ambient));

        var service = new WebClientSaveTimingService(NullLogger<WebClientSaveTimingService>.Instance);
        var startedAt = DateTime.UtcNow.AddSeconds(-2);
        var timing = new WebClientSaveTimingDataContract(
            startedAt,
            totalDurationMs: 1500,
            clientCount: 2,
            dirtyClientCount: 1,
            settingChangeCount: 4,
            httpPutCount: 1,
            isSaveAll: false,
            stages:
            [
                new WebClientSaveTimingStageDataContract(WebClientSaveTimingStageNames.CollectChanges, 20),
                new WebClientSaveTimingStageDataContract(WebClientSaveTimingStageNames.HttpPutSettings, 1000),
                new WebClientSaveTimingStageDataContract(WebClientSaveTimingStageNames.RefreshStatuses, 300),
                new WebClientSaveTimingStageDataContract(WebClientSaveTimingStageNames.RefreshScheduling, 180),
                new WebClientSaveTimingStageDataContract(WebClientSaveTimingStageNames.Other, 0)
            ]);

        service.RecordClientSaveTiming(timing);

        Assert.That(Activity.Current, Is.SameAs(ambient), "Ambient request activity should be restored");

        var parent = _stoppedActivities.Single(a => a.OperationName == "Web.SettingsClientSave");
        Assert.That(parent.ParentId, Is.Null.Or.Empty);
        Assert.That(parent.TraceId, Is.Not.EqualTo(ambient.TraceId));

        var stages = _stoppedActivities
            .Where(a => a.ParentId == parent.Id)
            .ToList();
        Assert.That(stages.Select(a => a.OperationName), Is.EquivalentTo(new[]
        {
            "Web.CollectChanges",
            "Web.HttpPutSettings",
            "Web.RefreshStatuses",
            "Web.RefreshScheduling",
            "Web.Other"
        }));
        Assert.That(stages.All(a => a.TraceId == parent.TraceId), Is.True);

        Assert.That(parent.Links.Count, Is.EqualTo(1));
        Assert.That(parent.Links.Single().Context.TraceId, Is.EqualTo(ambient.TraceId));
        Assert.That(parent.Links.Single().Context.SpanId, Is.EqualTo(ambient.SpanId));
    }

    [Test]
    public void RecordClientSaveTiming_WithNegativeStageDuration_ClampsStageDurationTagsToZero()
    {
        var service = new WebClientSaveTimingService(NullLogger<WebClientSaveTimingService>.Instance);
        var timing = new WebClientSaveTimingDataContract(
            DateTime.UtcNow.AddSeconds(-1),
            totalDurationMs: 100,
            clientCount: 1,
            dirtyClientCount: 1,
            settingChangeCount: 1,
            httpPutCount: 1,
            isSaveAll: false,
            stages:
            [
                new WebClientSaveTimingStageDataContract(WebClientSaveTimingStageNames.CollectChanges, -10)
            ]);

        service.RecordClientSaveTiming(timing);

        var parent = _stoppedActivities.Single(a => a.OperationName == "Web.SettingsClientSave");
        Assert.That(parent.GetTagItem("fig.web.stage.collectchanges_ms"), Is.EqualTo(0L));

        var child = _stoppedActivities.Single(a => a.OperationName == "Web.CollectChanges");
        Assert.That(child.GetTagItem("fig.web.stage.duration_ms"), Is.EqualTo(0L));
    }
}
