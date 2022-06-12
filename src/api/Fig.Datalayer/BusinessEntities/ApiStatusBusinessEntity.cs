namespace Fig.Datalayer.BusinessEntities;

public class ApiStatusBusinessEntity
{
    public virtual Guid? Id { get; set; }

    public virtual Guid RuntimeId { get; set; }

    public virtual double UptimeSeconds { get; set; }

    public virtual DateTime LastSeen { get; set; }

    public virtual string? IpAddress { get; set; }

    public virtual string? Hostname { get; set; }

    public virtual string Version { get; set; }

    public virtual List<string> CertificatesInStore { get; set; }

    public virtual bool IsActive { get; set; }
}