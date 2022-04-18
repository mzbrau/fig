using System;

namespace Fig.Contracts.Status
{
    public class ClientRunSessionDataContract
    {
        public Guid RunSessionId { get; set; }

        public DateTime? LastSeen { get; set; }

        public bool? LiveReload { get; set; }

        public double? PollIntervalMs { get; set; }

        public double UptimeSeconds { get; set; }

        public string? IpAddress { get; set; }

        public string? Hostname { get; set; }
    }
}