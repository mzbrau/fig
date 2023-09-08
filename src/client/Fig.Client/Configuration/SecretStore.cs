using System;

namespace Fig.Client.Configuration
{
    [Serializable]
    public enum SecretStore
    {
        EnvironmentVariable,
        AppSettings,
        DpApi,
        InCode // Not suitable for production
    }
}