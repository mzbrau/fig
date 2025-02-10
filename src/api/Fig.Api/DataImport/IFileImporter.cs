namespace Fig.Api.DataImport;

public interface IFileImporter : IDisposable
{
    Task Initialize(
        string path, 
        string filter, 
        Func<string, Task> import, 
        Func<Task<bool>> canImport,
        CancellationToken stoppingToken);
}