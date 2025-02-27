namespace Fig.Api.ExtensionMethods;

public static class StringExtensionMethods
{
    public static string NormalizeToLegacyConnectionString(this string connectionString)
    {
        // https://github.com/dotnet/SqlClient/issues/811
        return connectionString
            .Replace("Application Intent", "ApplicationIntent")
            .Replace("Connect Retry Count", "ConnectRetryCount")
            .Replace("Connect Retry Interval", "ConnectRetryInterval")
            .Replace("Pool Blocking Period", "PoolBlockingPeriod")
            .Replace("Multiple Active Result Sets", "MultipleActiveResultSets")
            .Replace("Multi Subnet Failover", "MultiSubnetFailover")
            .Replace("Transparent Network IP Resolution", "TransparentNetworkIPResolution")
            .Replace("Trust Server Certificate", "TrustServerCertificate");
    }
}