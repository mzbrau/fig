using System;

namespace Fig.Contracts.Status
{
    public class ApiStatusDataContract
    {
        public Guid RuntimeId { get; set; }

        public DateTime StartTimeUtc { get; set; }

        public DateTime LastSeen { get; set; }

        public string? IpAddress { get; set; }

        public string? Hostname { get; set; }
        
        public long MemoryUsageBytes { get; set; }

        public string Version { get; set; }
    }
}