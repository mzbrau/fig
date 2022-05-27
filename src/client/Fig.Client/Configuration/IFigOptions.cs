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
    }
}