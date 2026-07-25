using System;

namespace Fig.Contracts.Status
{
    public class ApiVersionDataContract
    {
        public ApiVersionDataContract(string apiVersion, string hostname, DateTime lastSettingChange)
        {
            ApiVersion = apiVersion;
#pragma warning disable CS0618 // Hostname kept for wire compatibility; no longer populated
            Hostname = hostname;
#pragma warning restore CS0618
            LastSettingChange = lastSettingChange;
        }

        public string ApiVersion { get; }
        
        [Obsolete("Hostname is no longer populated for security reasons. This property will be removed in a future version.")]
        public string Hostname { get; }
        
        public DateTime LastSettingChange { get; }
    }
}