using System.IO;
using Fig.Client.OfflineSettings;

namespace Fig.Client.RegistrationChecksum;

public class RegistrationChecksumStore : IRegistrationChecksumStore
{
    private const string FileExtension = "checksum";

    public string? Get(string clientName, string? instance)
    {
        var path = GetFilePath(clientName, instance);
        if (!File.Exists(path))
            return null;

        var value = File.ReadAllText(path).Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public void Save(string clientName, string? instance, string checksum)
    {
        FigAppDataPaths.EnsureFigFolderExists();
        File.WriteAllText(GetFilePath(clientName, instance), checksum);
    }

    public void Delete(string clientName, string? instance)
    {
        var path = GetFilePath(clientName, instance);
        if (File.Exists(path))
            File.Delete(path);
    }

    public string GetFilePath(string clientName, string? instance) =>
        FigAppDataPaths.GetFilePath(clientName, instance, FileExtension);
}
