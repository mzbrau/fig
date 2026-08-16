using System;
using System.IO;
using Fig.Api.Utils;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class FileMonitorTests
{
    [Test]
    public void IsFileLocked_ReturnsFalse_WhenFileIsUnlocked()
    {
        var path = Path.Combine(Path.GetTempPath(), $"fig-unlocked-{Guid.NewGuid():N}.txt");
        File.WriteAllText(path, "unlocked");

        try
        {
            var monitor = new FileMonitor();
            Assert.That(monitor.IsFileLocked(new FileInfo(path)), Is.False);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void IsFileLocked_ReturnsTrue_WhenFileIsLocked()
    {
        var path = Path.Combine(Path.GetTempPath(), $"fig-locked-{Guid.NewGuid():N}.txt");
        File.WriteAllText(path, "locked");

        try
        {
            using var lockedStream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            var monitor = new FileMonitor();
            Assert.That(monitor.IsFileLocked(new FileInfo(path)), Is.True);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void IsFileLocked_ReturnsTrue_WhenFileDoesNotExist()
    {
        var path = Path.Combine(Path.GetTempPath(), $"fig-missing-{Guid.NewGuid():N}.txt");
        var monitor = new FileMonitor();

        Assert.That(monitor.IsFileLocked(new FileInfo(path)), Is.True);
    }
}
