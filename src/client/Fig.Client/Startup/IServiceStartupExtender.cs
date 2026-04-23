using System;

namespace Fig.Client.Startup;

/// <summary>
/// Allows the host application to signal to the process supervisor (e.g. the Windows
/// Service Control Manager) that more startup time is needed before Fig's registration
/// call is issued.  The default implementation is a no-op so this feature is opt-in.
/// </summary>
public interface IServiceStartupExtender
{
    /// <summary>
    /// Called by the Fig client immediately before issuing a long-running registration
    /// HTTP call.  Implementations should request additional time from the supervisor
    /// (e.g. via <c>ServiceBase.RequestAdditionalTime</c>) if needed.
    /// </summary>
    /// <param name="requestedTime">
    /// A hint for how much additional time the caller expects the next operation to need.
    /// Implementations may round up or ignore this value if the supervisor API does not
    /// accept fine-grained durations.
    /// </param>
    void RequestAdditionalTime(TimeSpan requestedTime);
}
