namespace Fig.Api.DataImport;

public interface IFileImporter : IDisposable
{
    Task Initialize();
}