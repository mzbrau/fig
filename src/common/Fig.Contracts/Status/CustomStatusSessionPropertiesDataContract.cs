using System;

namespace Fig.Contracts.Status
{
    /// <summary>
    /// Lightweight view of custom status properties for a single run session.
    /// </summary>
    public class CustomStatusSessionPropertiesDataContract
    {
        public CustomStatusSessionPropertiesDataContract(
            string clientName,
            string? instance,
            Guid runSessionId,
            DateTime? lastSeen,
            CustomStatusPropertiesDataContract? customProperties)
        {
            ClientName = clientName;
            Instance = instance;
            RunSessionId = runSessionId;
            LastSeen = lastSeen;
            CustomProperties = customProperties;
        }

        public string ClientName { get; }

        public string? Instance { get; }

        public Guid RunSessionId { get; }

        public DateTime? LastSeen { get; }

        public CustomStatusPropertiesDataContract? CustomProperties { get; }
    }
}
