using System.Diagnostics;

namespace Fig.Api.Utils;

public class FileMonitor : IFileMonitor
{
    public async Task<bool> WaitUntilUnlocked(string path, TimeSpan timeout)
    {
        var watch = Stopwatch.StartNew();
        var file = new FileInfo(path);

        while (IsFileLocked(file))
        {
            if (watch.ElapsedMilliseconds < timeout.TotalMilliseconds)
            {
                return false;
            }
            
            await Task.Delay(100);
        }

        return true;
    }

    private bool IsFileLocked(FileInfo file)
    {
        try
        {
            using FileStream stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            stream.Close();
        }
        catch (IOException)
        {
            //the file is unavailable because it is:
            //still being written to
            //or being processed by another thread
            //or does not exist (has already been processed)
            return true;
        }

        //file is not locked
        return false;
    }
}