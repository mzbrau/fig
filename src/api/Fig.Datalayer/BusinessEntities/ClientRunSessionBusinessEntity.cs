namespace Fig.Datalayer.BusinessEntities;

public class ClientRunSessionBusinessEntity
{
    public virtual Guid Id { get; set; }

    public virtual Guid RunSessionId { get; set; }

    public virtual DateTime? LastSeen { get; set; }

    public virtual bool? LiveReload { get; set; }

    public virtual double? PollIntervalMs { get; set; }

    public virtual double UptimeSeconds { get; set; }

    public virtual string? IpAddress { get; set; }

    public virtual string? Hostname { get; set; }

    public virtual string FigVersion { get; set; }

    public virtual string ApplicationVersion { get; set; }
}