namespace Fig.Datalayer.BusinessEntities;

public abstract class ClientBase
{
    public virtual Guid Id { get; set; }

    public virtual string Name { get; set; } = string.Empty;

    public virtual string ClientSecret { get; set; } = string.Empty;

    public virtual string? Instance { get; set; }

    public virtual DateTime? LastRegistration { get; set; }

    public virtual DateTime? LastSettingValueUpdate { get; set; }

    public virtual DateTime? LastSeen { get; set; }

    public virtual bool? LiveReload { get; set; }

    public virtual int? PollIntervalSeconds { get; set; }
    
    public virtual double UptimeSeconds { get; set; }

    public virtual string? IpAddress { get; set; }

    public virtual string? Hostname { get; set; }
}