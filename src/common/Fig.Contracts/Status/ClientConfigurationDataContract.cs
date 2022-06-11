using System;

namespace Fig.Contracts.Status
{
    public class ClientConfigurationDataContract
    {
        public Guid RunSessionId { get; set; }

        public double? PollIntervalMs { get; set; }

        public bool? LiveReload { get; set; }

        public bool RestartRequested { get; set; }
    }
}