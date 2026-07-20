using System.Diagnostics;
using Fig.Api.Observability;
using Fig.Api.Services;
using Fig.Contracts.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class WebClientLoadTimingServiceTests
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
    public void RecordClientLoadTiming_EmitsFigWebSpansWithOriginAndCounts()
    {
        var service = new WebClientLoadTimingService(NullLogger<WebClientLoadTimingService>.Instance);
        var startedAt = DateTime.UtcNow.AddSeconds(-5);
        var timing = new WebClientLoadTimingDataContract(
            startedAt,
            totalDurationMs: 4500,
            clientCount: 12,
            settingCount: 340,
            stages:
            [
                new WebClientLoadTimingStageDataContract(WebClientLoadTimingStageNames.HttpFetchClients, 2000),
                new WebClientLoadTimingStageDataContract(WebClientLoadTimingStageNames.ConvertToModels, 1500),
                new WebClientLoadTimingStageDataContract(WebClientLoadTimingStageNames.InitializeModels, 1000)
            ],
            descriptionClientCount: 5,
            descriptionResponseChars: 12000,
            settingGroupsHttpMs: 980,
            convertDescriptionHtmlMs: 0,
            httpFetchRequestMs: 1800,
            httpFetchDeserializeMs: 200,
            httpFetchBodyReadMs: 120,
            httpFetchParseMs: 80,
            convertModelBuildMs: 1400,
            initializeSettingsMs: 900,
            displayScriptsExecuted: 48,
            displayScriptsSucceeded: 45,
            displayScriptsFailed: 2,
            displayScriptsSkipped: 1,
            initializeScriptsMs: 700,
            initializeOtherMs: 200,
            displayScriptFailures: ["ClientA/SettingX: Unexpected token"]);

        service.RecordClientLoadTiming(timing);

        var parent = _stoppedActivities.Single(a => a.OperationName == "Web.SettingsClientLoad");
        Assert.That(parent.Source.Name, Is.EqualTo(WebClientActivitySource.Name));
        Assert.That(parent.GetTagItem("fig.telemetry.origin"), Is.EqualTo("web"));
        Assert.That(parent.GetTagItem("fig.reporting.client"), Is.EqualTo("fig-web"));
        Assert.That(parent.GetTagItem("fig.web.client_count"), Is.EqualTo(12));
        Assert.That(parent.GetTagItem("fig.web.setting_count"), Is.EqualTo(340));
        Assert.That(parent.GetTagItem("fig.web.total_duration_ms"), Is.EqualTo(4500L));
        Assert.That(parent.GetTagItem("fig.web.description_client_count"), Is.EqualTo(5));
        Assert.That(parent.GetTagItem("fig.web.description_response_chars"), Is.EqualTo(12000L));
        Assert.That(parent.GetTagItem("fig.web.settinggroups_http_ms"), Is.EqualTo(980L));
        Assert.That(parent.GetTagItem("fig.web.convert_description_html_ms"), Is.EqualTo(0L));
        Assert.That(parent.GetTagItem("fig.web.httpfetch_request_ms"), Is.EqualTo(1800L));
        Assert.That(parent.GetTagItem("fig.web.httpfetch_deserialize_ms"), Is.EqualTo(200L));
        Assert.That(parent.GetTagItem("fig.web.httpfetch_body_read_ms"), Is.EqualTo(120L));
        Assert.That(parent.GetTagItem("fig.web.httpfetch_parse_ms"), Is.EqualTo(80L));
        Assert.That(parent.GetTagItem("fig.web.convert_model_build_ms"), Is.EqualTo(1400L));
        Assert.That(parent.GetTagItem("fig.web.initialize_settings_ms"), Is.EqualTo(900L));
        Assert.That(parent.GetTagItem("fig.web.initialize_scripts_ms"), Is.EqualTo(700L));
        Assert.That(parent.GetTagItem("fig.web.initialize_other_ms"), Is.EqualTo(200L));
        Assert.That(parent.GetTagItem("fig.web.display_scripts_executed"), Is.EqualTo(48));
        Assert.That(parent.GetTagItem("fig.web.display_scripts_succeeded"), Is.EqualTo(45));
        Assert.That(parent.GetTagItem("fig.web.display_scripts_failed"), Is.EqualTo(2));
        Assert.That(parent.GetTagItem("fig.web.display_scripts_skipped"), Is.EqualTo(1));
        Assert.That(parent.GetTagItem("fig.web.display_script_failures"), Is.EqualTo("ClientA/SettingX: Unexpected token"));
        Assert.That(parent.GetTagItem("fig.web.stage.httpfetchclients_ms"), Is.EqualTo(2000L));
        Assert.That(parent.GetTagItem("fig.web.stage.converttomodels_ms"), Is.EqualTo(1500L));
        Assert.That(parent.GetTagItem("fig.web.stage.initializemodels_ms"), Is.EqualTo(1000L));

        var stageNames = _stoppedActivities
            .Where(a => a.OperationName.StartsWith("Web.") && a.OperationName != "Web.SettingsClientLoad")
            .Select(a => a.OperationName)
            .ToList();

        Assert.That(stageNames, Is.EquivalentTo(new[]
        {
            "Web.HttpFetchClients",
            "Web.ConvertToModels",
            "Web.InitializeModels"
        }));

        foreach (var stage in _stoppedActivities.Where(a => a.ParentId == parent.Id))
        {
            Assert.That(stage.Source.Name, Is.EqualTo(WebClientActivitySource.Name));
            Assert.That(stage.GetTagItem("fig.telemetry.origin"), Is.EqualTo("web"));
            Assert.That(stage.Duration.TotalMilliseconds, Is.GreaterThan(0));
        }

        Assert.That(parent.Duration, Is.EqualTo(TimeSpan.FromMilliseconds(4500)).Within(TimeSpan.FromMilliseconds(50)));
    }

    [Test]
    public void RecordClientLoadTiming_WithEmptyReport_DoesNotEmitSpans()
    {
        var service = new WebClientLoadTimingService(NullLogger<WebClientLoadTimingService>.Instance);
        var timing = new WebClientLoadTimingDataContract(
            DateTime.UtcNow,
            totalDurationMs: 0,
            clientCount: 0,
            settingCount: 0,
            stages: []);

        service.RecordClientLoadTiming(timing);

        Assert.That(_stoppedActivities, Is.Empty);
    }

    [Test]
    public void RecordClientLoadTiming_WithAmbientActivity_CreatesRootSpanNotNestedUnderAmbient()
    {
        using var ambient = new Activity("HttpPOST").Start();
        Assert.That(Activity.Current, Is.SameAs(ambient));

        var service = new WebClientLoadTimingService(NullLogger<WebClientLoadTimingService>.Instance);
        var startedAt = DateTime.UtcNow.AddSeconds(-3);
        var timing = new WebClientLoadTimingDataContract(
            startedAt,
            totalDurationMs: 2500,
            clientCount: 4,
            settingCount: 20,
            stages:
            [
                new WebClientLoadTimingStageDataContract(WebClientLoadTimingStageNames.HttpFetchClients, 1500),
                new WebClientLoadTimingStageDataContract(WebClientLoadTimingStageNames.ConvertToModels, 1000)
            ]);

        service.RecordClientLoadTiming(timing);

        Assert.That(Activity.Current, Is.SameAs(ambient), "Ambient request activity should be restored");

        var parent = _stoppedActivities.Single(a => a.OperationName == "Web.SettingsClientLoad");
        Assert.That(parent.ParentId, Is.Null.Or.Empty);
        Assert.That(parent.TraceId, Is.Not.EqualTo(ambient.TraceId));

        var stages = _stoppedActivities
            .Where(a => a.ParentId == parent.Id)
            .ToList();
        Assert.That(stages.Select(a => a.OperationName), Is.EquivalentTo(new[]
        {
            "Web.HttpFetchClients",
            "Web.ConvertToModels"
        }));
        Assert.That(stages.All(a => a.TraceId == parent.TraceId), Is.True);

        Assert.That(parent.Links.Count, Is.EqualTo(1));
        Assert.That(parent.Links.Single().Context.TraceId, Is.EqualTo(ambient.TraceId));
        Assert.That(parent.Links.Single().Context.SpanId, Is.EqualTo(ambient.SpanId));
    }
}
