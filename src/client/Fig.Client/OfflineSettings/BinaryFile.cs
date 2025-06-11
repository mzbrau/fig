using System;
using System.IO;
using System.Linq;

namespace Fig.Client.OfflineSettings;

internal class BinaryFile : IBinaryFile
{
    public void Write(string clientName, string? instance, string value)
    {
        CreateFigFolder();
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
        {
            File.Delete(path);
        }
    }

    public string GetFilePath(string clientName, string? instance)
    {
        var sanitizedClientName = SanitizeFileName(clientName);
        var sanitizedInstance = string.IsNullOrWhiteSpace(instance) ? null : SanitizeFileName(instance!);

        var fileName = sanitizedInstance is null
            ? $"{sanitizedClientName}.dat"
            : $"{sanitizedClientName}_{sanitizedInstance}.dat";

        return Path.Combine(GetFigFolder(), fileName);
    }

    private string GetFigFolder()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "Fig");
    }

    private string SanitizeFileName(string input)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(input.Where(c => !invalidChars.Contains(c)).ToArray());
        return sanitized.Replace(" ", string.Empty);
    }

    private void CreateFigFolder()
    {
        var folder = GetFigFolder();
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }
    }
}