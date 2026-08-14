using Fig.Api.Converters;
using Fig.Api.Services;
using Fig.Contracts.Health;
using Fig.Datalayer.BusinessEntities;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class ClientUptimeTrackerTests
{
    [Test]
    public void ComputePercent_ShallReturnNull_BeforeFirstObservation()
    {
        var client = CreateClient();
        Assert.That(ClientUptimeTracker.ComputePercent(client, DateTime.UtcNow), Is.Null);
    }

    [Test]
    public void ApplyStateChange_ShallInitialize_OnFirstObservation()
    {
        var client = CreateClient(CreateSession(FigHealthStatus.Healthy));
        var now = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

        var changed = ClientUptimeTracker.ApplyStateChange(client, now);

        Assert.That(changed, Is.True);
        Assert.That(client.UptimeCurrentlyUp, Is.True);
        Assert.That(client.UptimeWindowStartUtc, Is.EqualTo(now));
        Assert.That(client.UptimeLastStateChangeUtc, Is.EqualTo(now));
        Assert.That(client.UptimeAccumulatedMs, Is.EqualTo(0));
        Assert.That(ClientUptimeTracker.ComputePercent(client, now), Is.EqualTo(100).Within(0.01));
    }

    [Test]
    public void ApplyStateChange_ShallNoOp_WhenStateUnchanged()
    {
        var client = CreateClient(CreateSession(FigHealthStatus.Healthy));
        var t0 = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        ClientUptimeTracker.ApplyStateChange(client, t0);

        var changed = ClientUptimeTracker.ApplyStateChange(client, t0.AddHours(1));

        Assert.That(changed, Is.False);
        Assert.That(client.UptimeLastStateChangeUtc, Is.EqualTo(t0));
    }

    [Test]
    public void ApplyStateChange_ShallAccumulate_WhenGoingDown()
    {
        var client = CreateClient(CreateSession(FigHealthStatus.Healthy));
        var t0 = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        ClientUptimeTracker.ApplyStateChange(client, t0);

        client.RunSessions.Clear();
        var t1 = t0.AddHours(6);
        ClientUptimeTracker.ApplyStateChange(client, t1);

        Assert.That(client.UptimeCurrentlyUp, Is.False);
        Assert.That(client.UptimeAccumulatedMs, Is.EqualTo((long)TimeSpan.FromHours(6).TotalMilliseconds));

        var percent = ClientUptimeTracker.ComputePercent(client, t1);
        Assert.That(percent, Is.EqualTo(100).Within(0.01));
    }

    [Test]
    public void ComputePercent_ShallReflectPartialUptime_AfterDowntime()
    {
        var client = CreateClient(CreateSession(FigHealthStatus.Healthy));
        var t0 = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        ClientUptimeTracker.ApplyStateChange(client, t0);

        // Up for 18h, then down
        client.RunSessions.Clear();
        var t1 = t0.AddHours(18);
        ClientUptimeTracker.ApplyStateChange(client, t1);

        // Still down at 24h → 18/24 = 75%
        var percent = ClientUptimeTracker.ComputePercent(client, t0.AddHours(24));
        Assert.That(percent, Is.EqualTo(75).Within(0.1));
    }

    [Test]
    public void IsClientUp_ShallTreatUnknownAsUp()
    {
        var client = CreateClient(CreateSession(FigHealthStatus.Unknown));
        Assert.That(ClientUptimeTracker.IsClientUp(client), Is.True);
    }

    [Test]
    public void IsClientUp_ShallTreatDegradedOnlyAsDown()
    {
        var client = CreateClient(CreateSession(FigHealthStatus.Degraded));
        Assert.That(ClientUptimeTracker.IsClientUp(client), Is.False);
    }

    [Test]
    public void IsClientUp_ShallBeUp_WhenAnySessionHealthyOrUnknown()
    {
        var client = CreateClient(
            CreateSession(FigHealthStatus.Unhealthy),
            CreateSession(FigHealthStatus.Unknown));
        Assert.That(ClientUptimeTracker.IsClientUp(client), Is.True);
    }

    [Test]
    public void ApplyStateChange_ShallScaleAccumulated_WhenWindowExceeds24Hours()
    {
        var client = CreateClient(CreateSession(FigHealthStatus.Healthy));
        var t0 = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        ClientUptimeTracker.ApplyStateChange(client, t0);

        // Up for 48h then go down → close 48h segment, scale by 24/48
        client.RunSessions.Clear();
        var t1 = t0.AddHours(48);
        ClientUptimeTracker.ApplyStateChange(client, t1);

        Assert.That(client.UptimeAccumulatedMs, Is.EqualTo((long)TimeSpan.FromHours(24).TotalMilliseconds).Within(2));
        Assert.That(client.UptimeWindowStartUtc, Is.EqualTo(t1 - ClientUptimeTracker.Window));

        var percent = ClientUptimeTracker.ComputePercent(client, t1);
        Assert.That(percent, Is.EqualTo(100).Within(0.1));
    }

    [Test]
    public void ComputePercent_ShallIncludeOpenUpSegment()
    {
        var client = CreateClient(CreateSession(FigHealthStatus.Healthy));
        var t0 = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        ClientUptimeTracker.ApplyStateChange(client, t0);

        var percent = ClientUptimeTracker.ComputePercent(client, t0.AddHours(12));
        Assert.That(percent, Is.EqualTo(100).Within(0.01));
    }

    [Test]
    public void ClientStatusConverter_ShallExposeUptimePercent()
    {
        var client = CreateClient(CreateSession(FigHealthStatus.Healthy));
        var t0 = DateTime.UtcNow.AddHours(-1);
        ClientUptimeTracker.ApplyStateChange(client, t0);

        var converted = new ClientStatusConverter().Convert(client);

        Assert.That(converted.UptimePercent24Hr, Is.Not.Null);
        Assert.That(converted.UptimePercent24Hr!.Value, Is.EqualTo(100).Within(0.1));
    }

    private static ClientStatusBusinessEntity CreateClient(params ClientRunSessionBusinessEntity[] sessions)
    {
        return new ClientStatusBusinessEntity
        {
            Name = "TestClient",
            ClientSecret = "secret",
            RunSessions = sessions.ToList()
        };
    }

    private static ClientRunSessionBusinessEntity CreateSession(FigHealthStatus status)
    {
        return new ClientRunSessionBusinessEntity
        {
            RunSessionId = Guid.NewGuid(),
            LastSeen = DateTime.UtcNow,
            PollIntervalMs = 30_000,
            StartTimeUtc = DateTime.UtcNow.AddHours(-1),
            HealthStatus = status,
            FigVersion = "1.0",
            ApplicationVersion = "1.0",
            RunningUser = "test"
        };
    }
}
