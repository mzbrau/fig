namespace Fig.Api.Encryption;

public interface ICertificateImporter : IDisposable
{
    Task Initialize();
}