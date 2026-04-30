using System;

namespace Fig.Client.ConfigurationProvider;

internal readonly struct ProviderRegistrationKey : IEquatable<ProviderRegistrationKey>
{
    public ProviderRegistrationKey(string clientName, string? instance, Type settingsType)
    {
        ClientName = clientName ?? throw new ArgumentNullException(nameof(clientName));
        Instance = InstanceNormalization.Normalize(instance);
        SettingsType = settingsType ?? throw new ArgumentNullException(nameof(settingsType));
    }

    public string ClientName { get; }

    public string? Instance { get; }

    public Type SettingsType { get; }

    public bool Equals(ProviderRegistrationKey other)
    {
        return string.Equals(ClientName, other.ClientName, StringComparison.Ordinal) &&
               string.Equals(Instance, other.Instance, StringComparison.Ordinal) &&
               SettingsType == other.SettingsType;
    }

    public override bool Equals(object? obj)
    {
        return obj is ProviderRegistrationKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = StringComparer.Ordinal.GetHashCode(ClientName);
            hashCode = (hashCode * 397) ^ (Instance is null ? 0 : StringComparer.Ordinal.GetHashCode(Instance));
            hashCode = (hashCode * 397) ^ SettingsType.GetHashCode();
            return hashCode;
        }
    }
}
