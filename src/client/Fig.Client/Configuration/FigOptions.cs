using System;

namespace Fig.Client.Configuration
{
    public class FigOptions : IFigOptions
    {
        public Uri ApiUri { get; private set; }
        public double PollIntervalMs { get; private set; } = 30000;
        public bool LiveReload { get; private set; } = true;

        public IFigOptions StaticUri(string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException();

            ApiUri = new Uri(value);
            return this;
        }

        public IFigOptions ReadUriFromEnvironmentVariable(string variableName)
        {
            var value = Environment.GetEnvironmentVariable(variableName);
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"Environment variable {variableName} contained no value");

            ApiUri = new Uri(value);
            return this;
        }

        public IFigOptions SetPollInterval(int pollIntervalMs)
        {
            PollIntervalMs = pollIntervalMs;
            return this;
        }

        public IFigOptions SetLiveReload(bool liveReload)
        {
            LiveReload = liveReload;
            return this;
        }
    }
}