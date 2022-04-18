using System;

namespace Fig.Client.Configuration
{
    public interface IFigOptions
    {
        Uri ApiUri { get; }

        public double PollIntervalMs { get; }

        public bool LiveReload { get; }
    }
}