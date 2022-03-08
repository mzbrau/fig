using System;

namespace Fig.Contracts.EventHistory
{
    public class EventLogDataContract
    {
        public DateTime Timestamp { get; set; }

        public string? ClientName { get; set; }

        public string? Instance { get; set; }

        public string? SettingName { get; set; }

        public string EventType { get; set; }

        public string? OriginalValue { get; set; }

        public string? NewValue { get; set; }

        public string? AuthenticatedUser { get; set; }

        public string? VerificationName { get; set; }

        public string? IpAddress { get; set; }

        public string? Hostname { get; set; }
    }
}