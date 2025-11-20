using Fig.Client.Utils;
using NUnit.Framework;
using System.Runtime.InteropServices;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class WindowsServiceDetectorTests
{
    [Test]
    public void IsRunningAsWindowsService_OnNonWindows_ReturnsFalse()
    {
        // This test will pass on Linux/Mac, and on Windows it depends on the execution context
        var result = WindowsServiceDetector.IsRunningAsWindowsService();
        
        // On non-Windows platforms, should always return false
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.That(result, Is.False, "Should return false on non-Windows platforms");
        }
        // On Windows when running interactively (like in a test runner), should return false
        else
        {
            // When running unit tests, Environment.UserInteractive should be true
            Assert.That(result, Is.False, "Should return false when running interactively on Windows");
        }
    }
}
