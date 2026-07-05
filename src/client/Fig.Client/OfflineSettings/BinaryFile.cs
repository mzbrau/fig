using System.IO;

namespace Fig.Client.OfflineSettings;

internal class BinaryFile : IBinaryFile
{
    private const string FileExtension = "dat";

    public void Write(string clientName, string? instance, string value)
    {
        FigAppDataPaths.EnsureFigFolderExists();
        Delete(clientName, instance);

        using var fileStream = new FileStream(GetFilePath(clientName, instance), FileMode.Create);
        using var binaryWriter = new BinaryWriter(fileStream);
        binaryWriter.Write(value);
        binaryWriter.Close();
    }

    public string? Read(string clientName, string? instance)
    {
        var path = GetFilePath(clientName, instance);
        if (!File.Exists(path))
            return null;

        using var fileStream = new FileStream(GetFilePath(clientName, instance), FileMode.Open);
        using var binaryReader = new BinaryReader(fileStream);
        var value = binaryReader.ReadString();
        fileStream.Close();

        return value;
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