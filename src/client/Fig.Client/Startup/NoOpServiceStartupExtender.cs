using System;

namespace Fig.Client.Startup;

/// <summary>
/// No-op implementation of <see cref="IServiceStartupExtender"/> used when the host
/// application has not registered a custom extender.  All calls are silently discarded.
/// </summary>
internal sealed class NoOpServiceStartupExtender : IServiceStartupExtender
{
    public void RequestAdditionalTime(TimeSpan requestedTime)
    {
        // Intentionally empty.
    }
}
