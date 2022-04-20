using System;
using System.Collections.Generic;

namespace Fig.Contracts.Status
{
    public class ClientStatusDataContract
    {
        public string Name { get; set; } = string.Empty;
        
        public string? Instance { get; set; }

        public DateTime? LastRegistration { get; set; }

        public DateTime? LastSettingValueUpdate { get; set; }
        
        public ICollection<ClientRunSessionDataContract> RunSessions { get; set; }
    }
}