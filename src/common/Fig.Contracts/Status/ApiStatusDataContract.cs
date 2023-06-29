using System;

namespace Fig.Contracts.Status
{
    public class ApiStatusDataContract
    {
        public ApiStatusDataContract(Guid runtimeId, DateTime startTimeUtc, DateTime lastSeen, string? ipAddress, string? hostname, long memoryUsageBytes, string runningUser, long totalRequests, double requestsPerMinute, string version, bool configurationErrorDetected, int numberOfPluginVerifiers, string pluginVerifiers)
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
            NumberOfPluginVerifiers = numberOfPluginVerifiers;
            PluginVerifiers = pluginVerifiers;
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
        
        public int NumberOfPluginVerifiers { get; }

        public string PluginVerifiers { get; }
    }
}

