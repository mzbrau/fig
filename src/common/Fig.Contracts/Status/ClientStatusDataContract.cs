using System;
using System.Collections.Generic;

namespace Fig.Contracts.Status
{
    public class ClientStatusDataContract
    {
        public ClientStatusDataContract(string name, string? instance, DateTime? lastRegistration, DateTime? lastSettingValueUpdate, ICollection<ClientRunSessionDataContract> runSessions)
        {
            Name = name;
            Instance = instance;
            LastRegistration = lastRegistration;
            LastSettingValueUpdate = lastSettingValueUpdate;
            RunSessions = runSessions;
        }

        public string Name { get; }
        
        public string? Instance { get; }

        public DateTime? LastRegistration { get; }

        public DateTime? LastSettingValueUpdate { get; }
        
        public ICollection<ClientRunSessionDataContract> RunSessions { get; }
    }
}