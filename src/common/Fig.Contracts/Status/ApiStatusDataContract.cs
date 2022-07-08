using System;

namespace Fig.Contracts.Status
{
    public class ApiStatusDataContract
    {
        public ApiStatusDataContract(Guid runtimeId, DateTime startTimeUtc, DateTime lastSeen, string? ipAddress, string? hostname, long memoryUsageBytes, string runningUser, long totalRequests, double requestsPerMinute, string version, bool configurationErrorDetected)
        {
            RuntimeId = runtimeId;
            StartTimeUtc = startTimeUtc;
            LastSeen = lastSeen;
            IpAddress = ipAddress;
            Hostname = hostname;
            MemoryUsageBytes = memoryUsageBytes;
            RunningUser = runningUser;
            TotalRequests = totalRequests;
            RequestsPerMinute = requestsPerMinute;
            Version = version;
            ConfigurationErrorDetected = configurationErrorDetected;
        }

        public Guid RuntimeId { get; }

        public DateTime StartTimeUtc { get; }

        public DateTime LastSeen { get; }

        public string? IpAddress { get; }

        public string? Hostname { get; }

        public long MemoryUsageBytes { get; }

        public string RunningUser { get; }

        public long TotalRequests { get; }

        public double RequestsPerMinute { get; }

        public string Version { get; }
        
        public bool ConfigurationErrorDetected { get; }
    }
}