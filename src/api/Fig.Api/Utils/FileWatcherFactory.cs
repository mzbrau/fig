namespace Fig.Api.Utils;

public class FileWatcherFactory : IFileWatcherFactory
{
    public IFileWatcher Create(string path, string filter)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        
        return new FileWatcher(path, filter);
    }
}