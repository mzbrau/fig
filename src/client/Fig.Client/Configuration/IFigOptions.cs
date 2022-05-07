using System;

namespace Fig.Client.Configuration
{
    public interface IFigOptions
    {
        Uri ApiUri { get; }

        public double PollIntervalMs { get; }

        public bool LiveReload { get; }

        SecretStore SecretStore { get; }

        string Instance { get; }

        string ClientSecret { get; }

        string? VersionOverride { get; }

        bool AllowOfflineSettings { get; }

        IFigOptions ReadUriFromEnvironmentVariable();

        IFigOptions WithApiAddress(string value);

        IFigOptions WithPollInterval(int pollIntervalMs);

        IFigOptions WithLiveReload(bool liveReload = true);

        IFigOptions WithInstance(string instance);

        IFigOptions WithSecretStore(SecretStore secretStore);

        IFigOptions WithSecret(string secret);

        IFigOptions OverrideApplicationVersion(string version);

        IFigOptions WithOfflineSettings(bool isEnabled);
    }
}