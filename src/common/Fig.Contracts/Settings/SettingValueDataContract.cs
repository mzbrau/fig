using System;

namespace Fig.Contracts.Settings
{
    public class SettingValueDataContract
    {
        // TODO: is name required?
        public string Name { get; set; }
        
        public string Value { get; set; }
        
        public DateTime ChangedAt { get; set; }
        
        public string ChangedBy { get; set; }
    }
}