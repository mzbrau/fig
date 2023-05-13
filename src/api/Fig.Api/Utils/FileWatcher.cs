namespace Fig.Api.Utils;

public class FileWatcher : IFileWatcher
{
    private readonly FileSystemWatcher _watcher;
    
    public FileWatcher(string path, string filter)
    {
        _watcher = new FileSystemWatcher(path, filter);
        _watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName;

        _watcher.IncludeSubdirectories = true;
        _watcher.EnableRaisingEvents = true;
        _watcher.Created += OnCreated;
    }

    private void OnCreated(object sender, FileSystemEventArgs e)
    {
        FileCreated?.Invoke(this, e);
    }

    public event EventHandler<FileSystemEventArgs>? FileCreated;

    public void Dispose()
    {
        _watcher.Created -= OnCreated;
        _watcher.Dispose();
    }
}