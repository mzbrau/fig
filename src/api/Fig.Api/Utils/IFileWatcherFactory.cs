namespace Fig.Api.Utils;

public interface IFileWatcherFactory
{
    IFileWatcher Create(string path, string filter);
}