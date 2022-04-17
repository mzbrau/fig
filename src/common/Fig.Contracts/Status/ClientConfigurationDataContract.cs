using System;

namespace Fig.Contracts.Status
{
    public class ClientConfigurationDataContract
    {
        public Guid RunSessionId { get; set; }
        
        public int PollIntervalMs { get; set; }
        
        public bool LiveReload { get; set; } 
    }
}