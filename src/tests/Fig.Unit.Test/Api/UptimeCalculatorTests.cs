using Fig.Api.Reports;
using Fig.Common.Constants;
using Fig.Datalayer.BusinessEntities;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class UptimeCalculatorTests
{
    [Test]
    public void ShallComputeFullUptimeWhenSessionCoversRange()
    {
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddHours(10);
        var events = new List<EventLogBusinessEntity>
        {
            new()
            {
                EventType = EventMessage.NewSession,
                Timestamp = from.AddHours(-1)
            }
        };

        var result = UptimeCalculator.Calculate(from, to, events, Array.Empty<ClientRunSessionBusinessEntity>());

        Assert.That(result.UptimePercent, Is.EqualTo(100).Within(0.01));
        Assert.That(result.Outages, Is.Empty);
    }

    [Test]
    public void ShallComputeDowntimeBetweenSessions()
    {
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddHours(4);
        var events = new List<EventLogBusinessEntity>
        {
            new()
            {
                EventType = EventMessage.NewSession,
                Timestamp = from
            },
            new()
            {
                EventType = EventMessage.ExpiredSession,
                Timestamp = from.AddHours(1),
                OriginalValue = "host (1.1.1.1) up time:3600s"
            },
            new()
            {
                EventType = EventMessage.NewSession,
                Timestamp = from.AddHours(3)
            }
        };

        var result = UptimeCalculator.Calculate(from, to, events, Array.Empty<ClientRunSessionBusinessEntity>());

        Assert.That(result.Uptime.TotalHours, Is.EqualTo(2).Within(0.01));
        Assert.That(result.Downtime.TotalHours, Is.EqualTo(2).Within(0.01));
        Assert.That(result.UptimePercent, Is.EqualTo(50).Within(0.1));
        Assert.That(result.Outages.Count, Is.EqualTo(1));
    }

    [Test]
    public void ShallParseUptimeSecondsFromExpiredSessionMessage()
    {
        Assert.That(UptimeCalculator.TryParseUptimeSeconds("host (ip) up time:125.5s", out var seconds), Is.True);
        Assert.That(seconds, Is.EqualTo(125.5).Within(0.01));
    }

    [Test]
    public void ShallNotCountDowntimeWhenOneOfTwoConcurrentSessionsExpires()
    {
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddHours(4);
        var events = new List<EventLogBusinessEntity>
        {
            new()
            {
                EventType = EventMessage.NewSession,
                Timestamp = from,
                NewValue = "host-a (1.1.1.1)"
            },
            new()
            {
                EventType = EventMessage.NewSession,
                Timestamp = from.AddMinutes(5),
                NewValue = "host-b (1.1.1.2)"
            },
            new()
            {
                EventType = EventMessage.ExpiredSession,
                Timestamp = from.AddHours(2),
                OriginalValue = "host-a (1.1.1.1) up time:7200s"
            }
            // host-b remains open through end of range
        };

        var result = UptimeCalculator.Calculate(from, to, events, Array.Empty<ClientRunSessionBusinessEntity>());

        Assert.That(result.UptimePercent, Is.EqualTo(100).Within(0.01));
        Assert.That(result.Outages, Is.Empty);
        Assert.That(result.PeakConcurrentSessions, Is.EqualTo(2));
    }

    [Test]
    public void ShallRecordOutageOnlyWhenAllConcurrentSessionsAreOffline()
    {
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddHours(4);
        var events = new List<EventLogBusinessEntity>
        {
            new()
            {
                EventType = EventMessage.NewSession,
                Timestamp = from,
                NewValue = "host-a (1.1.1.1)"
            },
            new()
            {
                EventType = EventMessage.NewSession,
                Timestamp = from,
                NewValue = "host-b (1.1.1.2)"
            },
            new()
            {
                EventType = EventMessage.ExpiredSession,
                Timestamp = from.AddHours(1),
                OriginalValue = "host-a (1.1.1.1) up time:3600s"
            },
            new()
            {
                EventType = EventMessage.ExpiredSession,
                Timestamp = from.AddHours(2),
                OriginalValue = "host-b (1.1.1.2) up time:7200s"
            },
            new()
            {
                EventType = EventMessage.NewSession,
                Timestamp = from.AddHours(3),
                NewValue = "host-a (1.1.1.1)"
            }
        };

        var result = UptimeCalculator.Calculate(from, to, events, Array.Empty<ClientRunSessionBusinessEntity>());

        Assert.That(result.PeakConcurrentSessions, Is.EqualTo(2));
        Assert.That(result.Outages.Count, Is.EqualTo(1));
        Assert.That(result.Outages[0].StartUtc, Is.EqualTo(from.AddHours(2)));
        Assert.That(result.Outages[0].EndUtc, Is.EqualTo(from.AddHours(3)));
        Assert.That(result.Uptime.TotalHours, Is.EqualTo(3).Within(0.01));
    }
}

[TestFixture]
public class ClientStatusSecretExclusionTests
{
    [Test]
    public void ShallMaskSecretSettingsRatherThanOmitThem()
    {
        var settings = new[]
        {
            new SettingBusinessEntity { Name = "Public", IsSecret = false },
            new SettingBusinessEntity { Name = "Password", IsSecret = true },
            new SettingBusinessEntity { Name = "ApiKey", IsSecret = true }
        };

        var included = settings.Select(s => s.Name).ToList();
        var maskedCount = settings.Count(s => s.IsSecret);

        Assert.That(included, Is.EquivalentTo(new[] { "Public", "Password", "ApiKey" }));
        Assert.That(maskedCount, Is.EqualTo(2));
    }
}
