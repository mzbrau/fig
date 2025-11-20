using System;
using System.Runtime.InteropServices;

namespace Fig.Client.Utils;

/// <summary>
/// Utility class to detect if the application is running as a Windows service.
/// </summary>
public static class WindowsServiceDetector
{
    /// <summary>
    /// Determines if the current process is running as a Windows service.
    /// </summary>
    /// <returns>True if running as a Windows service, false otherwise.</returns>
    public static bool IsRunningAsWindowsService()
    {
        // Only applicable on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return false;

        // Environment.UserInteractive is false when running as a Windows service
        // This property is only available on Windows
        return !Environment.UserInteractive;
    }
}
