using System;

namespace Fig.Contracts.Status
{
    public class ApiVersionDataContract
    {
        public ApiVersionDataContract(string apiVersion, DateTime lastSettingChange)
        {
            ApiVersion = apiVersion;
            LastSettingChange = lastSettingChange;
        }

        public string ApiVersion { get; }
        
        public DateTime LastSettingChange { get; }
    }
}