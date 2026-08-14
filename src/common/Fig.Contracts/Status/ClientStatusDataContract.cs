using System;
using System.Collections.Generic;

namespace Fig.Contracts.Status
{
    public class ClientStatusDataContract
    {
        public ClientStatusDataContract(string name,
            string? instance,
            DateTime? lastRegistration,
            DateTime? lastSettingValueUpdate,
            ICollection<ClientRunSessionDataContract> runSessions,
            DateTime? lastRunSessionDisconnected = null,
            string? lastRunSessionMachineName = null,
            double? uptimePercent24Hr = null)
        {
            Name = name;
            Instance = instance;
            LastRegistration = lastRegistration;
            LastSettingValueUpdate = lastSettingValueUpdate;
            RunSessions = runSessions;
            LastRunSessionDisconnected = lastRunSessionDisconnected;
            LastRunSessionMachineName = lastRunSessionMachineName;
            UptimePercent24Hr = uptimePercent24Hr;
        }

        public string Name { get; }
        
        public string? Instance { get; }

        public DateTime? LastRegistration { get; }

        public DateTime? LastSettingValueUpdate { get; }
        
        public DateTime? LastRunSessionDisconnected { get; }
        
        public string? LastRunSessionMachineName { get; }

        /// <summary>Approximate rolling 24-hour uptime percentage (0–100), or null before first observation.</summary>
        public double? UptimePercent24Hr { get; }
        
        public ICollection<ClientRunSessionDataContract> RunSessions { get; }
    }
}
