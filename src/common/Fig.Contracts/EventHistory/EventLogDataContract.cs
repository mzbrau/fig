using System;

namespace Fig.Contracts.EventHistory
{
    public class EventLogDataContract
    {
        public EventLogDataContract(DateTime timestamp,
            string? clientName,
            string? instance,
            string? settingName,
            string eventType,
            string? originalValue,
            string? newValue,
            string? authenticatedUser,
            string? message,
            string? verificationName,
            string? ipAddress,
            string? hostname)
        {
            Timestamp = timestamp;
            ClientName = clientName;
            Instance = instance;
            SettingName = settingName;
            EventType = eventType;
            OriginalValue = originalValue;
            NewValue = newValue;
            AuthenticatedUser = authenticatedUser;
            Message = message;
            VerificationName = verificationName;
            IpAddress = ipAddress;
            Hostname = hostname;
        }

        public DateTime Timestamp { get; }

        public string? ClientName { get; }

        public string? Instance { get; }

        public string? SettingName { get; }

        public string EventType { get; }

        public string? OriginalValue { get; }

        public string? NewValue { get; }

        public string? AuthenticatedUser { get; }
        
        public string? Message { get; }

        public string? VerificationName { get; }

        public string? IpAddress { get; }

        public string? Hostname { get; }
    }
}