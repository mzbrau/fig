using Fig.Api.ExtensionMethods;
using Fig.Contracts.Health;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Services;

/// <summary>
/// Approximate rolling 24-hour uptime for a client.
/// A client is "up" when ≥1 non-expired run session is Healthy or Unknown.
/// When the observation span exceeds 24 hours, accumulated uptime is scaled by
/// <c>24h / elapsed</c>. That is an intentional approximation (not a true sliding
/// segment/bucket store): older state is decayed rather than expired exactly.
/// </summary>
public static class ClientUptimeTracker
{
    public static readonly TimeSpan Window = TimeSpan.FromHours(24);

    public static bool IsClientUp(ClientStatusBusinessEntity client)
    {
        return client.RunSessions.Any(IsSessionCountingAsUp);
    }

    public static bool IsSessionCountingAsUp(ClientRunSessionBusinessEntity session)
    {
        if (session.IsExpired())
            return false;

        return session.HealthStatus is FigHealthStatus.Healthy or FigHealthStatus.Unknown;
    }

    /// <summary>
    /// Updates persisted uptime metadata when the up/down state may have changed.
    /// Returns true if metadata was modified.
    /// </summary>
    public static bool ApplyStateChange(ClientStatusBusinessEntity client, DateTime utcNow)
    {
        var isUp = IsClientUp(client);

        if (client.UptimeWindowStartUtc is null || client.UptimeLastStateChangeUtc is null)
        {
            client.UptimeWindowStartUtc = utcNow;
            client.UptimeLastStateChangeUtc = utcNow;
            client.UptimeCurrentlyUp = isUp;
            client.UptimeAccumulatedMs = 0;
            return true;
        }

        if (client.UptimeCurrentlyUp == isUp)
            return false;

        CloseOpenSegment(client, utcNow);
        ShrinkWindowIfNeeded(client, utcNow);

        client.UptimeCurrentlyUp = isUp;
        client.UptimeLastStateChangeUtc = utcNow;
        return true;
    }

    /// <summary>
    /// Live uptime percent for the rolling ~24h window. Null until first observation.
    /// </summary>
    public static double? ComputePercent(ClientStatusBusinessEntity client, DateTime utcNow)
    {
        if (client.UptimeWindowStartUtc is null || client.UptimeLastStateChangeUtc is null)
            return null;

        var accumulated = (double)client.UptimeAccumulatedMs;
        if (client.UptimeCurrentlyUp)
            accumulated += (utcNow - client.UptimeLastStateChangeUtc.Value).TotalMilliseconds;

        if (accumulated < 0)
            accumulated = 0;

        var windowStart = client.UptimeWindowStartUtc.Value;
        var elapsed = utcNow - windowStart;
        if (elapsed <= TimeSpan.Zero)
            return client.UptimeCurrentlyUp ? 100.0 : 0.0;

        double windowMs;
        if (elapsed > Window)
        {
            var scale = Window.TotalMilliseconds / elapsed.TotalMilliseconds;
            accumulated *= scale;
            windowMs = Window.TotalMilliseconds;
        }
        else
        {
            windowMs = elapsed.TotalMilliseconds;
        }

        if (windowMs <= 0)
            return 0;

        var percent = accumulated / windowMs * 100.0;
        if (percent < 0)
            return 0;
        if (percent > 100)
            return 100;
        return percent;
    }

    private static void CloseOpenSegment(ClientStatusBusinessEntity client, DateTime utcNow)
    {
        if (!client.UptimeCurrentlyUp || client.UptimeLastStateChangeUtc is null)
            return;

        var segmentMs = (utcNow - client.UptimeLastStateChangeUtc.Value).TotalMilliseconds;
        if (segmentMs > 0)
            client.UptimeAccumulatedMs += (long)segmentMs;
    }

    private static void ShrinkWindowIfNeeded(ClientStatusBusinessEntity client, DateTime utcNow)
    {
        if (client.UptimeWindowStartUtc is null)
            return;

        var elapsed = utcNow - client.UptimeWindowStartUtc.Value;
        if (elapsed <= Window)
            return;

        var scale = Window.TotalMilliseconds / elapsed.TotalMilliseconds;
        client.UptimeAccumulatedMs = (long)(client.UptimeAccumulatedMs * scale);
        client.UptimeWindowStartUtc = utcNow - Window;
    }
}
