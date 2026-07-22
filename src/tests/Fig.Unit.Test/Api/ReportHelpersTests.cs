using Fig.Api.Reports;
using Fig.Api.Reports.Implementations;
using Fig.Client.Abstractions.Data;
using Fig.Common.Constants;
using Fig.Datalayer.BusinessEntities;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class ReportDateRangeTests
{
    [Test]
    public void Validate_ThrowsWhenFromAfterTo()
    {
        var from = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        Assert.Throws<ReportParameterValidationException>(() => ReportDateRange.Validate(from, to));
    }

    [Test]
    public void Validate_ThrowsWhenSpanExceedsMax()
    {
        var from = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddDays(400);
        Assert.Throws<ReportParameterValidationException>(() => ReportDateRange.Validate(from, to));
    }

    [Test]
    public void Validate_ReturnsUtcBounds()
    {
        var from = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc);
        var (fromUtc, toUtc) = ReportDateRange.Validate(from, to);
        Assert.That(fromUtc, Is.EqualTo(from));
        Assert.That(toUtc, Is.EqualTo(to));
    }

    [Test]
    public void NormalizeOptionalClient_TreatsWhitespaceAsNull()
    {
        Assert.That(ReportDateRange.NormalizeOptionalClient("  "), Is.Null);
        Assert.That(ReportDateRange.NormalizeOptionalClient("Acme"), Is.EqualTo("Acme"));
    }
}

[TestFixture]
public class EventAnalyticsTests
{
    [Test]
    public void CountByEventType_OrdersByCountDescending()
    {
        var events = new[]
        {
            Log(EventMessage.Login),
            Log(EventMessage.Login),
            Log(EventMessage.LoginFailed)
        };

        var slices = EventAnalytics.CountByEventType(events);
        Assert.That(slices[0].Label, Is.EqualTo(EventMessage.Login));
        Assert.That(slices[0].Value, Is.EqualTo(2));
    }

    [Test]
    public void TopBy_ReturnsTopN()
    {
        var events = new[]
        {
            Log(EventMessage.SettingValueUpdated, "A"),
            Log(EventMessage.SettingValueUpdated, "A"),
            Log(EventMessage.SettingValueUpdated, "B")
        };

        var top = EventAnalytics.TopBy(events, e => e.ClientName, 1);
        Assert.That(top, Has.Count.EqualTo(1));
        Assert.That(top[0].Key, Is.EqualTo("A"));
        Assert.That(top[0].Count, Is.EqualTo(2));
    }

    [Test]
    public void DailySeries_FillsMissingDays()
    {
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddDays(2);
        var events = new[] { Log(EventMessage.Login, timestamp: from.AddHours(1)) };
        var series = EventAnalytics.DailySeries(events, from, to);
        Assert.That(series, Has.Count.EqualTo(3));
        Assert.That(series[0].Count, Is.EqualTo(1));
        Assert.That(series[1].Count, Is.EqualTo(0));
    }

    [Test]
    public void CountOfType_CountsMatchingEvents()
    {
        var events = new[]
        {
            Log(EventMessage.Login),
            Log(EventMessage.Login),
            Log(EventMessage.LoginFailed)
        };

        Assert.That(EventAnalytics.CountOfType(events, EventMessage.Login), Is.EqualTo(2));
        Assert.That(EventAnalytics.CountOfType(events, EventMessage.SettingValueUpdated), Is.EqualTo(0));
    }

    private static EventLogBusinessEntity Log(
        string eventType,
        string? clientName = null,
        DateTime? timestamp = null)
        => new()
        {
            EventType = eventType,
            ClientName = clientName,
            Timestamp = timestamp ?? DateTime.UtcNow
        };
}

[TestFixture]
public class AnomalyDetectorTests
{
    [Test]
    public void Evaluate_FlagsWhenPeriodAtLeastDoubleBaselineAndBaselineMeetsFloor()
    {
        var metric = AnomalyDetector.Evaluate("Test", 10, 3);
        Assert.That(metric.IsAnomaly, Is.True);
    }

    [Test]
    public void Evaluate_DoesNotFlagWhenBaselineBelowFloor()
    {
        var metric = AnomalyDetector.Evaluate("Test", 100, 2);
        Assert.That(metric.IsAnomaly, Is.False);
    }

    [Test]
    public void Detect_ComputesSettingUpdateSpike()
    {
        var period = Enumerable.Range(0, 10)
            .Select(_ => new EventLogBusinessEntity { EventType = EventMessage.SettingValueUpdated })
            .ToList();
        var baseline = Enumerable.Range(0, 4)
            .Select(_ => new EventLogBusinessEntity { EventType = EventMessage.SettingValueUpdated })
            .ToList();

        var metrics = AnomalyDetector.Detect(period, baseline);
        Assert.That(metrics.Single(m => m.Name == "Setting Updates").IsAnomaly, Is.True);
        Assert.That(metrics.Single(m => m.Name == "Failed Logins").IsAnomaly, Is.False);
    }
}

[TestFixture]
public class SettingInventoryProjectorTests
{
    [Test]
    public void Project_MapsFlagsWithoutValues()
    {
        var client = new SettingClientBusinessEntity
        {
            Name = "App",
            Instance = "prod",
            Settings =
            [
                new SettingBusinessEntity
                {
                    Name = "Password",
                    IsSecret = true,
                    IsExternallyManaged = true,
                    Classification = Classification.Special,
                    EnvironmentSpecific = true,
                    InitOnlyExport = true,
                    SupportsLiveUpdate = false,
                    LookupTableKey = "Regions",
                    CategoryName = "Security"
                }
            ]
        };

        var row = SettingInventoryProjector.Project(client, client.Settings.First());
        Assert.That(row.IsSecret, Is.True);
        Assert.That(row.IsExternallyManaged, Is.True);
        Assert.That(row.Classification, Is.EqualTo("Special"));
        Assert.That(row.EnvironmentSpecific, Is.True);
        Assert.That(row.InitOnlyExport, Is.True);
        Assert.That(row.Category, Is.EqualTo("Security"));
        Assert.That(row.ClientDisplay, Does.Contain("App"));
    }

    [Test]
    public void ProjectAll_SecretsOnlyFilters()
    {
        var client = new SettingClientBusinessEntity
        {
            Name = "App",
            Settings =
            [
                new SettingBusinessEntity { Name = "A", IsSecret = true },
                new SettingBusinessEntity { Name = "B", IsSecret = false }
            ]
        };

        var rows = SettingInventoryProjector.ProjectAll([client], secretsOnly: true);
        Assert.That(rows, Has.Count.EqualTo(1));
        Assert.That(rows[0].SettingName, Is.EqualTo("A"));
    }
}

[TestFixture]
public class FleetUptimeAggregatorTests
{
    [Test]
    public void Aggregate_OrdersByLowestUptimeFirst()
    {
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddHours(2);

        var alwaysUp = (
            "UpClient",
            (string?)null,
            (IReadOnlyList<EventLogBusinessEntity>)
            [
                new EventLogBusinessEntity { EventType = EventMessage.NewSession, Timestamp = from }
            ],
            (IReadOnlyList<ClientRunSessionBusinessEntity>)[]);

        var alwaysDown = (
            "DownClient",
            (string?)null,
            (IReadOnlyList<EventLogBusinessEntity>)[],
            (IReadOnlyList<ClientRunSessionBusinessEntity>)[]);

        var rows = FleetUptimeAggregator.Aggregate(from, to, [alwaysUp, alwaysDown]);
        Assert.That(rows[0].ClientName, Is.EqualTo("DownClient"));
        Assert.That(rows[0].UptimePercent, Is.EqualTo(0).Within(0.01));
        Assert.That(rows[1].ClientName, Is.EqualTo("UpClient"));
        Assert.That(rows[1].UptimePercent, Is.EqualTo(100).Within(0.01));
    }

    [Test]
    public void Aggregate_ReturnsEmptyForNoClients()
    {
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddHours(1);
        var rows = FleetUptimeAggregator.Aggregate(from, to, []);
        Assert.That(rows, Is.Empty);
    }

    [Test]
    public void Aggregate_HandlesSingleClient()
    {
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddHours(1);
        var client = (
            "Only",
            (string?)null,
            (IReadOnlyList<EventLogBusinessEntity>)
            [
                new EventLogBusinessEntity { EventType = EventMessage.NewSession, Timestamp = from }
            ],
            (IReadOnlyList<ClientRunSessionBusinessEntity>)[]);

        var rows = FleetUptimeAggregator.Aggregate(from, to, [client]);
        Assert.That(rows, Has.Count.EqualTo(1));
        Assert.That(rows[0].UptimePercent, Is.EqualTo(100).Within(0.01));
    }
}

[TestFixture]
public class ClientVersionReportHelperTests
{
    [Test]
    public void BuildMultiVersionClientNames_FlagsClientsWithMultipleAppVersions()
    {
        var sessions = new[]
        {
            new ClientVersionReport.SessionSnapshot("AppA", "(default)", "h1", "1.0.0", "3.0.0", DateTime.UtcNow),
            new ClientVersionReport.SessionSnapshot("AppA", "prod", "h2", "1.1.0", "3.0.0", DateTime.UtcNow),
            new ClientVersionReport.SessionSnapshot("AppB", "(default)", "h3", "2.0.0", "3.1.0", DateTime.UtcNow),
            new ClientVersionReport.SessionSnapshot("AppB", "prod", "h4", "2.0.0", "3.2.0", DateTime.UtcNow)
        };

        var multi = ClientVersionReport.BuildMultiVersionClientNames(sessions);

        Assert.That(multi, Does.Contain("AppA"));
        Assert.That(multi, Does.Not.Contain("AppB"));
    }

    [Test]
    public void BuildFigVersionBreakdown_GroupsByFigVersion()
    {
        var sessions = new[]
        {
            new ClientVersionReport.SessionSnapshot("AppA", "(default)", "h1", "1.0.0", "3.0.0", DateTime.UtcNow),
            new ClientVersionReport.SessionSnapshot("AppA", "prod", "h2", "1.1.0", "3.0.0", DateTime.UtcNow),
            new ClientVersionReport.SessionSnapshot("AppB", "(default)", "h3", "2.0.0", "3.1.0", DateTime.UtcNow)
        };

        var slices = ClientVersionReport.BuildFigVersionBreakdown(sessions);

        Assert.That(slices, Has.Count.EqualTo(2));
        Assert.That(slices[0].Label, Is.EqualTo("3.0.0"));
        Assert.That(slices[0].Value, Is.EqualTo(2));
        Assert.That(slices[1].Label, Is.EqualTo("3.1.0"));
        Assert.That(slices[1].Value, Is.EqualTo(1));
    }
}
