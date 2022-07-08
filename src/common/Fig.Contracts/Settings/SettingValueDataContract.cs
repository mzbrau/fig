using System;

namespace Fig.Contracts.Settings
{
    public class SettingValueDataContract
    {
        public SettingValueDataContract(string name, string value, DateTime changedAt, string changedBy)
        {
            Name = name;
            Value = value;
            ChangedAt = changedAt;
            ChangedBy = changedBy;
        }

        public string Name { get; }
        
        public string Value { get; }
        
        public DateTime ChangedAt { get; }
        
        public string ChangedBy { get; }
    }
}