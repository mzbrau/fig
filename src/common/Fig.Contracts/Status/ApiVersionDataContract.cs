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
        
        [Obsolete("Hostname is no longer populated for security reasons. This property will be removed in a future version.")]
        public string Hostname { get; }
        
        public DateTime LastSettingChange { get; }
    }
}