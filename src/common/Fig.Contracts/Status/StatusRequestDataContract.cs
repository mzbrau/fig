using System;

namespace Fig.Contracts.Status
{
    public class StatusRequestDataContract
    {
        public double UptimeSeconds { get; set; }
        
        public DateTime LastSettingUpdate { get; set; }
        
        public int PollIntervalSeconds { get; set; }
        
        public bool LiveReload { get; set; } 
    }
}