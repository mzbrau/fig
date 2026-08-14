namespace Fig.Datalayer.BusinessEntities;

// ReSharper disable once ClassNeverInstantiated.Global used by NHibernate
public class ClientStatusBusinessEntity : ClientBase
{
    public virtual DateTime? LastRunSessionDisconnected { get; set; }

    public virtual string? LastRunSessionMachineName { get; set; }

    /// <summary>Start of the approximate rolling uptime accounting window.</summary>
    public virtual DateTime? UptimeWindowStartUtc { get; set; }

    /// <summary>Start of the current open up/down segment.</summary>
    public virtual DateTime? UptimeLastStateChangeUtc { get; set; }

    /// <summary>Whether the client was considered up at the last state change.</summary>
    public virtual bool UptimeCurrentlyUp { get; set; }

    /// <summary>Closed "up" milliseconds within the (scaled) window.</summary>
    public virtual long UptimeAccumulatedMs { get; set; }
}