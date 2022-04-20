namespace Fig.Web.Models.Clients;

public class ClientRunSessionModel
{
    public string Name { get; set; }
        
    public string? Instance { get; set; }

    public DateTime? LastRegistration { get; set; }

    public DateTime? LastSettingValueUpdate { get; set; }

    public Guid RunSessionId { get; set; }

    public DateTime? LastSeen { get; set; }

    public bool? LiveReload { get; set; }

    public double? PollIntervalMs { get; set; }

    public double UptimeSeconds { get; set; }

    public string? IpAddress { get; set; }

    public string? Hostname { get; set; }
}