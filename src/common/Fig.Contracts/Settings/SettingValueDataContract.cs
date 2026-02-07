using System;

namespace Fig.Contracts.Settings
{
    public class SettingValueDataContract
    {
        public SettingValueDataContract(string name, string value, DateTime changedAt, string changedBy)
            : this(name, value, changedAt, changedBy, null)
        {
        }

        public SettingValueDataContract(string name, string value, DateTime changedAt, string changedBy,
            string? changeMessage)
        {
            Name = name;
            Value = value;
            ChangedAt = changedAt;
            ChangedBy = changedBy;
            ChangeMessage = changeMessage;
        }

        public string Name { get; }
        
        public string Value { get; }
        
        public DateTime ChangedAt { get; }
        
        public string ChangedBy { get; }

        public string? ChangeMessage { get; }
    }
}