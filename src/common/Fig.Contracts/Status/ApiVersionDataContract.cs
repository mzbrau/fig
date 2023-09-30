using System;

namespace Fig.Contracts.Status
{
    public class ApiVersionDataContract
    {
        public ApiVersionDataContract(string apiVersion, string hostname, DateTime lastSettingChange)
        {
            ApiVersion = apiVersion;
            Hostname = hostname;
            LastSettingChange = lastSettingChange;
        }

        public string ApiVersion { get; }
        
        public string Hostname { get; }
        
        public DateTime LastSettingChange { get; }
    }
}