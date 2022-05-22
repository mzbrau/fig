namespace Fig.Api.Utils;

public interface IFileMonitor
{
    Task<bool> WaitUntilUnlocked(string path, TimeSpan timeout);
}