using System;
using System.Collections.Generic;

namespace Fig.Contracts.Status
{
    public class ClientStatusDataContract
    {
        public virtual string Name { get; set; } = string.Empty;
        
        public virtual string? Instance { get; set; }

        public virtual DateTime? LastRegistration { get; set; }

        public virtual DateTime? LastSettingValueUpdate { get; set; }
        
        public virtual ICollection<ClientRunSessionDataContract> RunSessions { get; set; }
    }
}