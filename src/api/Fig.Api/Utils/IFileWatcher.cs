namespace Fig.Api.Utils;

public interface IFileWatcher : IDisposable
{
    event EventHandler<FileSystemEventArgs> FileCreated;
}