namespace Fig.Client.Configuration
{
    public enum SecretStore
    {
        EnvironmentVariable,
        AppSettings,
        DpApi,
        InCode // Not suitable for production
    }
}