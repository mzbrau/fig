using System;
using ICSharpCode.Decompiler.Semantics;

namespace Fig.Client.Configuration
{
    public class FigOptions : IFigOptions
    {
        private const string ApiAddressEnvironmentVariable = "FIG_API_URI";
        
        public Uri? ApiUri { get; set; }

        public SecretStore SecretStore { get; set; }

        public double PollIntervalMs { get; set; } = 30000;

        public bool LiveReload { get; set; } = true;

        public string Instance { get; set; }

        public string ClientSecret { get; set; }

        public string? VersionOverride { get; set; }

        public bool AllowOfflineSettings { get; set; } = true;

        public IFigOptions ReadUriFromEnvironmentVariable()
        {
            var value = Environment.GetEnvironmentVariable(ApiAddressEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"Environment variable {ApiAddressEnvironmentVariable} contained no value");

            ApiUri = new Uri(value);
            return this;
        }
        
        public IFigOptions WithApiAddress(string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException();

            ApiUri = new Uri(value);
            return this;
        }

        public IFigOptions WithPollInterval(int pollIntervalMs)
        {
            PollIntervalMs = pollIntervalMs;
            return this;
        }

        public IFigOptions WithLiveReload(bool liveReload = true)
        {
            LiveReload = liveReload;
            return this;
        }

        public IFigOptions WithInstance(string instance)
        {
            Instance = instance;
            return this;
        }

        public IFigOptions WithSecretStore(SecretStore secretStore)
        {
            SecretStore = secretStore;
            return this;
        }

        public IFigOptions WithSecret(string secret)
        {
            SecretStore = SecretStore.InCode;
            ClientSecret = secret;
            return this;
        }

        public IFigOptions OverrideApplicationVersion(string version)
        {
            VersionOverride = version;
            return this;
        }

        public IFigOptions WithOfflineSettings(bool isEnabled)
        {
            AllowOfflineSettings = isEnabled;
            return this;
        }
    }
}