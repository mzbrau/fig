namespace Fig.Client.ConfigurationProvider;

internal static class InstanceNormalization
{
    public static string? Normalize(string? instance)
    {
        return string.IsNullOrWhiteSpace(instance)
            ? null
            : instance;
    }
}
